using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ManicTimePluginTester.Cli;

// Reads the same X11 properties the real Linux tracker reads: _NET_ACTIVE_WINDOW for detection,
// _NET_WM_PID for the owning process and _NET_WM_NAME (fallback WM_NAME) for the title
// (see ManicTime's X11 GetApplicationTitleApplicationHandler). X11 and XWayland apps only —
// native-Wayland windows are handled in ManicTime by per-compositor code this tester doesn't have.
public class LinuxActiveWindow : IActiveWindow
{
    public static readonly LinuxActiveWindow Instance = new();

    private const string X11 = "libX11.so.6";

    [DllImport(X11)]
    private static extern IntPtr XOpenDisplay(string? display);

    [DllImport(X11)]
    private static extern IntPtr XDefaultRootWindow(IntPtr display);

    [DllImport(X11)]
    private static extern IntPtr XInternAtom(IntPtr display, string atomName, bool onlyIfExists);

    [DllImport(X11)]
    private static extern int XGetWindowProperty(
        IntPtr display,
        IntPtr window,
        IntPtr property,
        IntPtr offset,
        IntPtr length,
        bool delete,
        IntPtr requestedType,
        out IntPtr actualType,
        out int actualFormat,
        out IntPtr itemCount,
        out IntPtr bytesAfter,
        out IntPtr data);

    [DllImport(X11)]
    private static extern int XFree(IntPtr data);

    private delegate int XErrorHandler(IntPtr display, IntPtr errorEvent);

    [DllImport(X11)]
    private static extern IntPtr XSetErrorHandler(XErrorHandler handler);

    private const IntPtr AnyPropertyType = 0;

    // Kept alive for the process lifetime so the native callback stays valid.
    private static readonly XErrorHandler IgnoreErrors = (_, _) => 0;

    private IntPtr _display;

    private IntPtr Display
    {
        get
        {
            if (_display == IntPtr.Zero)
            {
                // Without a handler Xlib's default TERMINATES the process on any error — e.g. a
                // BadWindow when the active window closes between two property reads.
                XSetErrorHandler(IgnoreErrors);
                _display = XOpenDisplay(null);
            }
            return _display;
        }
    }

    public (int ProcessId, IntPtr Handle) GetForegroundWindow()
    {
        if (Display == IntPtr.Zero)
        {
            Console.WriteLine("WARNING: cannot open X display — window tracking needs X11 or XWayland.");
            return (0, IntPtr.Zero);
        }

        IntPtr window = ReadWindow(XDefaultRootWindow(Display), "_NET_ACTIVE_WINDOW");
        if (window == IntPtr.Zero)
            return (0, IntPtr.Zero);
        int processId = (int)ReadCardinal(window, "_NET_WM_PID");
        return (processId, window);
    }

    public IntPtr GetMainWindowHandle(Process process)
    {
        // X11 has no "main window of a process" — find the first client window owned by the pid.
        if (Display == IntPtr.Zero)
            return IntPtr.Zero;
        foreach (IntPtr window in ReadWindowList(XDefaultRootWindow(Display), "_NET_CLIENT_LIST"))
            if ((int)ReadCardinal(window, "_NET_WM_PID") == process.Id)
                return window;
        return IntPtr.Zero;
    }

    public string? GetWindowTitle(Process process, IntPtr handle)
    {
        if (Display == IntPtr.Zero || handle == IntPtr.Zero)
            return process.ProcessName;
        return ReadUtf8(handle, "_NET_WM_NAME") ?? ReadUtf8(handle, "WM_NAME") ?? process.ProcessName;
    }

    private IntPtr ReadWindow(IntPtr window, string property) =>
        (IntPtr)ReadCardinal(window, property);

    private long ReadCardinal(IntPtr window, string property)
    {
        long[] values = ReadLongs(window, property, maxItems: 1);
        return values.Length > 0 ? values[0] : 0;
    }

    private IntPtr[] ReadWindowList(IntPtr window, string property) =>
        ReadLongs(window, property, maxItems: 4096).Select(v => (IntPtr)v).ToArray();

    // Format-32 properties are returned as native longs (8 bytes on 64-bit).
    private long[] ReadLongs(IntPtr window, string property, int maxItems)
    {
        if (!TryGetProperty(window, property, maxItems, out IntPtr data, out long itemCount))
            return Array.Empty<long>();
        try
        {
            long[] values = new long[itemCount];
            for (int i = 0; i < itemCount; i++)
                values[i] = Marshal.ReadIntPtr(data, i * IntPtr.Size);
            return values;
        }
        finally
        {
            XFree(data);
        }
    }

    private string? ReadUtf8(IntPtr window, string property)
    {
        if (!TryGetProperty(window, property, maxItems: 1024, out IntPtr data, out long itemCount))
            return null;
        try
        {
            byte[] bytes = new byte[itemCount];
            Marshal.Copy(data, bytes, 0, (int)itemCount);
            string value = Encoding.UTF8.GetString(bytes);
            return value.Length > 0 ? value : null;
        }
        finally
        {
            XFree(data);
        }
    }

    private bool TryGetProperty(IntPtr window, string property, int maxItems, out IntPtr data, out long itemCount)
    {
        data = IntPtr.Zero;
        itemCount = 0;
        IntPtr atom = XInternAtom(Display, property, onlyIfExists: true);
        if (atom == IntPtr.Zero)
            return false;
        int status = XGetWindowProperty(
            Display, window, atom, IntPtr.Zero, maxItems, false, AnyPropertyType,
            out _, out _, out IntPtr items, out _, out data);
        if (status != 0 || data == IntPtr.Zero)
            return false;
        itemCount = items;
        if (itemCount == 0)
        {
            XFree(data);
            return false;
        }
        return true;
    }
}
