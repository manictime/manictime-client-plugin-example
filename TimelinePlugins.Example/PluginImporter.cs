using System;
using System.Linq;
using Finkit.ManicTime.Common.ActivityTimelines.Timelines;
using Finkit.ManicTime.Plugins.Activities;
using Finkit.ManicTime.Plugins.Groups;
using Finkit.ManicTime.Shared.Helpers;
using Finkit.ManicTime.Shared.Logging;

namespace TimelinePlugins.Example
{
    public static class PluginImporter
    {
        public static Activity[] GetData(PluginTimeline timeline, Func<Group> createGroup,
         Func<Activity> createActivity, DateTime fromLocalTime, DateTime toLocalTime,
            ITimelineRepository timelineRepository, PluginDataService pluginDataService)
        {

            try
            {
                //get current data in plugin if needed
                //var currrentData = timeline.Response == null
                //    ? null
                //    : JsonConvert.DeserializeObject<TimelinePluginExampleDto>(timeline.Response);

                var response =  pluginDataService.GetExampleDataAsync(fromLocalTime).Result;
                var groups = response.Groups.Select(groupDto => CreateGroup(groupDto.GroupId, groupDto.DisplayName, groupDto.Color, createGroup));
                var activities =
                    response.Activities.Select(activityDto => CreateActivity(activityDto.Id, activityDto.StartTime, activityDto.EndTime,
                                activityDto.DisplayName,groups.Single(g => g.SourceId == activityDto.GroupId), createActivity));

                //save response data to plugin repository if needed (caching...)
                //timeline.Response = JsonConvert.SerializeObject(response);
                //timelineRepository.Save(timeline);

                return activities.ToArray();
            }
            catch (Exception ex)
            {
                ApplicationLog.WriteError(ex);
                throw;
            }
        }

        private static Group CreateGroup(string id, string displayName,string color,  Func<Group> createGroup)
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
