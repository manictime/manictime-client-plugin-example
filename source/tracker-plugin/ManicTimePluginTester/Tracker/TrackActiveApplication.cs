using System;
using System.Diagnostics;
using System.Text;
using System.Timers;
using Timer = System.Timers.Timer;
using Finkit.ManicTime.Common;
using ManicTime.Client.Tracker.EventTracking.Publishers.ApplicationTracking;
using Plugins.Outlook;
using Plugins.Notepad;

namespace ManicTimePluginTester.Tracker
{
    public class TrackActiveApplication
    {
        private const int ProcessIdIdle = 0;
        private readonly Timer pollingTimer;
        private readonly IDocumentRetreiver[] _documentRetreivers;

        public TrackActiveApplication()
        {
            // if you created a new document plugin, add it to the list
            _documentRetreivers = new IDocumentRetreiver[]
            {
                new NotepadFileRetreiver(),
                new OutlookRetreiver()
            };

            pollingTimer = new Timer();
            pollingTimer.Interval = 1000;
            pollingTimer.Elapsed += pollingTimer_Elapsed;
        }

        public event EventHandler<EventArgs<ActiveApplication>> Changed = delegate { };
        public event EventHandler<EventArgs<Exception>> Error = delegate { };

        public void Start()
        {
            pollingTimer.Start();
        }

        protected void OnChanged(ActiveApplication activeApplication)
        {
            Changed(this, new EventArgs<ActiveApplication>(activeApplication));
        }

        protected void OnError(Exception exception)
        {
            Error(this, new EventArgs<Exception>(exception));
        }

        private void pollingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ActiveApplication currentActiveWindow = GetActiveWindow();
            OnChanged(currentActiveWindow);
        }

        public ActiveApplication GetActiveWindow()
        {
            ActiveApplication activeApplication = null;

            try
            {
                var handle = WindowsFunctions.GetForegroundWindow();
                int processID;
              
                WindowsFunctions.GetWindowThreadProcessId(handle, out processID);

                Process proc = Process.GetProcessById(processID);

                const int nChars = 512;
                StringBuilder Buff = new StringBuilder(nChars);
                string title = "";

                if (WindowsFunctions.GetWindowText(handle, Buff, nChars) > 0)
                {
                    title = Buff.ToString();
                }
                else
                {
                    try
                    {
                        title = proc.MainWindowTitle;
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                    }
                }

                string processName = proc.ProcessName;

                DocumentInfo documentInfo = GetDocument(new ApplicationInfo(
                    processID,
                    proc.ProcessName,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    handle,
                    title
                ));

                activeApplication = new ActiveApplication
                {
                    Title = title,
                    ProcessName = processName ?? "",
                    ProcessId = processID,
                    DocumentInfo = documentInfo
                };
            }
            catch (Exception ex)
            {
                OnError(ex);
            }

            return activeApplication;
        }

        private DocumentInfo GetDocument(ApplicationInfo application)
        {
            foreach (IDocumentRetreiver documentRetreiver in _documentRetreivers)
            {
                try
                {
                    DocumentInfo document = documentRetreiver.GetDocument(application);
                    if (document != null)
                        return document;
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
            return null;
        }
    }
}
