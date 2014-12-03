using System;
using System.Linq;
using Finkit.ManicTime.Client.Main.Views;
using Finkit.ManicTime.Common;
using Finkit.ManicTime.Common.ActivityTimelines.Timelines;
using Finkit.ManicTime.Common.TagSources;
using Finkit.ManicTime.Plugins.Activities;
using Finkit.ManicTime.Shared;
using Finkit.ManicTime.Shared.Logging;

namespace TagPlugins.Example.Views
{
    public class TagSyncViewModel : ViewModel
    {
        private readonly TagSourceService _tagSourceService;
        private readonly ITimelineRepository _timelineRepository;
        private readonly IActivityRepository _activityRepository;
        public DelegateCommand SyncCommand { get; private set; }

        private string _lastSync;
        public string LastSync
        {
            get 
            {
                return _lastSync;
            }
            set
            {
                if (_lastSync == value)
                    return;
                _lastSync = value;
                OnPropertyChanged("LastSync");
            }
        }


        private Exception _error;
        public Exception Error
        {
            get 
            {
                return _error;
            }
            set
            {
                if (_error == value)
                    return;
                _error = value;
                OnPropertyChanged("Error");
            }
        }
        
        private string _syncResponse;
        public string SyncResponse
        {
            get 
            {
                return _syncResponse;
            }
            set
            {
                if (_syncResponse == value)
                    return;
                _syncResponse = value;
                OnPropertyChanged("SyncResponse");
            }
        }

        private bool _isSyncEnabled;
        public bool IsSyncEnabled
        {
            get 
            {
                return _isSyncEnabled;
            }
            set
            {
                if (_isSyncEnabled == value)
                    return;
                _isSyncEnabled = value;
                OnPropertyChanged("IsSyncEnabled");
            }
        }

        private TagSourceInstance _currentTagSourceInstance;

        public TagSyncViewModel(TagSourceService tagSourceService,
            ITimelineRepository timelineRepository, IActivityRepository activityRepository)
        {
            _tagSourceService = tagSourceService;
            _timelineRepository = timelineRepository;
            _activityRepository = activityRepository;
            SyncCommand = new DelegateCommand(OnSync) { CanExecute = true };
            IsSyncEnabled = true;
            InitTagSource();
            InitModel();
        }

        private void InitTagSource()
        {
            var exampleTagSources = _tagSourceService.GetTagSourceInstances()
                .Where(i => i.TagSource != null && i.TagSource.ServiceInfo.Id.Equals(ExampleTagPlugin.Id))
                .ToArray();
            if (exampleTagSources.Any())
            {
                _currentTagSourceInstance = exampleTagSources[0]; //we could handle multiple example tag sources here
                
            }
            else
                SyncCommand.CanExecute = false;
        }

        private void InitModel()
        {
            var settings = ((ExampleTagSourceSettings) _currentTagSourceInstance.Settings);
            LastSync = settings.LastSync;
        }


        private async void OnSync()
        {
            try
            {
                Error = null;
                IsSyncEnabled = false;

                var settings = (ExampleTagSourceSettings) _currentTagSourceInstance.Settings;
                var timeline = _timelineRepository.GetLocalTimeline(TimelineTypeNames.Tags);
                var date = DateTimeHelper.Today();
                var activityQuery =
                    new ActivityQuery().WithTimelineId(timeline.TimelineId)
                        .WithFromDate(date)
                        .WithToDate(date);

                var tagsToSync = _activityRepository.Get(activityQuery)
                    .Select(t => t.DisplayName).ToArray();

                var tagSourceDataProvider = 
                    new ExampleTagSourceDataProvider(settings.SourceUrl);

                SyncResponse =await tagSourceDataProvider.SyncTagsAsync(tagsToSync);
                
                LastSync = DateTime.Now.ToString("g");
                SaveLastSync(settings);
            }
            catch (Exception ex)
            {
                ApplicationLog.WriteError(ex);
                Error = ex;
            }
            finally
            {
                IsSyncEnabled = true;
            }
        }

        private void SaveLastSync(ExampleTagSourceSettings settings)
        {
            settings.LastSync = LastSync;
            _tagSourceService.SaveTagSourceInstance(_currentTagSourceInstance);
        }
    }
}
