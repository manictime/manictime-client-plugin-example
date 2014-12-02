using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Finkit.ManicTime.Common.TagSources;
using Finkit.ManicTime.Shared;
using Newtonsoft.Json;

namespace TagPlugins.Example
{
    public class TagSourceDataProvider
    {
        private readonly string _url;
        private readonly HttpClient _httpClient;

        public TagSourceDataProvider(string url)
        {
            _url = url;
            _httpClient = new HttpClient();
        }

        public async Task<TagSourceItem[]> LoadTagsAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(_url);
                return JsonConvert.DeserializeObject<string[]>(response).Select(x=> new TagSourceItem { Tags = new [] {x}}).ToArray();
            }
            catch (Exception ex)
            {
                AsyncExceptionHandler.Handle(ex);
                throw;
            }   
        }
    }
}
