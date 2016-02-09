using System.Threading.Tasks;
using Finkit.ManicTime.Common.TagSources;

namespace TagPlugin.TemplateSettings
{
    public class SettingsViewModel : TagSourceSettingsViewModel
    {
        public override bool ShowStartingTags => true;

        public override void Initialize(ITagSourceSettings settings)
        {
        }

        public override async Task<bool> BeforeOk()
        {
            return true;
        }

        public override Task OnOk()
        {
            return Task.FromResult(true);
        }
    }
}