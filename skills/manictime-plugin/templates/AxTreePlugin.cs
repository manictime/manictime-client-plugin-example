// TEMPLATE — working example: reads the current conversation title from the Claude desktop
// app. Per-app parts to change: the namespace/class names, the process-name check, the anchor
// predicate (here: landmark region "Primary pane"), the target predicate (here: first AXButton
// with a non-reject title), the reject list, and the DocumentInfo mapping at the end.
// Everything else (wake, caching, ownership, budgets) is the reusable machinery — keep it.
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Finkit.ManicTime.Shared.DocumentTracking;
using ManicTime;
using ManicTime.Client.Tracker.EventTracking.Publishers.ApplicationTracking;
using Microsoft.Extensions.DependencyInjection;

namespace ClaudeTitle;

public class ClaudeTitlePluginServiceConfigurator : IServiceConfigurator
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDocumentRetreiver, ClaudeTitleRetreiver>();
    }
}

// Reads the current conversation title from the Claude desktop app via the macOS accessibility
// API. Structure (found by inspecting the tree): the conversation title is the first AXButton
// inside the landmark region described as "Primary pane".
[DocumentRetreiver(DocumentCacheOption = DocumentCacheOption.ForFiveSecondsOrUntilTitleChange, CallOrder = 5)]
public class ClaudeTitleRetreiver : IDocumentRetreiver
{
    private const string Ax = "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";
    private const string Cf = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const uint Utf8 = 0x08000100;
    private const int MaxNodes = 2000;
    private const int MaxDepth = 40;

    [DllImport(Ax)] private static extern IntPtr AXUIElementCreateApplication(int pid);
    [DllImport(Ax)] private static extern int AXUIElementCopyAttributeValue(IntPtr element, IntPtr attr, ref IntPtr value);
    [DllImport(Ax)] private static extern int AXUIElementSetAttributeValue(IntPtr element, IntPtr attr, IntPtr value);
    [DllImport(Cf)] private static extern IntPtr CFStringCreateWithCString(IntPtr alloc, string s, uint enc);
    [DllImport(Cf)] [return: MarshalAs(UnmanagedType.I1)] private static extern bool CFStringGetCString(IntPtr s, byte[] buf, nint size, uint enc);
    [DllImport(Cf)] private static extern nint CFArrayGetCount(IntPtr array);
    [DllImport(Cf)] private static extern IntPtr CFArrayGetValueAtIndex(IntPtr array, nint index);
    [DllImport(Cf)] private static extern nint CFGetTypeID(IntPtr cf);
    [DllImport(Cf)] private static extern nint CFStringGetTypeID();
    [DllImport(Cf)] private static extern void CFRelease(IntPtr cf);
    [DllImport(Cf)] private static extern IntPtr CFRetain(IntPtr cf);
    [DllImport("/usr/lib/libSystem.dylib")] private static extern IntPtr dlopen(string path, int mode);
    [DllImport("/usr/lib/libSystem.dylib")] private static extern IntPtr dlsym(IntPtr handle, string name);

    private static readonly IntPtr RoleAttr = Str("AXRole");
    private static readonly IntPtr SubroleAttr = Str("AXSubrole");
    private static readonly IntPtr TitleAttr = Str("AXTitle");
    private static readonly IntPtr DescriptionAttr = Str("AXDescription");
    private static readonly IntPtr ChildrenAttr = Str("AXChildren");
    private static readonly IntPtr FocusedWindowAttr = Str("AXFocusedWindow");
    private static readonly IntPtr MainWindowAttr = Str("AXMainWindow");
    private static readonly IntPtr ManualAccessibilityAttr = Str("AXManualAccessibility");

    private static readonly HashSet<string> RejectTitles = new(StringComparer.OrdinalIgnoreCase)
        { "Claude", "New chat", "New conversation" };

    private readonly HashSet<int> _wokenProcessIds = new();

    // Walking the whole tree costs one IPC round trip per attribute per node (seconds for a big
    // Electron tree), so the located "Primary pane" element is cached per process and only the
    // small title search under it runs on each call. Evicted when it stops answering.
    private readonly Dictionary<int, IntPtr> _primaryPaneByProcessId = new();

    public DocumentInfo? GetDocument(ApplicationInfo application)
    {
        if (!string.Equals(application.ProcessName, "Claude", StringComparison.OrdinalIgnoreCase)
            || application.ProcessId == null)
            return null;
        try
        {
            return GetDocumentCore(application.ProcessId.Value);
        }
        catch
        {
            return null;
        }
    }

