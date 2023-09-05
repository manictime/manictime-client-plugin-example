using Finkit.ManicTime.Client.Main.Views;

namespace TimelinePlugins.Example.CustomPrompt;
public class PromptViewModel : ViewModel
{
    public DelegateCommand OkCommand { get; set; }

    public PromptViewModel()
    {
        OkCommand = new DelegateCommand(OnOk) { CanExecute = true };
    }

    private void OnOk()
    {
        OnCloseRequested(MessageButtons.Ok);
    }
}
