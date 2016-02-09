using System.Collections.Generic;
using System.Threading.Tasks;
using Finkit.ManicTime.Common.Plugins.Timelines.Tags.TagLabel;
using Finkit.ManicTime.Common.TagSources;

namespace TagPlugin
{
    public class TagsImporter
    {
        public static async Task<List<TagSourceItem>> GetTags()
        {
            var tagSourceItems = new List<TagSourceItem>
            {
                new TagSourceItem
                {
                    Tags = new[] { "Demo Project 1", "Demo Activity 1", ClientPlugin.HiddenTagLabel },
                    Notes = "Note"
                },
                new TagSourceItem
                {
                    Tags = new[] { "Demo Project 2", "Demo Activity 2", TagLabels.Billable, ClientPlugin.HiddenTagLabel },
                    Notes = "Note 2"
                }
            };

            return tagSourceItems;
        }
    }
}