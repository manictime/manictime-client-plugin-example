using Finkit.ManicTime.Client.Main.Views;

namespace TagPlugin.ExportTags;

public class ExportTagsPromptViewModel : ViewModel
{
    private string _messagePrompt;
    public string MessagePrompt
    {
        get => _messagePrompt;
        set
        {
            if (_messagePrompt == value)
                return;
            _messagePrompt = value;
            OnPropertyChanged(nameof(MessagePrompt));
        }
    }
    
    public DelegateCommand YesCommand { get; set; }
    public DelegateCommand NoCommand { get; set; }

    public ExportTagsPromptViewModel()
    {
        MessagePrompt = "Do you really want to export tags?";

        YesCommand = new DelegateCommand(OnYes) { CanExecute = true };
        NoCommand = new DelegateCommand(OnNo) { CanExecute = true };
    }

    private void OnYes()
    {
        OnCloseRequested(MessageButtons.Yes);
    }

    private void OnNo()
    {
        OnCloseRequested(MessageButtons.No);
    }
}