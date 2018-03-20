using System.Threading.Tasks;
using Finkit.ManicTime.Common.TagSources;

namespace TagPlugin.Settings
{
    public class SettingsViewModel : TagSourceSettingsViewModel
    {
        public override bool ShowStartingTags => true;

        private bool _useCustomNotes;
        public bool UseCustomNotes
        {
            get { return _useCustomNotes; }
            set
            {
                if (_useCustomNotes == value)
                    return;
                _useCustomNotes = value;
                OnPropertyChanged("UseCustomNotes");
            }
        }

        private string _customNotes;
        public string CustomNotes
        {
            get { return _customNotes; }
            set
            {
                if (_customNotes == value)
                    return;
                _customNotes = value;
                OnPropertyChanged("CustomNotes");
            }
        }

        public override void Initialize(ITagSourceSettings settings)
        {
            var sampleTagSettings = (SampleTagSettings)settings ?? new SampleTagSettings();
            Settings = sampleTagSettings;

            CustomNotes = sampleTagSettings.CustomNotes;
            UseCustomNotes = CustomNotes != null;
        }

        public override Task<bool> BeforeOk()
        {
            return Task.FromResult(true);
        }

        public override Task OnOk()
        {
            var sampleTagSettings = (SampleTagSettings)Settings;
            sampleTagSettings.CustomNotes = UseCustomNotes
                    ? CustomNotes 
                    : null;
            return Task.FromResult(true);
        }
    }
}