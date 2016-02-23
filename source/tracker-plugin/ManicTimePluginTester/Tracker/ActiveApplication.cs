using ManicTime.Client.Tracker.EventTracking.Publishers.ApplicationTracking;
using System;

namespace ManicTimePluginTester.Tracker
{
    public class ActiveApplication
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public string Title { get; set; }
        public DocumentInfo DocumentInfo { get; set; }
    }
}
