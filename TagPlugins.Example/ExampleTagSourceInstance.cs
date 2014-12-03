using Finkit.ManicTime.Common.TagSources;

namespace TagPlugins.Example
{
    public class ExampleTagSourceInstance : BasicTagSourceInstance
    {
        private ExampleTagSourceSettings _settings;

        public ExampleTagSourceInstance(ExampleTagSourceSettings settings)
        {
            _settings = settings;
        }

        protected override void Update()
        {
            var tagSourceDataProvider = new ExampleTagSourceDataProvider(_settings.SourceUrl);
            var tags = tagSourceDataProvider.LoadTagsAsync().Result;
            TagSourceCache.Update(InstanceId,tags,null,true);
        }

        public override void UpdateSettings(ITagSourceSettings settings)
        {
            _settings = (ExampleTagSourceSettings)settings;
        }
    }
}
