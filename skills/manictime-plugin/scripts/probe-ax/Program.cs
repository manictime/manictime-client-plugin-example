using System.Runtime.InteropServices;
using System.Text;

// Dumps the accessibility tree of a process: role/subrole/title/description/value/identifier/help
// per node, indented by depth. Usage: probe <pid> [maxDepth]
const string Ax = "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";
const string Cf = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
const uint Utf8 = 0x08000100;

[DllImport(Ax)] static extern IntPtr AXUIElementCreateApplication(int pid);
[DllImport(Ax)] static extern int AXUIElementCopyAttributeValue(IntPtr element, IntPtr attr, ref IntPtr value);
[DllImport(Ax)] static extern int AXUIElementSetAttributeValue(IntPtr element, IntPtr attr, IntPtr value);
[DllImport(Ax)] [return: MarshalAs(UnmanagedType.I1)] static extern bool AXIsProcessTrustedWithOptions(IntPtr options);
[DllImport(Cf)] static extern IntPtr CFStringCreateWithCString(IntPtr alloc, string s, uint enc);
[DllImport(Cf)] [return: MarshalAs(UnmanagedType.I1)] static extern bool CFStringGetCString(IntPtr s, byte[] buf, nint size, uint enc);
[DllImport(Cf)] static extern nint CFArrayGetCount(IntPtr array);
[DllImport(Cf)] static extern IntPtr CFArrayGetValueAtIndex(IntPtr array, nint index);
[DllImport(Cf)] static extern nint CFGetTypeID(IntPtr cf);
[DllImport(Cf)] static extern nint CFStringGetTypeID();
[DllImport(Cf)] static extern void CFRelease(IntPtr cf);
[DllImport(Cf)] static extern IntPtr CFDictionaryCreate(IntPtr alloc, IntPtr[] keys, IntPtr[] values, nint count, IntPtr keyCb, IntPtr valueCb);
[DllImport("/usr/lib/libSystem.dylib")] static extern IntPtr dlopen(string path, int mode);
[DllImport("/usr/lib/libSystem.dylib")] static extern IntPtr dlsym(IntPtr handle, string name);

IntPtr Str(string s) => CFStringCreateWithCString(IntPtr.Zero, s, Utf8);
IntPtr DataSymbol(string library, string name)
{
    IntPtr symbol = dlsym(dlopen(library, 0), name);
    return symbol == IntPtr.Zero ? IntPtr.Zero : Marshal.ReadIntPtr(symbol);
}

// Values from AXUIElementCopyAttributeValue are owned by the caller — released here.
// The strings are chosen by the target app (untrusted): control characters are escaped so a
// malicious accessible name can't inject terminal escape sequences into this dump.
string? Read(IntPtr el, IntPtr attr)
{
    IntPtr v = IntPtr.Zero;
    if (AXUIElementCopyAttributeValue(el, attr, ref v) != 0 || v == IntPtr.Zero)
        return null;
    try
    {
        if (CFGetTypeID(v) != CFStringGetTypeID())
            return null;
        byte[] buf = new byte[2048];
        if (!CFStringGetCString(v, buf, buf.Length, Utf8))
            return null;
        int end = Array.IndexOf(buf, (byte)0);
        string raw = Encoding.UTF8.GetString(buf, 0, end >= 0 ? end : buf.Length);
        StringBuilder safe = new(raw.Length);
        foreach (char c in raw)
            safe.Append(!char.IsControl(c) || c == '\t' ? c : $"\\u{(int)c:x4}");
        return safe.ToString();
    }
    finally
    {
        CFRelease(v);
    }
}

int maxDepth = 25;
if (args.Length == 0 || !int.TryParse(args[0], out int pid)
    || (args.Length > 1 && (!int.TryParse(args[1], out maxDepth) || maxDepth < 1)))
{
    Console.Error.WriteLine("Usage: probe-ax <pid> [maxDepth>=1]   (dumps the accessibility tree of the process)");
    return 1;
}
IntPtr role = Str("AXRole"), subrole = Str("AXSubrole"), title = Str("AXTitle");
IntPtr desc = Str("AXDescription"), val = Str("AXValue"), ident = Str("AXIdentifier"), help = Str("AXHelp"), children = Str("AXChildren");
IntPtr cfTrue = DataSymbol(Cf, "kCFBooleanTrue");

// Ask macOS to show the Accessibility permission dialog when it is missing; without the
// permission the tree comes back empty and looks like the app exposes nothing.
IntPtr promptKey = DataSymbol(Ax, "kAXTrustedCheckOptionPrompt");
IntPtr options = CFDictionaryCreate(IntPtr.Zero, new[] { promptKey }, new[] { cfTrue }, 1, IntPtr.Zero, IntPtr.Zero);
bool trusted = AXIsProcessTrustedWithOptions(options);
CFRelease(options);
if (!trusted)
{
    Console.Error.WriteLine(
        "NOT TRUSTED for Accessibility — the tree below is empty/incomplete, NOT proof the app " +
        "exposes nothing. macOS should have shown a permission dialog; approve it (or enable this " +
        "terminal under System Settings -> Privacy & Security -> Accessibility) and run again.");
}

IntPtr app = AXUIElementCreateApplication(pid);

// Electron/Chromium apps expose a stub tree until an assistive client announces itself.
IntPtr manual = Str("AXManualAccessibility");
AXUIElementSetAttributeValue(app, manual, cfTrue);
Thread.Sleep(1500); // give the tree time to materialize

int printed = 0;
bool truncated = false;
void Dump(IntPtr el, int depth)
{
    if (printed >= 4000)
    {
        truncated = true;
        return;
    }
    if (depth > maxDepth)
        return;
    string line = $"{new string(' ', depth * 2)}{Read(el, role)}";
    string? s;
    if ((s = Read(el, subrole)) != null) line += $" subrole='{s}'";
    if ((s = Read(el, title)) != null && s.Length > 0) line += $" title='{s}'";
    if ((s = Read(el, desc)) != null && s.Length > 0) line += $" desc='{s}'";
    if ((s = Read(el, ident)) != null && s.Length > 0) line += $" id='{s}'";
    if ((s = Read(el, help)) != null && s.Length > 0) line += $" help='{s}'";
    if ((s = Read(el, val)) != null && s.Length > 0 && s.Length < 200) line += $" value='{s}'";
    Console.WriteLine(line);
    printed++;

    IntPtr kids = IntPtr.Zero;
    if (AXUIElementCopyAttributeValue(el, children, ref kids) != 0 || kids == IntPtr.Zero)
        return;
    try
    {
        nint count = CFArrayGetCount(kids);
        for (nint i = 0; i < count && i < 100; i++)
            Dump(CFArrayGetValueAtIndex(kids, i), depth + 1);
        if (count > 100)
            Console.WriteLine($"{new string(' ', (depth + 1) * 2)}... ({count - 100} more children not shown)");
    }
    finally
    {
        CFRelease(kids);
    }
}

Dump(app, 0);
CFRelease(app);
Console.WriteLine(truncated
    ? $"[{printed} nodes — TRUNCATED at the 4000-node budget, the tree continues]"
    : $"[{printed} nodes]");
if (printed <= 1)
    Console.Error.WriteLine("Almost nothing in the tree — is the pid right and the process running? " +
        "For Electron apps, run again: the first run only sends the accessibility wake.");
return 0;
