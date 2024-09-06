using System;
using System.IO;
using System.Linq;
using System.Windows;
using Finkit.ManicTime.Client.Main.Logic;
using Finkit.ManicTime.Common;
using Finkit.ManicTime.Plugins.Activities;
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

    public void ExportTags(MultipleGroupActivity[] allTagActivities, DateRange range)
    {
        var rows = allTagActivities.Select(t => t.DisplayName + "\t" + t.StartTime + "\t" + t.EndTime);
        var exportString = rows.Aggregate("", (sum, row) => sum + (sum == "" ? "" : "\n") + row);

        if (!Directory.Exists("c:\\ManicTimeData"))
            Directory.CreateDirectory("c:\\ManicTimeData");

        File.WriteAllText("c:\\ManicTimeData\\sampleExport.txt", exportString);
        ApplicationLog.WriteInfo("Data exported to: c:\\ManicTimeData\\sampleExport.txt");
        MessageBox.Show("Data exported to c:\\ManicTimeData\\sampleExport.txt");
    }
}