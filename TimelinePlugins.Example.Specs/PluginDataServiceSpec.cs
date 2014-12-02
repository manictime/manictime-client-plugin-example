
using System;
using System.Net.Http;
using Machine.Specifications;
using TimelinePlugins.Example.DataSourceDto;

namespace TimelinePlugins.Example.Specs
{
    
    public class when_getting_timeline_data
    {
        private static PluginDataService _pluginDataService;
        private static TimelinePluginExampleDto _response;

        private Establish context = () =>
        {
           _pluginDataService = new PluginDataService();
        };

        private Because of = () =>
        {
            _response = _pluginDataService.GetExampleDataAsync(DateTime.Now).Result;
        };

        private It should_return_response = () =>
        {
            _response.ShouldNotBeNull();
            
        };


    }
}
