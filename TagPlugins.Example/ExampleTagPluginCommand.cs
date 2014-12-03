using Finkit.ManicTime.Client.Main.Views;
using Finkit.ManicTime.Common.ServiceProviders.PluginCommands;
using TagPlugins.Example.Views;

namespace TagPlugins.Example
{
    public class ExampleTagPluginCommand : PluginCommand
    {
        public ExampleTagPluginCommand()
        {
            CanExecute = true;
        }
       
        public override string Name
        {
            get { return "Sync tags"; }
        }

        public override void Execute()
        {
            ViewHelper.ShowViewModelWindow<TagSyncViewModel>(null);
        }
    }
}
