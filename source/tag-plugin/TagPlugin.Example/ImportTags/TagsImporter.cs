using System.Collections.Generic;
using Finkit.ManicTime.Common.TagSources;
using Finkit.ManicTime.Shared.Tags.Labels;

namespace TagPlugin.ImportTags
{
    public class TagsImporter
    {
        public static List<TagSourceItem> GetTags()
        {
            var tagSourceItems = new List<TagSourceItem>
            {
                new TagSourceItem
                {
                    Tags = new[] { "Demo Project 1", "Demo Activity 1", ClientPlugin.HiddenTagLabel },
                    Notes = "Note",
                    IsApproved = false
                },
                new TagSourceItem
                {
                    Tags = new[] { "Demo Project 2", "Demo Activity 2", TagLabels.Billable, ClientPlugin.HiddenTagLabel },
                    Notes = "Note 2",
                    IsApproved = false
                }
            };

            return tagSourceItems;
        }
    }
}