using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ManicTimePluginTester.Cli;

// Same extraction the real macOS tracker uses: NSWorkspace.frontmostApplication for detection,
// NSRunningApplication.localizedName as the process name (ManicTime passes the app's DISPLAY
// name, e.g. "Control Center", not the executable name "ControlCenter"), and the accessibility
// API for the title — AXUIElementCreateApplication(pid) -> AXFocusedWindow -> AXTitle, with the
// process name as fallback. Reading titles requires the terminal running this tester to be
// granted Accessibility permission (System Settings -> Privacy & Security -> Accessibility).
public class MacActiveWindow : IActiveWindow
{
    public static readonly MacActiveWindow Instance = new();

    private const string ApplicationServices =
        "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";
    private const string CoreFoundation =
        "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string ObjC = "/usr/lib/libobjc.dylib";
    private const uint Utf8Encoding = 0x08000100;

    [DllImport(ApplicationServices)]
    private static extern IntPtr AXUIElementCreateApplication(int pid);

    [DllImport(ApplicationServices)]
    private static extern int AXUIElementCopyAttributeValue(IntPtr element, IntPtr attribute, ref IntPtr value);

    [DllImport(ApplicationServices)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool AXIsProcessTrusted();

    [DllImport(ApplicationServices)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool AXIsProcessTrustedWithOptions(IntPtr options);

    [DllImport(CoreFoundation)]
    private static extern IntPtr CFStringCreateWithCString(IntPtr allocator, string value, uint encoding);

    [DllImport(CoreFoundation)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool CFStringGetCString(IntPtr value, byte[] buffer, nint bufferSize, uint encoding);

    [DllImport(CoreFoundation)]
    private static extern void CFRelease(IntPtr value);

    [DllImport(CoreFoundation)]
    private static extern IntPtr CFDictionaryCreate(
        IntPtr allocator,
        IntPtr[] keys,
        IntPtr[] values,
        nint count,
        IntPtr keyCallbacks,
        IntPtr valueCallbacks);

    [DllImport(ObjC)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjC)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, int arg);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern int objc_msgSend_Int(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern IntPtr dlopen(string path, int mode);

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern IntPtr dlsym(IntPtr handle, string name);

    // NSWorkspace/NSRunningApplication live in AppKit, which a console process does not load on
    // its own — without this, objc_getClass returns null and detection silently degrades.
    private static readonly IntPtr AppKit = dlopen("/System/Library/Frameworks/AppKit.framework/AppKit", 0);

    private static readonly IntPtr FocusedWindowAttr = CFStringCreateWithCString(IntPtr.Zero, "AXFocusedWindow", Utf8Encoding);
    private static readonly IntPtr MainWindowAttr = CFStringCreateWithCString(IntPtr.Zero, "AXMainWindow", Utf8Encoding);
    private static readonly IntPtr TitleAttr = CFStringCreateWithCString(IntPtr.Zero, "AXTitle", Utf8Encoding);
    private static bool _trusted;
    private static bool _trustWarned;

    public (int ProcessId, IntPtr Handle) GetForegroundWindow()
    {
        // frontmostApplication returns an autoreleased object; without a pool on this thread it
        // would leak on every poll.
        IntPtr pool = AutoreleasePool();
        try
        {
            IntPtr workspace = objc_msgSend_IntPtr(objc_getClass("NSWorkspace"), sel_registerName("sharedWorkspace"));
            IntPtr application = objc_msgSend_IntPtr(workspace, sel_registerName("frontmostApplication"));
            int processId = application == IntPtr.Zero ? 0 : objc_msgSend_Int(application, sel_registerName("processIdentifier"));
            return (processId, IntPtr.Zero);
        }
        finally
        {
            Drain(pool);
        }
    }

    public IntPtr GetMainWindowHandle(Process process) => IntPtr.Zero;

    // ManicTime passes NSRunningApplication.localizedName as ApplicationInfo.ProcessName — often
    // different from the executable name (spaces, localization). localizedName can be empty (some
    // apps ship an empty CFBundleDisplayName), so ManicTime falls back to the bundle name, then
    // the executable name, then the bundle identifier — mirrored here.
    public string GetProcessName(Process process)
    {
        IntPtr pool = AutoreleasePool();
        try
        {
            IntPtr application = objc_msgSend_IntPtr(
                objc_getClass("NSRunningApplication"),
                sel_registerName("runningApplicationWithProcessIdentifier:"),
                process.Id);
            if (application == IntPtr.Zero)
                return process.ProcessName;

            string? localizedName = ReadCfString(objc_msgSend_IntPtr(application, sel_registerName("localizedName")));
            if (!string.IsNullOrEmpty(localizedName))
                return localizedName;

            string? bundleName = FileNameWithoutAppExtension(ReadUrlPath(application, "bundleURL"));
            if (!string.IsNullOrEmpty(bundleName))
                return bundleName;

            string? executableName = FileNameWithoutAppExtension(ReadUrlPath(application, "executableURL"));
            if (!string.IsNullOrEmpty(executableName))
                return executableName;

            string? bundleIdentifier =
                ReadCfString(objc_msgSend_IntPtr(application, sel_registerName("bundleIdentifier")));
            return string.IsNullOrEmpty(bundleIdentifier) ? process.ProcessName : bundleIdentifier;
        }
        finally
        {
            Drain(pool);
        }
    }

    private static string? ReadUrlPath(IntPtr application, string urlSelector)
    {
        IntPtr url = objc_msgSend_IntPtr(application, sel_registerName(urlSelector));
        return url == IntPtr.Zero
            ? null
            : ReadCfString(objc_msgSend_IntPtr(url, sel_registerName("path")));
    }

    private static string? FileNameWithoutAppExtension(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        string name = Path.GetFileName(path.TrimEnd('/'));
        return name.EndsWith(".app", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
    }

    public string? GetWindowTitle(Process process, IntPtr handle)
    {
        WarnIfNotTrusted();

        IntPtr application = AXUIElementCreateApplication(process.Id);
        if (application == IntPtr.Zero)
            return null;
        try
        {
            IntPtr window = Copy(application, FocusedWindowAttr);
            if (window == IntPtr.Zero)
                window = Copy(application, MainWindowAttr);
            if (window == IntPtr.Zero)
                return null;
            try
            {
                return ReadString(window, TitleAttr);
            }
            finally
            {
                CFRelease(window);
            }
        }
        finally
        {
            CFRelease(application);
        }
    }

    private static IntPtr Copy(IntPtr element, IntPtr attribute)
    {
        IntPtr value = IntPtr.Zero;
        return AXUIElementCopyAttributeValue(element, attribute, ref value) == 0 ? value : IntPtr.Zero;
    }

    private static string? ReadString(IntPtr element, IntPtr attribute)
    {
        IntPtr value = Copy(element, attribute);
        if (value == IntPtr.Zero)
            return null;
        try
        {
            return ReadCfString(value);
        }
        finally
        {
            CFRelease(value);
        }
    }

    private static string? ReadCfString(IntPtr value)
    {
        if (value == IntPtr.Zero)
            return null;
        byte[] buffer = new byte[2048];
        if (!CFStringGetCString(value, buffer, buffer.Length, Utf8Encoding))
            return null;
        int end = Array.IndexOf(buffer, (byte)0);
        return Encoding.UTF8.GetString(buffer, 0, end >= 0 ? end : buffer.Length);
    }

    private static IntPtr AutoreleasePool() => objc_msgSend_IntPtr(
        objc_msgSend_IntPtr(objc_getClass("NSAutoreleasePool"), sel_registerName("alloc")),
        sel_registerName("init"));

    private static void Drain(IntPtr pool) => objc_msgSend_IntPtr(pool, sel_registerName("drain"));

    // Asks macOS to show the standard accessibility permission dialog when permission is missing;
    // the silent AXIsProcessTrusted() never prompts. Checked once per run.
    private static void WarnIfNotTrusted()
    {
        if (_trusted || _trustWarned)
            return;

        IntPtr promptKey = GetDataSymbol(ApplicationServices, "kAXTrustedCheckOptionPrompt");
        IntPtr trueValue = GetDataSymbol(CoreFoundation, "kCFBooleanTrue");
        if (promptKey == IntPtr.Zero || trueValue == IntPtr.Zero)
        {
            _trusted = AXIsProcessTrusted();
        }
        else
        {
            IntPtr options = CFDictionaryCreate(
                IntPtr.Zero, new[] { promptKey }, new[] { trueValue }, 1, IntPtr.Zero, IntPtr.Zero);
            try
            {
                _trusted = AXIsProcessTrustedWithOptions(options);
            }
            finally
            {
                CFRelease(options);
            }
        }

        if (!_trusted)
        {
            _trustWarned = true;
            Console.WriteLine("WARNING: this process is not trusted for Accessibility — window titles will be empty. " +
                "macOS should have shown a permission dialog; approve it (or enable the app under " +
                "System Settings -> Privacy & Security -> Accessibility) and run again.");
        }
    }

    // kAXTrustedCheckOptionPrompt and kCFBooleanTrue are data symbols, not functions.
    private static IntPtr GetDataSymbol(string library, string name)
    {
        IntPtr handle = dlopen(library, 0);
        IntPtr symbol = handle == IntPtr.Zero ? IntPtr.Zero : dlsym(handle, name);
        return symbol == IntPtr.Zero ? IntPtr.Zero : Marshal.ReadIntPtr(symbol);
    }
}
