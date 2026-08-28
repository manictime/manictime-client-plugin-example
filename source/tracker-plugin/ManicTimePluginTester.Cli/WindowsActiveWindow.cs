using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ManicTimePluginTester.Cli;

// Same extraction the real Windows tracker uses: GetForegroundWindow for detection,
// GetWindowTextLength + GetWindowText for the title with Process.MainWindowTitle as fallback
// (see ManicTime's GetWindowTitleApplicationHandler).
public class WindowsActiveWindow : IActiveWindow
{
    public static readonly WindowsActiveWindow Instance = new();

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    (int ProcessId, IntPtr Handle) IActiveWindow.GetForegroundWindow()
    {
        IntPtr handle = GetForegroundWindow();
        if (handle == IntPtr.Zero)
            return (0, IntPtr.Zero);
        GetWindowThreadProcessId(handle, out int processId);
        return (processId, handle);
    }

    public IntPtr GetMainWindowHandle(Process process) => process.MainWindowHandle;

    public string? GetWindowTitle(Process process, IntPtr handle)
    {
        if (handle != IntPtr.Zero)
        {
            int length = GetWindowTextLength(handle);
            StringBuilder buffer = new(length + 1);
            if (GetWindowText(handle, buffer, buffer.Capacity) != 0)
                return buffer.ToString();
        }
        return process.MainWindowTitle;
    }
}
