using System;

namespace TimelinePlugins.Example
{
    public static class PluginUriProvider
    {
        public static string GetDefaultBase()
        {
            //return "http://www.manictime.com/sampleData/PluginExample/ActivitiesAndGroups/";
            return "http://localhost:61365/sampleData/PluginExample/ActivitiesAndGroups/";
        }

        public static string GetWithParameters(DateTimeOffset fromTime, string baseUri = null)
        {
            return string.Format("{0}?fromTime={1}",baseUri ?? GetDefaultBase(),fromTime.ToString("yyyy-MM-dd"));
        }
    }
}
