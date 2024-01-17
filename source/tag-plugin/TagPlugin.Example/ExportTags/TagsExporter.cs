using System;
using System.IO;
using System.Linq;
using System.Windows;
using Finkit.ManicTime.Client.Main.Logic;
using Finkit.ManicTime.Client.Main.Views;
using Finkit.ManicTime.Common;
using Finkit.ManicTime.Plugins.Timelines.Tags;
using Finkit.ManicTime.Shared.Logging;

namespace TagPlugin.ExportTags;

public class TagsExporter
{
    /*
        Date range for which the data will be fetched.
    */
    public DateRange GetDateRange()
    {
        var from = DateTime.Today;
        var to = DateTime.Today;

        return new DateRange(DateTimeHelper.FromUnshiftedDateTime(from), DateTimeHelper.FromUnshiftedDateTime(to));
    }

    /*
        Tag activities for the selected date range. All activities are returned. 
        We usually append a hidden tag (the ones preceded with a colon (:)) so we know which plugin was responsible for them.
        Below we filter the list to get only tags from this plugin. You can easily just skip that and process all of them.
        
        In this sample we only get the tags from this plugin, then save them to a local file.
    */
    public void ExportTags(TagActivity[] allTagActivities, DateRange range)
    {
        MessageButton pressedBtn = Application.Current.Dispatcher.Invoke(() =>
                ViewHelper.ShowViewModelWindow<ExportTagsPromptViewModel>(Application.Current.MainWindow));

        if (pressedBtn != MessageButtons.Yes)
            return;

        var pluginTags = allTagActivities
            .Where(ta => ta.Groups.Select(g => g.DisplayKey.ToLower()).Contains(ClientPlugin.HiddenTagLabel.ToLower()));

        var rows = pluginTags.Select(t => t.DisplayName + "\t" + t.StartTime + "\t" + t.EndTime);
        var exportString = rows.Aggregate("", (sum, row) => sum + (sum == "" ? "" : "\n") + row);

        if (!Directory.Exists("c:\\ManicTimeData"))
            Directory.CreateDirectory("c:\\ManicTimeData");

        File.WriteAllText("c:\\ManicTimeData\\sampleExport.txt", exportString);
        ApplicationLog.WriteInfo("Data exported to: c:\\ManicTimeData\\sampleExport.txt");
        MessageBox.Show("Data exported to c:\\ManicTimeData\\sampleExport.txt");
    }
}