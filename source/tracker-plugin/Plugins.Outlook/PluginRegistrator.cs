using Finkit.ManicTime.Shared.Plugins;
using ManicTime.Client.Tracker.EventTracking.Publishers.ApplicationTracking;

namespace Plugins.Outlook
{
    // register the plugin. ManicTime will look for all dlls and load those with Plugin attribute.
    [Plugin]
    public class PluginRegistrator
    {
        public PluginRegistrator(IServiceRegistry serviceRegistry)
        {
            serviceRegistry.RegisterInstance<IDocumentRetreiver>(new OutlookRetreiver());
        }
    }
}
