using ManicTime;
using ManicTime.Client.Tracker.EventTracking.Publishers.ApplicationTracking;
using Microsoft.Extensions.DependencyInjection;

namespace Plugins.Notepad;

public class PluginServiceConfigurator : IServiceConfigurator
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDocumentRetreiver, NotepadFileRetreiver>();
    }
}