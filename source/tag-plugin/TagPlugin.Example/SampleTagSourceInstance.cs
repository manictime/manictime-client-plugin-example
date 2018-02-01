using Finkit.ManicTime.Common.TagSources;
using TagPlugin.ImportTags;

namespace TagPlugin
{
    public class SampleTagSourceInstance : BasicTagSourceInstance
    {
        public override void UpdateSettings(ITagSourceSettings settings)
        {
            //throw new NotImplementedException();
        }

        protected override void Update()
        {
            var tags = TagsImporter.GetTags();
            TagSourceCache.Update(InstanceId, tags, null, true);
        }
    }
}