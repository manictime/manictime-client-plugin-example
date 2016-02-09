using System;
using System.Linq;
using System.Windows;
using Finkit.ManicTime.Common;
using Finkit.ManicTime.Common.TagSources;
using Finkit.ManicTime.Shared.Plugins.ServiceProviders.PluginCommands;
using TagPlugins.Core;
using Finkit.ManicTime.Common.ActivityTimelines.Timelines;
using Finkit.ManicTime.Plugins.Activities;
using Finkit.ManicTime.Shared.Logging;
using Finkit.ManicTime.Client.Main.Logic;
using Finkit.ManicTime.Plugins.Timelines.Tags;
using Finkit.ManicTime.Shared;

namespace TagPlugin
{
    public class ExportTagsCommand : PluginCommand
    {
        private readonly TagSourceService _tagSourceService;
        private readonly IActivityRepository _activityRepository;
        private readonly ITimelineRepository _timelineRepository;

        public ExportTagsCommand(
            IEventHub eventHub, 
            TagSourceService tagSourceService,
            ITimelineRepository timelineRepository,
            IActivityRepository activityRepository)
        {
            _tagSourceService = tagSourceService;
            _timelineRepository = timelineRepository;
            _activityRepository = activityRepository;
            eventHub.Subscribe<TagSourceCacheUpdatedEvent>(OnTagSourceCacheUpdated);
            InvokeOnUiThread(SetCanExecute);
        }

        public override string Name => "Send tags";

        private static void InvokeOnUiThread(Action action)
        {
            var currentApplication = Application.Current;
            if (currentApplication == null || currentApplication.CheckAccess())
                action();
            else
                currentApplication.Dispatcher.Invoke(action);
        }

        private void OnTagSourceCacheUpdated(TagSourceCacheUpdatedEvent obj)
        {
            InvokeOnUiThread(SetCanExecute);
        }

        private void SetCanExecute()
        {
            CanExecute =
                TagPluginsHelper.GetTagSourceInstances(_tagSourceService.GetTagSourceInstances(),
                    ClientPlugin.Id).Any();
        }

        public override void Execute()
        {
            try
            {
                DateRange range = TagsExporter.GetDateRange();
                var tagActivities = GetTagActivities(range.From, range.To);
                TagsExporter.ExportTags(tagActivities, range);
            }
            catch (Exception ex)
            {
                ApplicationLog.WriteError(ex);
                return;
            }
        }

        private TagActivity[] GetTagActivities(int dateFrom, int dateTo)
        {
            var timeline = _timelineRepository.GetLocalTimeline(TimelineTypeNames.Tags);
            var activityQuery = new ActivityQuery().WithTimelineId(timeline.TimelineId)
                    .WithFromDate(dateFrom)
                    .WithToDate(dateTo);

            return _activityRepository
                .Get(activityQuery)
                .Cast<TagActivity>()
                .ToArray();
        }
    }
}