    private DocumentInfo? GetDocumentCore(int processId)
    {
        IntPtr app = AXUIElementCreateApplication(processId);
        if (app == IntPtr.Zero)
            return null;
        try
        {
            // Electron exposes a stub accessibility tree until an assistive client announces
            // itself; set the flag once per process and read the tree on a later call.
            if (_wokenProcessIds.Add(processId))
            {
                IntPtr trueSymbol = dlsym(dlopen(Cf, 0), "kCFBooleanTrue");
                if (trueSymbol != IntPtr.Zero)
                    AXUIElementSetAttributeValue(app, ManualAccessibilityAttr, Marshal.ReadIntPtr(trueSymbol));
                return null;
            }

            string? title = ReadTitle(processId, app);
            if (title == null)
                return null;

            return new DocumentInfo
            {
                DocumentGroupName = "Claude",
                DocumentName = title,
                DocumentType = DocumentTypes.Chat,
                Title = title
            };
        }
        finally
        {
            CFRelease(app);
        }
    }

    private string? ReadTitle(int processId, IntPtr app)
    {
        if (!_primaryPaneByProcessId.TryGetValue(processId, out IntPtr primaryPane))
        {
            IntPtr window = Copy(app, FocusedWindowAttr);
            if (window == IntPtr.Zero)
                window = Copy(app, MainWindowAttr);
            if (window == IntPtr.Zero)
                return null;
            try
            {
                int budget = MaxNodes;
                primaryPane = FindNode(window, 0, ref budget, node =>
                    Read(node, SubroleAttr) == "AXLandmarkRegion" && Read(node, DescriptionAttr) == "Primary pane");
            }
            finally
            {
                CFRelease(window);
            }
            if (primaryPane == IntPtr.Zero)
            {
                // No pane at all — the tree may be a stub (recycled pid after an app restart),
                // so forget the wake and re-send it on the next call.
                _wokenProcessIds.Remove(processId);
                return null;
            }
            _primaryPaneByProcessId[processId] = primaryPane;
        }

        int titleBudget = MaxNodes;
        string? title = null;
        IntPtr titleNode = FindNode(primaryPane, 0, ref titleBudget, node =>
        {
            if (Read(node, RoleAttr) != "AXButton")
                return false;
            string? candidate = Normalize(Read(node, TitleAttr));
            if (string.IsNullOrWhiteSpace(candidate) || RejectTitles.Contains(candidate))
                return false;
            title = candidate;
            return true;
        });
        if (titleNode != IntPtr.Zero)
            CFRelease(titleNode);

        // No acceptable title: keep the cached pane while it still answers (there may simply be
        // no conversation open); evict it only when it went stale (window re-rendered).
        if (title == null && Read(primaryPane, RoleAttr) == null)
        {
            _primaryPaneByProcessId.Remove(processId);
            CFRelease(primaryPane);
        }
        return title;
    }

    // Electron apps embed invisible Unicode direction marks in accessible names; strip them and
    // collapse whitespace so titles compare and display cleanly.
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    private static string? Normalize(string? value) =>
        value == null
            ? null
            : Regex.Replace(
                Regex.Replace(value, @"[\u200E\u200F\u202A-\u202E\u2066-\u2069]", "", RegexOptions.None, RegexTimeout),
                @"\s+", " ", RegexOptions.None, RegexTimeout).Trim();

    // Depth-first search; returns the first node matching the predicate, RETAINED (caller
    // releases), or IntPtr.Zero. Elements from a released child array are not valid to keep.
    private static IntPtr FindNode(IntPtr node, int depth, ref int budget, Func<IntPtr, bool> matches)
    {
        if (depth > MaxDepth || --budget < 0)
            return IntPtr.Zero;
        if (matches(node))
            return CFRetain(node);

        IntPtr children = Copy(node, ChildrenAttr);
        if (children == IntPtr.Zero)
            return IntPtr.Zero;
        try
        {
            nint count = CFArrayGetCount(children);
            for (nint i = 0; i < count; i++)
            {
                IntPtr found = FindNode(CFArrayGetValueAtIndex(children, i), depth + 1, ref budget, matches);
                if (found != IntPtr.Zero)
                    return found;
            }
            return IntPtr.Zero;
        }
        finally
        {
            CFRelease(children);
        }
    }

    private static IntPtr Copy(IntPtr element, IntPtr attribute)
    {
        IntPtr value = IntPtr.Zero;
        return AXUIElementCopyAttributeValue(element, attribute, ref value) == 0 ? value : IntPtr.Zero;
    }

    private static string? Read(IntPtr element, IntPtr attribute)
    {
        IntPtr value = Copy(element, attribute);
        if (value == IntPtr.Zero)
            return null;
        try
        {
            if (CFGetTypeID(value) != CFStringGetTypeID())
                return null;
            byte[] buffer = new byte[2048];
            if (!CFStringGetCString(value, buffer, buffer.Length, Utf8))
                return null;
            int end = Array.IndexOf(buffer, (byte)0);
            return Encoding.UTF8.GetString(buffer, 0, end >= 0 ? end : buffer.Length);
        }
        finally
        {
            CFRelease(value);
        }
    }

    private static IntPtr Str(string value) => CFStringCreateWithCString(IntPtr.Zero, value, Utf8);
}
