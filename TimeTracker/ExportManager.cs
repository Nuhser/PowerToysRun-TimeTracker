using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Community.Powertoys.Run.Plugin.TimeTracker
{
    public class ExportManager
    {
        private static Dictionary<DateOnly, List<SummaryEntry>> GetDateToSummaryEntriesDict(
            Dictionary<DateOnly, List<TimeTracker.TrackerEntry>>? trackerEntries
        )
        {
            Dictionary<DateOnly, List<SummaryEntry>> dateToSummaryEntries = [];

            foreach (var day in trackerEntries ?? [])
            {
                if (!dateToSummaryEntries.ContainsKey(day.Key))
                {
                    dateToSummaryEntries.Add(day.Key, []);
                }

                foreach (var task in day.Value)
                {
                    bool foundEntryWithSameName = false;

                    foreach (var entry in dateToSummaryEntries[day.Key])
                    {
                        if (entry.Name == task.Name)
                        {
                            if (entry.ChildEntries.Count != 0)
                            {
                                entry.ChildEntries.Add(new(task.Name, task.Start, task.End));
                                entry.UpdateTotalChildDuration();
                            }
                            else
                            {
                                entry.ChildEntries.Add(new(entry.Name, entry.Start, entry.End));
                                entry.Start = null;
                                entry.End = null;
                                entry.ChildEntries.Add(new(task.Name, task.Start, task.End));
                                entry.UpdateTotalChildDuration();
                            }

                            foundEntryWithSameName = true;
                            break;
                        }
                    }

                    if (!foundEntryWithSameName)
                    {
                        dateToSummaryEntries[day.Key].Add(new(task.Name, task.Start, task.End));
                    }
                }
            }

            return dateToSummaryEntries;
        }

        public static string ExportToMarkdown(
            Dictionary<DateOnly, List<TimeTracker.TrackerEntry>>? trackerEntries
        )
        {
            string exportFileName = Path.Combine(SettingsManager.PLUGIN_PATH, @"summary.md");

            using StreamWriter exportFile = new(exportFileName);

            exportFile.WriteLine("# Time Tracker Summary");
            exportFile.WriteLine();

            foreach (var day in GetDateToSummaryEntriesDict(trackerEntries))
            {
                exportFile.WriteLine("## " + day.Key.ToString("dddd, d. MMMM yyyy"));
                exportFile.WriteLine();
                exportFile.WriteLine("|Name|Start|End|Duration|");
                exportFile.WriteLine("|-----|-----|-----|-----|");

                foreach (var task in day.Value)
                {
                    exportFile.WriteLine(
                        "|" +
                        task.Name +
                        "|" +
                        (task.Start?.ToString("HH:mm") ?? " ") +
                        "|" +
                        (task.End?.ToString("HH:mm") ?? " ") +
                        "|" +
                        (task.Duration != null
                            ? (task.Duration?.Hours + "h " + task.Duration?.Minutes + "m " + task.Duration?.Seconds + "s")
                            : " ") +
                        "|"
                    );

                    foreach (var child in task.ChildEntries)
                    {
                        exportFile.WriteLine(
                            "||" +
                            (child.Start?.ToString("HH:mm") ?? " ") +
                            "|" +
                            (child.End?.ToString("HH:mm") ?? " ") +
                            "|" +
                            (child.Duration != null
                                ? (child.Duration?.Hours + "h " + child.Duration?.Minutes + "m " + child.Duration?.Seconds + "s")
                                : " ") +
                            "|"
                        );
                    }
                }
            }

            return exportFileName;
        }

        public static string ExportToCSV(
            Dictionary<DateOnly, List<TimeTracker.TrackerEntry>>? trackerEntries
        )
        {
            string exportFileName = Path.Combine(SettingsManager.PLUGIN_PATH, @"summary.csv");

            using StreamWriter exportFile = new(exportFileName);

            exportFile.WriteLine("Date,Name,Start,End,Duration");

            foreach (var day in GetDateToSummaryEntriesDict(trackerEntries))
            {
                foreach (var task in day.Value)
                {
                    exportFile.WriteLine(string.Join(",", [
                        day.Key.ToString("dd.MM.yyyy"),
                        task.Name,
                        (task.Start?.ToString("HH:mm") ?? ""),
                        (task.End?.ToString("HH:mm") ?? ""),
                        (task.Duration != null
                            ? (task.Duration?.Hours + "h " + task.Duration?.Minutes + "m " + task.Duration?.Seconds + "s")
                            : ""
                        )
                    ]));

                    foreach (var child in task.ChildEntries)
                    {
                        exportFile.WriteLine(string.Join(",", [
                            "",
                            "",
                            (child.Start?.ToString("HH:mm") ?? ""),
                            (child.End?.ToString("HH:mm") ?? ""),
                            (child.Duration != null
                                ? (child.Duration?.Hours + "h " + child.Duration?.Minutes + "m " + child.Duration?.Seconds + "s")
                                : ""
                            )
                        ]));
                    }
                }
            }

            return exportFileName;
        }

        private class SummaryEntry(string name, DateTime? start, DateTime? end)
        {
            public string Name { get; set; } = name;
            public DateTime? Start { get; set; } = start;
            public DateTime? End { get; set; } = end;
            public TimeSpan? Duration { get; set; } = (end != null) ? (end - start) : null;
            public List<SummaryEntry> ChildEntries = [];

            public void UpdateTotalChildDuration()
            {

                Duration = ChildEntries
                    .Select(child => child.Duration)
                    .Aggregate((a, b) => a?.Add(b ?? TimeSpan.Zero) ?? b?.Add(a ?? TimeSpan.Zero));
            }
        }
    }
}