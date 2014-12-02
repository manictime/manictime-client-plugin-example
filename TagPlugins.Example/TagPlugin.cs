using Finkit.ManicTime.Common.ServiceProviders.Manager;
using Finkit.ManicTime.Common.ServiceProviders.PluginCommands;
using Finkit.ManicTime.Plugins;

namespace TagPlugins.Example
{
    [Plugin]
    public class TagPlugin
    {
        public TagPlugin(IPluginCommandRegistry pluginCommandRegistry, PluginContext pluginContext)
        {

            pluginCommandRegistry.RegisterCommand<TagPluginCommand>(pluginContext.Spec);   
        }
    }
}
