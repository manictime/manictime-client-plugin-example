using Finkit.ManicTime.Client.Main.Views;
using Finkit.ManicTime.Common.ServiceProviders.PluginCommands;
using TagPlugins.Example.Views;

namespace TagPlugins.Example
{
    public class TagPluginCommand : PluginCommand
    {
        public TagPluginCommand()
        {
            CanExecute = true;
        }
       
        public override string Name
        {
            get { return "Sync"; }
        }

        public override void Execute()
        {
            ViewHelper.ShowViewModelWindow<TagSyncViewModel>(null);
        }
    }
}
