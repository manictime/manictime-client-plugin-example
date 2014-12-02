using Finkit.ManicTime.Common.TagSources;
using Machine.Specifications;

namespace TagPlugins.Example.Specs
{
    public class when_getting_data_from_tag_source
    {
        private static TagSourceDataProvider _tagSourceDataProvider;
        private static TagSourceItem[] tags;
        private Establish context = () =>
        {
            _tagSourceDataProvider = new TagSourceDataProvider(TagSourceUriProvider.GetDefaultBase());
        };


        private Because of = () =>
        {
            tags = _tagSourceDataProvider.LoadTagsAsync().Result;
        };

        private It should_return_tag_source = () => tags.ShouldNotBeEmpty();
    }
}
