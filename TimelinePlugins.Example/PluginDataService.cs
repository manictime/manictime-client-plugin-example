using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TimelinePlugins.Example.DataSourceDto;

namespace TimelinePlugins.Example
{
    //data service for retrieving timeline data
    public class PluginDataService
    {
        private readonly HttpClient _httpClient;
        public PluginDataService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<TimelinePluginExampleDto> GetExampleDataAsync(DateTimeOffset from, string baseUri = null)
        {
            var response = await _httpClient.GetStringAsync(PluginUriProvider.GetWithParameters(from));
            return JsonConvert.DeserializeObject<TimelinePluginExampleDto>(response);
        }
        
    }
}
