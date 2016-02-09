using System;
using System.Collections.Generic;
using System.Linq;
using Finkit.ManicTime.Client.Views.DayView;
using Finkit.ManicTime.Plugins;
using Finkit.ManicTime.Plugins.Activities;
using Finkit.ManicTime.Plugins.Folders;
using Finkit.ManicTime.Plugins.Groups;
using Finkit.ManicTime.Plugins.Timelines;
using Finkit.ManicTime.Shared.Helpers;

namespace TimelinePlugins.Example
{
    public class PluginDayViewLoader : IDayViewLoader
    {
        private readonly ITimelineEntityFactory _timelineEntityFactory;


        public PluginDayViewLoader(ITimelineEntityFactory timelineEntityFactory)
        {
            _timelineEntityFactory = timelineEntityFactory;
        }

        public void BeginLoad(Dictionary<Timeline, object> timelinesWithTimestamps, DateTime fromLocalTime,
            DateTime toLocalTime, Action<Dictionary<Timeline, DayViewLoadResult>, Exception> onLoadComplete)
        {
            ThreadHelper.QueueUserWorkItem(() =>
            {
                try
                {
                    foreach (var timeline in timelinesWithTimestamps.Keys)
                    {
                        try
                        {
                            onLoadComplete(new[] {timeline}
                                .ToDictionary(t => t,
                                    t => new DayViewLoadResult(PluginImporter.GetData((PluginTimeline) t,
                                        () => _timelineEntityFactory.Create<Group>(timeline),
                                        () => _timelineEntityFactory.Create<Activity>(timeline),
                                        fromLocalTime, toLocalTime),
                                        new Folder[] {},
                                        null)),
                                null);
                        }
                        catch (Exception ex)
                        {
                            onLoadComplete(new[] {timeline}.ToDictionary(t => t, t => (DayViewLoadResult) null), ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    onLoadComplete(timelinesWithTimestamps.Keys.ToDictionary(t => t, t => (DayViewLoadResult) null), ex);
                }
            });
        }
    }
}
