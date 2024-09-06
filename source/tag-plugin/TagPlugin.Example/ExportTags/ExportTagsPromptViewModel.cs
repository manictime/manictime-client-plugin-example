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

    public bool AutoTagTimelineExists { get; private set; }
    public bool ExportTags { get; set; } = true;

    public DelegateCommand OkCommand { get; set; }
    public DelegateCommand CancelCommand { get; set; }

    public ExportTagsPromptViewModel()
    {
        MessagePrompt = "What would you like to export?";

        OkCommand = new DelegateCommand(OnOk) { CanExecute = true };
        CancelCommand = new DelegateCommand(OnCancel) { CanExecute = true };
    }

    public void Initialize(bool autoTagTimelineExists)
    {
        AutoTagTimelineExists = autoTagTimelineExists;
    }

    private void OnOk()
    {
        OnCloseRequested(MessageButtons.Ok);
    }

    private void OnCancel()
    {
        OnCloseRequested(MessageButtons.Cancel);
    }
}