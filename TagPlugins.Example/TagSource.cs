using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Finkit.ManicTime.Common.TagSources;

namespace TagPlugins.Example
{
    public class TagSource : BasicTagSource
    {
        public TagSource(ITagSourceCache tagSourceCache) : base(tagSourceCache)
        {

        }

        protected override BasicTagSourceInstance CreateServerTagSourceInstance(ITagSourceSettings settings, string cacheTimestamp)
        {
            return new TagSourceInstance((TagSourceSettings)settings);
        }
    }
}
