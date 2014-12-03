using Finkit.ManicTime.Common.ServiceProviders.Manager;
using Finkit.ManicTime.Common.ServiceProviders.PluginCommands;
using Finkit.ManicTime.Plugins;

namespace TagPlugins.Example
{
    [Plugin]
    public class ExampleTagPlugin
    {
        public const string Id = "TagPlugins.Example";

        public ExampleTagPlugin(IPluginCommandRegistry pluginCommandRegistry, PluginContext pluginContext)
        {

            pluginCommandRegistry.RegisterCommand<ExampleTagPluginCommand>(pluginContext.Spec);   
        }
    }
}
