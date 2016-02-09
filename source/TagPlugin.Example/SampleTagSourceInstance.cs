using Finkit.ManicTime.Common.TagSources;

namespace TagPlugin
{
    public class SampleTagSourceInstance : BasicTagSourceInstance
    {
        public override void UpdateSettings(ITagSourceSettings settings)
        {
            //throw new NotImplementedException();
        }

        protected override async void Update()
        {
            var tags = await TagsImporter.GetTags();
            TagSourceCache.Update(InstanceId, tags, null, true);
        }
    }
}