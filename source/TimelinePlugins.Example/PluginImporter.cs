using System;
using System.Linq;
using Finkit.ManicTime.Plugins.Activities;
using Finkit.ManicTime.Plugins.Groups;
using Finkit.ManicTime.Shared.Helpers;
using Finkit.ManicTime.Shared.Logging;

namespace TimelinePlugins.Example
{
    public static class PluginImporter
    {
        public static Activity[] GetData(PluginTimeline timeline, Func<Group> createGroup,
            Func<Activity> createActivity, DateTime fromLocalTime, DateTime toLocalTime)
        {
            /*
                get activities from your source for time range fromLocalTime-toLocalTime
                then create Activity objects and return the collection.

                Here we create only one activity spanning the whole day.
            */
            try
            {
                var groupOne = CreateGroup("1-group", "Group One", "ff0000", createGroup);

                return new Activity[]
                {
                    CreateActivity("1-activity", fromLocalTime, toLocalTime, "Sample activity", groupOne, createActivity)
                };
            }
            catch (Exception ex)
            {
                ApplicationLog.WriteError(ex);
                throw;
            }
        }

        private static Group CreateGroup(string id, string displayName, string color, Func<Group> createGroup)
        {
            var group = createGroup();
            group.Color = color.ToRgb();
            group.SourceId = id;
            group.DisplayName = displayName;
            return group;
        }

        private static Activity CreateActivity(string id, DateTimeOffset startTime, DateTimeOffset endTime, 
            string displayName, Group group, Func<Activity> createActivity)
        {
            var activity = createActivity();
            activity.StartTime = startTime;
            activity.EndTime = endTime;
            activity.SourceId = id;
            activity.Group = group;
            activity.DisplayName = displayName;
            return activity;
        }
    }
}
