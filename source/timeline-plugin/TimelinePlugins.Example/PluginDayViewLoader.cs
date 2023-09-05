using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using Finkit.ManicTime.Client.Main.Views;
using Finkit.ManicTime.Client.Timelines.Messaging;
using Finkit.ManicTime.Client.Views.DayView;
using Finkit.ManicTime.Plugins;
using Finkit.ManicTime.Plugins.Activities;
using Finkit.ManicTime.Plugins.Folders;
using Finkit.ManicTime.Plugins.Groups;
using Finkit.ManicTime.Plugins.Timelines;
using Finkit.ManicTime.Shared.Helpers;
using Finkit.ManicTime.Shared.Http;
using TimelinePlugins.Example.CustomPrompt;

namespace TimelinePlugins.Example
{
    public class PluginDayViewLoader : IDayViewLoader
    {
        private readonly PluginImporter _pluginImporter;
        private readonly ITimelineEntityFactory _timelineEntityFactory;
        private readonly TimelinesMessageClient _timelinesMessageClient;
        private readonly Func<PromptViewModel> _createPromptViewModel;
        
        public PluginDayViewLoader(
            PluginImporter pluginImporter,
            ITimelineEntityFactory timelineEntityFactory,
            TimelinesMessageClient timelinesMessageClient,
            Func<PromptViewModel> createPromptViewModel)
        {
            _pluginImporter = pluginImporter;
            _timelineEntityFactory = timelineEntityFactory;
            _timelinesMessageClient = timelinesMessageClient;
            _createPromptViewModel = createPromptViewModel;
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
                            //simulate authentication error as if _state has outdated refresh token
                            if (timeline.Properties["_state"] is not string stateValue || stateValue != "authenticatedTokens")
                                throw new UnauthorizedException("test", null);

                            onLoadComplete(new[] { timeline }
                                    .ToDictionary(t => t,
                                        t => new DayViewLoadResult(_pluginImporter.GetData((PluginTimeline)t,
                                                () => _timelineEntityFactory.Create<Group>(timeline),
                                                () => _timelineEntityFactory.Create<Activity>(timeline),
                                                fromLocalTime, toLocalTime),
                                            new Folder[] { },
                                            null)),
                                null);
                        }
                        catch (UnauthorizedException ex)
                        {
                            //e.g. if you need to re-authenticate
                            var sourceTimeline = _timelinesMessageClient.GetTimeline(timeline.TimelineId, CancellationToken.None).Result;

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                // show custom prompt
                                using var promptViewModel = _createPromptViewModel();
                                var btnResult = ViewHelper.ShowViewModelWindow(Application.Current.MainWindow, promptViewModel);
                                if (btnResult == MessageButtons.Ok) // user re-authenicated (clicked ok)
                                {
                                    // update timeline access and refresh tokens
                                    sourceTimeline.Properties["_state"] = "authenticatedTokens";
                                    var updatedTimeline = _timelinesMessageClient.SaveTimeline(sourceTimeline, CancellationToken.None).Result;

                                    BeginLoad(new Dictionary<Timeline, object>(){ { updatedTimeline, null} }, fromLocalTime, toLocalTime, onLoadComplete);
                                }
                                else // user did not re-authenicated (clicked X)
                                {
                                    

                                    // show custom timeline message instead of warning
                                    onLoadComplete(new[] { timeline }.ToDictionary(t => t, t => new DayViewLoadResult("Please re-authenticate ...")), null);
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            onLoadComplete(new[] { timeline }.ToDictionary(t => t, t => (DayViewLoadResult)null), ex);
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
