using Finkit.ManicTime.Common.TagSources;
using ManicTime;
using Microsoft.Extensions.DependencyInjection;
using TagPlugin.ExportTags;
using TagPlugin.Settings;

namespace TagPlugin;

public class ClientPluginServiceConfigurator : IServiceConfigurator
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<TagsExporter>();
        services.AddSingleton<ITagSource, SampleTagSource>();
        services.AddSingleton<ITagSourceSettingsViewModel, SettingsViewModel>();
    }
}