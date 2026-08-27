using System.Diagnostics;
using ManicTime.Client.Tracker.EventTracking.Publishers.ApplicationTracking;

namespace ManicTimePluginTester.Cli;

// OS-specific window detection lives in WindowsActiveWindow / MacActiveWindow / LinuxActiveWindow.
// Each mirrors what the real ManicTime tracker reads on that OS, so retrievers get the same
// ProcessName/WindowTitle data here as they would inside ManicTime.
public static class ForegroundWindow
{
    public static ApplicationInfo? GetCurrent()
    {
        (int processId, IntPtr handle) = ActiveWindow().GetForegroundWindow();
        return processId > 0 ? Get(processId, handle) : null;
    }

    // Watch a specific process regardless of which window has focus.
    public static ApplicationInfo GetByProcessId(int processId) => Get(processId, windowHandle: null);

    private static IActiveWindow ActiveWindow()
    {
        if (OperatingSystem.IsWindows())
            return WindowsActiveWindow.Instance;
        if (OperatingSystem.IsMacOS())
            return MacActiveWindow.Instance;
        if (OperatingSystem.IsLinux())
            return LinuxActiveWindow.Instance;
        throw new PlatformNotSupportedException("Window tracking is implemented for Windows, macOS and Linux.");
    }

    // Builds the ApplicationInfo handed to document retrievers. ProcessName and WindowTitle
    // match the real tracker; the file/product fields are an approximation (on macOS the real
    // tracker passes the .app bundle path and mostly empty product info).
    private static ApplicationInfo Get(int processId, IntPtr? windowHandle)
    {
        IActiveWindow activeWindow = ActiveWindow();
        using Process process = Process.GetProcessById(processId);
        IntPtr handle = windowHandle ?? activeWindow.GetMainWindowHandle(process);
        string processName = activeWindow.GetProcessName(process);
        string title = activeWindow.GetWindowTitle(process, handle) ?? processName;

        string? filename = null;
        FileVersionInfo? versionInfo = null;
        try
        {
            filename = process.MainModule?.FileName;
            if (filename != null)
                versionInfo = FileVersionInfo.GetVersionInfo(filename);
        }
        catch
        {
            // Access to another process's main module can be denied (elevation, protected processes).
        }

        return new ApplicationInfo(
            processId,
            processName,
            filename,
            versionInfo?.FileDescription,
            versionInfo?.ProductName,
            versionInfo?.ProductVersion,
            versionInfo?.CompanyName,
            versionInfo?.ProductMajorPart,
            versionInfo?.ProductMinorPart,
            handle,
            title);
    }
}

public interface IActiveWindow
{
    (int ProcessId, IntPtr Handle) GetForegroundWindow();
    IntPtr GetMainWindowHandle(Process process);
    string? GetWindowTitle(Process process, IntPtr handle);

    // What ManicTime reports as ApplicationInfo.ProcessName — the executable name on Windows,
    // the app's localized display name on macOS.
    string GetProcessName(Process process) => process.ProcessName;
}
