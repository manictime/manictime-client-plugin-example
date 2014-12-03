using Finkit.ManicTime.Common.TagSources;

namespace TagPlugins.Example
{
    public class ExampleTagSource : BasicTagSource
    {
        public ExampleTagSource(ITagSourceCache tagSourceCache) : base(tagSourceCache)
        {

        }

        protected override BasicTagSourceInstance CreateServerTagSourceInstance(ITagSourceSettings settings, string cacheTimestamp)
        {
            return new ExampleTagSourceInstance((ExampleTagSourceSettings)settings);
        }
    }
}
