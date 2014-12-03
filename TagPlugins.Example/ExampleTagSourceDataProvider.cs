using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Finkit.ManicTime.Common.TagSources;
using Finkit.ManicTime.Shared;
using Newtonsoft.Json;

namespace TagPlugins.Example
{
    public class ExampleTagSourceDataProvider
    {
        private readonly string _url;
        private readonly HttpClient _httpClient;

        public ExampleTagSourceDataProvider(string url)
        {
            _url = url;
            _httpClient = new HttpClient();
        }

        public async Task<TagSourceItem[]> LoadTagsAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(ExampleTagSourceUriProvider.GetTagSourceUrl(_url));
                if (string.IsNullOrEmpty(response))
                    return new TagSourceItem[0];

                return JsonConvert.DeserializeObject<string[]>(response).Select(x=> new TagSourceItem { Tags = new [] {x}}).ToArray();
            }
            catch (Exception ex)
            {
                AsyncExceptionHandler.Handle(ex);
                throw;
            }   
        }

        public async Task<string> SyncTagsAsync(string[] tags)
        {
            try
            {
                var response = await _httpClient.PostAsync(ExampleTagSourceUriProvider.GetTagSyncUrl(_url),
                    new StringContent(JsonConvert.SerializeObject(tags),Encoding.UTF8,"application/json"));
                if (!response.IsSuccessStatusCode)
                    throw new Exception(response.ReasonPhrase);
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                AsyncExceptionHandler.Handle(ex);
                throw;
            }
        }
    }
}
