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
            Settings = settings ?? new TagSourceSettings();
            SourceUrl = ((TagSourceSettings) Settings).SourceUrl;
            if (string.IsNullOrEmpty(SourceUrl))
                SourceUrl = TagSourceUriProvider.GetDefaultBase();
        }

        public override Task<bool> BeforeOk()
        {
            var isOk = !string.IsNullOrEmpty(SourceUrl);
            return TaskEx.FromResult(isOk);
        }

        public override Task OnOk()
        {
            var settings = (TagSourceSettings) Settings;
            settings.SourceUrl = SourceUrl;
            return TaskEx.FromResult(true);
        }

        
    }
}
