using System;
using System.Threading.Tasks;
using Finkit.ManicTime.Common.TagSources;

namespace TagPlugins.Example.Views
{
    public class TagSourceViewModel : TagSourceSettingsViewModel
    {
        private string _sourceUrl;
        public string SourceUrl
        {
            get 
            {
                return _sourceUrl;
            }
            set
            {
                if (_sourceUrl == value)
                    return;
                _sourceUrl = value;
                OnPropertyChanged("SourceUrl");
            }
        }

        private string _lastSync;
        public string LastSync
        {
            get 
            {
                return _lastSync;
            }
            set
            {
                if (_lastSync == value)
                    return;
                _lastSync = value;
                OnPropertyChanged("LastSync");
            }
        }

        private Exception _error;
        public Exception Error
        {
            get { return _error; }
            set
            {
                _error = value;
                OnPropertyChanged("Error");
                OnPropertyChanged("ShowError");
            }
        }

        public override bool ShowStartingTags
        {
            get { return true; }
        }

        public override void Initialize(ITagSourceSettings settings)
        {
            Settings = settings ?? new ExampleTagSourceSettings();
            SourceUrl = ((ExampleTagSourceSettings) Settings).SourceUrl;
            LastSync = ((ExampleTagSourceSettings) Settings).LastSync ?? "Never.";
            if (string.IsNullOrEmpty(SourceUrl))
                SourceUrl = ExampleTagSourceUriProvider.GetDefaultBase();
        }

        public override Task<bool> BeforeOk()
        {
            var isOk = !string.IsNullOrEmpty(SourceUrl);
            return TaskEx.FromResult(isOk);
        }

        public override Task OnOk()
        {
            var settings = (ExampleTagSourceSettings) Settings;
            settings.SourceUrl = SourceUrl;
            return TaskEx.FromResult(true);
        }

        
    }
}
