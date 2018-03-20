using System.Linq;
using Finkit.ManicTime.Common.TagSources;
using TagPlugin.ImportTags;
using TagPlugin.Settings;

namespace TagPlugin
{
    public class SampleTagSourceInstance : BasicTagSourceInstance
    {
        private string _customNotes;

        public SampleTagSourceInstance(SampleTagSettings sampleTagSettings)
        {
            _customNotes = sampleTagSettings.CustomNotes;
        }

        public override void UpdateSettings(ITagSourceSettings settings)
        {
            var sampleTagSettings = (SampleTagSettings)settings ?? new SampleTagSettings();
            _customNotes = sampleTagSettings.CustomNotes;
        }

        protected override void Update()
        {
            var tags = TagsImporter.GetTags().ToArray();
            if (_customNotes != null)
            {
                tags = tags.Select(x =>
                {
                    x.Notes = _customNotes;
                    return x;
                }).ToArray();
            }

            TagSourceCache.Update(InstanceId, tags, null, true);
        }
    }
}