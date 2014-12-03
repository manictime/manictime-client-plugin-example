using Finkit.ManicTime.Common.TagSources;

namespace TagPlugins.Example
{
    public class ExampleTagSourceSettings : ITagSourceSettings
    {
        public string SourceUrl { get; set; }
        public string LastSync { get; set; }
    }
}
