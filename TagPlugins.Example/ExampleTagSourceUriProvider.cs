namespace TagPlugins.Example
{
    public static class ExampleTagSourceUriProvider
    {
        public static string GetDefaultBase()
        {
              return "http://localhost:61365/sampleData/PluginExample";
        }

        public static string GetTagSourceUrl(string baseUrl = null)
        {
            return string.Format("{0}{1}",baseUrl ??GetDefaultBase(),"/TagSource");
        }

        public static string GetTagSyncUrl(string baseUrl = null)
        {
            return string.Format("{0}{1}",baseUrl ??GetDefaultBase(),"/SyncTags");
        }
    }
}
