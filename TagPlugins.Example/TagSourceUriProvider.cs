using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TagPlugins.Example
{
    public static class TagSourceUriProvider
    {
        public static string GetDefaultBase()
        {
              return "http://localhost:61365/sampleData/PluginExample/ActivitiesAndGroups/";
        }
    }
}
