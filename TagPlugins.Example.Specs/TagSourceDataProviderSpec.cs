using Finkit.ManicTime.Common.TagSources;
using Machine.Specifications;
// ReSharper disable  UnusedMember.Local
namespace TagPlugins.Example.Specs
{
    public class when_getting_data_from_tag_source
    {
        private static ExampleTagSourceDataProvider _exampleTagSourceDataProvider;
        private static TagSourceItem[] tags;

        private Establish context = () =>
        {
            _exampleTagSourceDataProvider = new ExampleTagSourceDataProvider(ExampleTagSourceUriProvider.GetDefaultBase());
        };


        private Because of = () =>
        {
            tags = _exampleTagSourceDataProvider.LoadTagsAsync().Result;
        };

        private It should_return_tag_source = () => tags.ShouldNotBeEmpty();
    }

    public class when_syncing_data_with_tag_source
    {
        private static ExampleTagSourceDataProvider _exampleTagSourceDataProvider;
        private static string _response;
        private Establish context = () =>
        {
            _exampleTagSourceDataProvider = new ExampleTagSourceDataProvider(ExampleTagSourceUriProvider.GetDefaultBase());
        };

        private Because of = () =>
        {
            _response = _exampleTagSourceDataProvider.SyncTagsAsync(new[] {"Tag1", "Tag2", "Tag3"}).Result;
        };

        private It should_sync_tags = () => _response.ShouldEqual("Synced 3 tags.");

    }
}
