using System;
using System.Linq;
using System.Threading.Tasks;
using Finkit.ManicTime.Common.TagSources;
using Finkit.ManicTime.Shared;

namespace TagPlugins.Example
{
    public class TagSourceDataProvider
    {
        private readonly string _url;

        public TagSourceDataProvider(string url)
        {
            _url = url;
        }

        public async Task<TagSourceItem[]> LoadTagsAsync()
        {
            try
            {
                var result = Enumerable.Range(0, 10).Select(x => new TagSourceItem {Tags = new[] {"Tag" + x}}).ToArray();
                return await TaskEx.FromResult(result);
            }
            catch (Exception ex)
            {
                
                AsyncExceptionHandler.Handle(ex);
                throw;
            }   
        }
    }
}
