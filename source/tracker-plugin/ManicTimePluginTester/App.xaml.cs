using System.IO;
using System.Reflection;
using System.Windows;
using Finkit.ManicTime.Shared.Logging;

namespace ManicTimePluginTester
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            ApplicationLog.FilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "log.txt");
        }
    }
}
