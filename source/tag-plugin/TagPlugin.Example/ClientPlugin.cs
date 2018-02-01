using Finkit.ManicTime.Shared.Plugins;
using Finkit.ManicTime.Shared.Plugins.ServiceProviders.Manager;
using Finkit.ManicTime.Shared.Plugins.ServiceProviders.PluginCommands;
using TagPlugin.ExportTags;
using TagPlugins.Core.Commands;

namespace TagPlugin
{
    [Plugin]
    public class ClientPlugin
    {
        public static string Id = "ManicTime.TagSource.SampleTagPlugin";
        public static string HiddenTagLabel = ":samplePlugin";

        public ClientPlugin(IPluginCommandRegistry pluginCommandRegistry, PluginContext pluginContext)
        {
            //add Send tags command button
            pluginCommandRegistry.RegisterCommand<ExportTagsCommand>(pluginContext.Spec);

            //add settings command button
            pluginCommandRegistry.RegisterCommand<ConfigurePluginCommand>(pluginContext.Spec);
        }
    }
}