using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Finkit.ManicTime.Client.LocalActivityFetching.Messaging;
using Finkit.ManicTime.Client.Main.Logic;
using Finkit.ManicTime.Client.Main.Views;
using Finkit.ManicTime.Client.Views.SearchView.SearchUserControl;
using Finkit.ManicTime.Common;
using Finkit.ManicTime.Common.AutoTags;
using Finkit.ManicTime.Common.TagSources;
using Finkit.ManicTime.Common.Timelines;
using Finkit.ManicTime.Plugins.Activities;
using Finkit.ManicTime.Plugins.Timelines.Tags;
using Finkit.ManicTime.Shared;
using Finkit.ManicTime.Shared.AppSettings;
using Finkit.ManicTime.Shared.DayStartShifting;
using Finkit.ManicTime.Shared.Logging;
using Finkit.ManicTime.Shared.ManicTimeGrammar;
using Finkit.ManicTime.Shared.Plugins.ServiceProviders.PluginCommands;
using TagPlugins.Core;

namespace TagPlugin.ExportTags;

public class ExportTagsCommand : PluginCommand
{
    private readonly GlobalAppSettings _appSettings;
    private readonly TagSourceService _tagSourceService;
    private readonly ActivityReaderMessageClient _activityReaderMessageClient;
    private readonly IViewTimelineCache _viewTimelineCache;
    private readonly TagsExporter _tagsExporter;
    private readonly SearchActivityFetcher _searchActivityFetcher;
    private readonly MTGrammarWrapper _mtGrammarWrapper;
    private readonly Func<ExportTagsPromptViewModel> _createExportTagsPromptView;

    public ExportTagsCommand(
        GlobalAppSettings appSettings,
        IEventHub eventHub, 
        TagSourceService tagSourceService,
        ActivityReaderMessageClient activityReaderMessageClient,
        IViewTimelineCache viewTimelineCache,
        TagsExporter tagsExporter,
        SearchActivityFetcher searchActivityFetcher,
        MTGrammarWrapper mtGrammarWrapper,
        Func<ExportTagsPromptViewModel> createExportTagsPromptView)
    {
        _appSettings = appSettings;
        _tagSourceService = tagSourceService;
        _activityReaderMessageClient = activityReaderMessageClient;
        _viewTimelineCache = viewTimelineCache;
        _tagsExporter = tagsExporter;
        _searchActivityFetcher = searchActivityFetcher;
        _mtGrammarWrapper = mtGrammarWrapper;
        _createExportTagsPromptView = createExportTagsPromptView;
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
            ExportTagsPromptViewModel promptView = _createExportTagsPromptView();
            promptView.Initialize(
                _viewTimelineCache.Timelines.Any(t => t.TimelineType.TypeName == TimelineTypeNames.AutoTags));

            MessageButton pressedBtn = Application.Current.Dispatcher.Invoke(() =>
                ViewHelper.ShowViewModelWindow(Application.Current.MainWindow, promptView));

            if (pressedBtn != MessageButtons.Ok)
                return;

            DateRange range = _tagsExporter.GetDateRange();
            MultipleGroupActivity[] exportTagActivities =
            promptView.ExportTags
                ? await GetTagActivitiesAsync(range.From, range.To).ConfigureAwait(false) 
                : await GetAutoTagActivities(range.From, range.To).ConfigureAwait(false);

            _tagsExporter.ExportTags(exportTagActivities, range);
        }
        catch (Exception ex)
        {
            ApplicationLog.WriteError(ex);
        }
    }

    /*
        Tag activities for the selected date range. All activities are returned.
        We usually append a hidden tag (the ones preceded with a colon (:)) so we know which plugin was responsible for them.
        Below we filter the list to get only tags from this plugin. You can easily just skip that and process all of them.

        In this sample we only get the tags from this plugin, then save them to a local file.
    */
    private async Task<TagActivity[]> GetTagActivitiesAsync(int dateFrom, int dateTo)
    {
        var timeline = _viewTimelineCache.LocalTagTimeline;

        SearchActivityFetcherQuery searchQuery = new(
            [timeline.TimelineKey],
            dateFrom, dateTo,
            _appSettings.GetSnapshot().GetDayStartShift(),
            null,
            false);

        ExportActivityFilter activityFilter = new ExportActivityFilter(_mtGrammarWrapper);
        activityFilter.SetQueryText($"label:{ClientPlugin.HiddenTagLabel.ToLower()}");
        Activity[] activities = await _searchActivityFetcher
            .GetActivities(timeline, searchQuery, activityFilter, CancellationToken.None)
            .ToArrayAsync();

        return activities
            .Cast<TagActivity>()
            .ToArray();
    }

    /*
        AutoTag activities for the selected date range. All activities are returned.

        In this sample we only get all the AutoTags for the range, then save them to a local file.
    */
    private async Task<AutoTagActivity[]> GetAutoTagActivities(int dateFrom, int dateTo)
    {
        var timeline = _viewTimelineCache.Timelines.FirstOrDefault(t => t.TimelineType.TypeName == TimelineTypeNames.AutoTags);
        if (timeline == null)
            return [];

        SearchActivityFetcherQuery searchQuery = new(
            [timeline.TimelineKey],
            dateFrom, dateTo,
            _appSettings.GetSnapshot().GetDayStartShift(),
            null,
            false);

        ExportActivityFilter activityFilter = new ExportActivityFilter(_mtGrammarWrapper);
        Activity[] activities = await _searchActivityFetcher
            .GetActivities(timeline, searchQuery, activityFilter, CancellationToken.None)
            .ToArrayAsync();

        return activities.Cast<AutoTagActivity>().ToArray();
    }
}