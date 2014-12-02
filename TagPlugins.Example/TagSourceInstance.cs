using Finkit.ManicTime.Common.TagSources;

namespace TagPlugins.Example
{
    public class TagSourceInstance : BasicTagSourceInstance
    {
        private TagSourceSettings _settings;

        public TagSourceInstance(TagSourceSettings settings)
        {
            _settings = settings;
        }

        protected override void Update()
        {
            var tagSourceDataProvider = new TagSourceDataProvider(_settings.SourceUrl);
            var tags = tagSourceDataProvider.LoadTagsAsync().Result;
            TagSourceCache.Update(InstanceId,tags,null,true);
        }

        public override void UpdateSettings(ITagSourceSettings settings)
        {
            _settings = (TagSourceSettings)settings;
        }
    }
}
