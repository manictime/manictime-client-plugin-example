using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Finkit.ManicTime.Client.LocalActivityFetching.Messaging;
using Finkit.ManicTime.Client.Main.Logic;
using Finkit.ManicTime.Common;
using Finkit.ManicTime.Common.TagSources;
using Finkit.ManicTime.Common.Timelines;
using Finkit.ManicTime.Plugins.Timelines.Tags;
using Finkit.ManicTime.Shared;
using Finkit.ManicTime.Shared.Logging;
using Finkit.ManicTime.Shared.Plugins.ServiceProviders.PluginCommands;
using TagPlugins.Core;

namespace TagPlugin.ExportTags
{
    public class ExportTagsCommand : PluginCommand
    {
        private readonly TagSourceService _tagSourceService;
        private readonly ActivityReaderMessageClient _activityReaderMessageClient;
        private readonly IViewTimelineCache _viewTimelineCache;

        public ExportTagsCommand(
            IEventHub eventHub, 
            TagSourceService tagSourceService,
            ActivityReaderMessageClient activityReaderMessageClient,
            IViewTimelineCache viewTimelineCache)
        {
            _tagSourceService = tagSourceService;
            _activityReaderMessageClient = activityReaderMessageClient;
            _viewTimelineCache = viewTimelineCache;
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

        public override async void Execute()
        {
            try
            {
                DateRange range = TagsExporter.GetDateRange();
                var tagActivities = await GetTagActivitiesAsync(range.From, range.To).ConfigureAwait(false);
                TagsExporter.ExportTags(tagActivities, range);
            }
            catch (Exception ex)
            {
                ApplicationLog.WriteError(ex);
            }
        }

        private async Task<TagActivity[]> GetTagActivitiesAsync(int dateFrom, int dateTo)
        {
            var timeline = _viewTimelineCache.LocalTagTimeline;
            var activities = await _activityReaderMessageClient.GetActivitiesAsync(
                timeline,
                new Date(dateFrom.AsStartDateTime()),
                new Date(dateTo.AsStartDateTime()),
                false,
                CancellationToken.None).ConfigureAwait(false);

            return activities.Cast<TagActivity>().ToArray();
        }
    }
}