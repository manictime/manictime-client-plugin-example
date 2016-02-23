using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Finkit.ManicTime.Shared.DocumentTracking;
using ManicTime.Client.Tracker.EventTracking.Publishers.ApplicationTracking;
using ManicTime.Client.Tracker.Win.EventTracking.Publishers.ApplicationTracking.Handlers.DocumentTracking;

namespace Plugins.Outlook
{
    public class OutlookRetreiver : DocumentRetreiverBase
    {
        protected override bool CheckForProcess(ApplicationInfo application)
        {
            if (application.ProcessName.ToLower() == "outlook")
                return true;

            return false;
        }

        protected override DocumentInfo GetDocumentInfo(ApplicationInfo application)
        {
            object item = null;
            object oApp = null;
            object activeWindow = null;
            object selection = null;
            List<object> recipientReferences = null;

            try
            {
                oApp = Marshal.GetActiveObject("Outlook.Application");
                activeWindow = oApp.GetType().InvokeMember("ActiveWindow", BindingFlags.GetField | BindingFlags.InvokeMethod | BindingFlags.GetProperty, null, oApp, null);

                if (activeWindow != null)
                {
                    string activeWindowType = ComHelper.GetTypeName(activeWindow);

                    if (activeWindowType == "_Inspector")
                    {
                        item = activeWindow.GetType().InvokeMember("CurrentItem", BindingFlags.GetField | BindingFlags.InvokeMethod | BindingFlags.GetProperty, null, activeWindow, null);
                    }
                    else if (activeWindowType == "_Explorer")
                    {
                        selection = activeWindow.GetType().InvokeMember("Selection", BindingFlags.GetField | BindingFlags.InvokeMethod | BindingFlags.GetProperty, null, activeWindow, null);
                        var count = selection.GetType().InvokeMember("Count", BindingFlags.GetField | BindingFlags.InvokeMethod | BindingFlags.GetProperty, null, selection, null);
                        if ((int)count == 1)
                        {
                            var enumerator = (IEnumerator)selection.GetType().InvokeMember("GetEnumerator", BindingFlags.InvokeMethod | BindingFlags.Default | BindingFlags.IgnoreCase, null, selection, null);

                            if (enumerator.MoveNext())
                            {
                                item = enumerator.Current;
                            }
                        }
                    }
                }

                if (item == null)
                    return null;

                string itemType = ComHelper.GetTypeName(item);
                if (itemType == "_MailItem")
                {
                    var subject = (string)item.GetType().InvokeMember("Subject", BindingFlags.GetField | BindingFlags.InvokeMethod | BindingFlags.GetProperty, null, item, null);
                    var isSent = (bool)item.GetType().InvokeMember("Sent", BindingFlags.GetField | BindingFlags.InvokeMethod | BindingFlags.GetProperty, null, item, null);
                    if (isSent)
                    {
                        var sender = (string)item.GetType().InvokeMember("SenderEmailAddress", BindingFlags.GetField | BindingFlags.InvokeMethod | BindingFlags.GetProperty, null, item, null);
                        return new DocumentInfo
                        {
                            DocumentGroupName = "Reading email",
                            DocumentName = "Email from: " + sender + ", Subject: " + subject,
                            DocumentType = DocumentTypes.Email
                        };
                    }
                    else
                    {
                        string to = "";
                        recipientReferences = new List<object>();
                        var recipientObjects = item.GetType().InvokeMember("Recipients", BindingFlags.GetField | BindingFlags.InvokeMethod | BindingFlags.GetProperty, null, item, null);

                        var enumerator = (IEnumerator)recipientObjects.GetType().InvokeMember("GetEnumerator", BindingFlags.InvokeMethod | BindingFlags.Default | BindingFlags.IgnoreCase, null, recipientObjects, null);

                        if (enumerator.MoveNext())
                        {
                            var recipient = enumerator.Current;
                            var address = recipient.GetType().InvokeMember("Address", BindingFlags.GetField | BindingFlags.InvokeMethod | BindingFlags.GetProperty, null, recipient, null);
                            to += (to == "" ? "" : ", ") + address;
                            recipientReferences.Add(recipient);
                        }

                        return new DocumentInfo
                        {
                            DocumentGroupName = "Sending email",
                            DocumentName = "Email to: " + to + ", Subject: " + subject,
                            DocumentType = DocumentTypes.Email
                        };
                    }
                }
                else if (itemType == "_AppointmentItem")
                {
                    var subject = item.GetType().InvokeMember("Subject", BindingFlags.GetField | BindingFlags.InvokeMethod | BindingFlags.GetProperty, null, item, null);
                    var location = item.GetType().InvokeMember("Location", BindingFlags.GetField | BindingFlags.InvokeMethod | BindingFlags.GetProperty, null, item, null);
                    return new DocumentInfo
                    {
                        DocumentGroupName = "Outlook calendar",
                        DocumentName = "Subject: " + subject + ", Location: " + location,
                        DocumentType = DocumentTypes.Event
                    };
                }
                else if (itemType == "_TaskItem")
                {
                    var subject = item.GetType().InvokeMember("Subject", BindingFlags.GetField | BindingFlags.InvokeMethod | BindingFlags.GetProperty, null, item, null);
                    return new DocumentInfo
                    {
                        DocumentGroupName = "Tasks",
                        DocumentName = "Subject: " + subject,
                        DocumentType = DocumentTypes.Task
                    };
                }
            }
            finally
            {
                if (recipientReferences != null)
                {
                    foreach (var recipientReference in recipientReferences)
                    {
                        Marshal.ReleaseComObject(recipientReference);
                    }
                    recipientReferences.Clear();
                }

                if (item != null)
                    Marshal.ReleaseComObject(item);

                if (selection != null)
                    Marshal.ReleaseComObject(selection);
                if (activeWindow != null)
                    Marshal.ReleaseComObject(activeWindow);
                if (oApp != null)
                    Marshal.ReleaseComObject(oApp);
            }

            return null;
        }
    }
}
