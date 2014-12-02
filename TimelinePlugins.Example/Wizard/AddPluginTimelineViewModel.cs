using System;
using Finkit.ManicTime.Client.Views;
using Finkit.ManicTime.Common.Core;
using Finkit.ManicTime.Shared;
using Finkit.ManicTime.Shared.Logging;

namespace TimelinePlugins.Example.Wizard
{
    public class AddPluginTimelineViewModel : WizardModel<TimelineEditorWizardState>
    {
        private readonly ITimelineFactory _timelineFactory;

        //timeline setting for data source url
        private string _sourceUrl;
        public string SourceUrl
        {
            get
            {
                return _sourceUrl;
            }
            set
            {
                if (_sourceUrl == value)
                    return;
                _sourceUrl = value;
                OnPropertyChanged("SourceUrl");
            }
        }

        //title of timeline
        private string _title;
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                if (_title == value)
                    return;
                _title = value;
                OnPropertyChanged("Title");
            }
        }
        public AddPluginTimelineViewModel(ITimelineFactory timelineFactory)
        {
            _timelineFactory = timelineFactory;

        }

        
        

        public override void OnViewLoaded(bool isReloaded)
        {
            if (State.EditedTimeline == null)
            {
                //set default state for add
                Title = "ManicTime timeline plugin example";
                SourceUrl = PluginUriProvider.GetDefaultBase();
            }

            else
            {
                //load state for edit    
                Title = State.EditedTimeline.DisplayName;
                var timeline = State.EditedTimeline as PluginTimeline;
                if (timeline == null)
                    return;
                SourceUrl = timeline.SourceUrl;
            }
        }

        private bool IsValid()
        {
            return !string.IsNullOrEmpty(SourceUrl) && !string.IsNullOrEmpty(Title);
        }

        public override WizardModel<TimelineEditorWizardState> GetNextPage()
        {
            return null;
        }

        public override bool CanMoveToNextPage()
        {
            return  CreateOrUpdateTimeline() && Error == null;
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

        //function creates or update current timeline
        private bool CreateOrUpdateTimeline()
        {
            try
            {
                PluginTimeline timeline;

                if (!IsValid())
                    return false;
                
                if (State.EditedTimeline == null)
                {
                    timeline = (PluginTimeline)_timelineFactory.Create(PluginLoader.TimelineTypeName, TimelineTypeNames.GenericGroup);
                    timeline.SourceTypeName = PluginLoader.SourceTypeName;
                    timeline.SourceUrl = PluginUriProvider.GetDefaultBase();
                }
                else
                    timeline = (PluginTimeline)State.EditedTimeline;

                timeline.SourceUrl = SourceUrl;
                timeline.DisplayName = Title;
                timeline.UpdateInterval = TimeSpan.FromMinutes(10);
                timeline.SourceAddress = string.Empty;

                if (State.EditedTimeline == null)
                    State.AddTimeline(timeline);
                else
                    State.UpdateTimeline(timeline);

                return true;
            }
            catch (Exception ex)
            {
                ApplicationLog.WriteError(ex);
                Error = ex;
                return false;
            }

        }


        public override bool IsLastPage
        {
            get { return true; }
        }
    }
}
