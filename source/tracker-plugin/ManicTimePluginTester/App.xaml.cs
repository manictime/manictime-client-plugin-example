using System;
using System.IO;
using System.Reflection;
using System.Windows;
using Finkit.ManicTime.Common.SystemInformation;
using Finkit.ManicTime.Shared.AppSettings;
using Finkit.ManicTime.Shared.Logging;
using Finkit.ManicTime.Shared.SystemInformation;
using Microsoft.Extensions.Logging;

namespace ManicTimePluginTester
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            SystemInfo.Current = new WindowsSystemInfo(DateTimeOffset.Now, e.Args);
            var appSettings = new GlobalAppSettings();

            appSettings.Defaults = appSettings.Defaults.Update("paths.dataDir", Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)));
            ApplicationLog.SetLogger(new ManicTimeLoggerFactory
            {
                LoggerFactory = new LoggerFactory(new[] { new FileLoggerProvider(appSettings, "log") })
            });
        }
    }
}
