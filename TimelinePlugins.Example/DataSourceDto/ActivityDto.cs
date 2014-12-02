using System;

namespace TimelinePlugins.Example.DataSourceDto
{
    public class ActivityDto
    {
        public string DisplayName { get; set; }
        public string GroupId { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public string AdditionalData { get; set; }
        public string Id { get; set; }
    }
}
