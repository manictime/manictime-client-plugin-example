using Finkit.ManicTime.Common.TagSources;
using TagPlugin.Settings;

namespace TagPlugin;

[TagSourceSettings(typeof(SampleTagSettings))]
public class SampleTagSource : BasicTagSource
{
    public SampleTagSource(ITagSourceCache tagSourceCache) : base(tagSourceCache)
    {
    }

    protected override BasicTagSourceInstance CreateServerTagSourceInstance(ITagSourceSettings settings,
        string cacheTimestamp)
    {
        return new SampleTagSourceInstance((SampleTagSettings)settings);
    }
}