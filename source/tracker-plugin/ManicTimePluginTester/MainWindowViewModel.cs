using System;
using System.ComponentModel;
using System.Windows.Threading;
using ManicTimePluginTester.Tracker;
using Finkit.ManicTime.Common;

namespace ManicTimePluginsTester
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private TrackActiveApplication _trackActiveApplication;

        public Dispatcher Dispatcher { get; set; }

        private string _trackerOutput;
        public string TrackerOutput
        {
            get
            {
                return _trackerOutput;
            }
            set
            {
                if (_trackerOutput == value)
                    return;
                _trackerOutput = value;
                OnPropertyChanged("TrackerOutput");
            }
        }

        private string _debugOutput;
        public string DebugOutput
        {
            get
            {
                return _debugOutput;
            }
            set
            {
                if (_debugOutput == value)
                    return;
                _debugOutput = value;
                OnPropertyChanged("DebugOutput");
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindowViewModel()
        {
            _trackActiveApplication = new TrackActiveApplication();
        }

        public void OnLoaded()
        {
            _trackActiveApplication.Changed += _trackActiveApplication_Changed;
            _trackActiveApplication.Error += _trackActiveApplication_Error;
            _trackActiveApplication.Start();
        }

        void _trackActiveApplication_Error(object sender, EventArgs<System.Exception> e)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => DebugOutput += e.Data + Environment.NewLine));
        }

        void _trackActiveApplication_Changed(object sender, EventArgs<ActiveApplication> e)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => UpdateTrackerOutput(e.Data)));
        }

        private void UpdateTrackerOutput(ActiveApplication activeApplication)
        {
            if (activeApplication.DocumentInfo != null)
            {
                TrackerOutput =
                "\t DocumentInfo ->" + Environment.NewLine +
                "\t\t DocumentGroupName: '" + activeApplication.DocumentInfo.DocumentGroupName + "'" + Environment.NewLine +
                "\t\t DocumentName: '" + activeApplication.DocumentInfo.DocumentName + "'" + Environment.NewLine +
                "\t\t DocumentType: '" + activeApplication.DocumentInfo.DocumentType + "'" + Environment.NewLine +
                TrackerOutput;
            }

            TrackerOutput =
                DateTime.Now +
                " Title:'" + activeApplication.Title +
                "' ProcessName:'" + activeApplication.ProcessName + Environment.NewLine +
                TrackerOutput;
        }
    }
}
