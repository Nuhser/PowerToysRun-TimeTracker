using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Wox.Plugin;

namespace Community.Powertoys.Run.Plugin.TimeTracker
{
    public partial class QueryManager(SettingsManager settingsManager, Dictionary<DateOnly, List<TimeTracker.TrackerEntry>>? trackerEntries)
    {
        private const string COPY_GLYPH = "\xE8C8";
        private readonly JsonSerializerOptions JSON_SERIALIZER_OPTIONS = new() { WriteIndented = true };

        public List<Result> CheckQueryAndReturnResults(string queryString)
        {
            return GetQueryResults(queryString)
                .Where(result => result.ShouldShowResult(queryString))
                .Select(result => result.GetResult(queryString, settingsManager))
                .ToList();
        }

        public List<QueryResult> GetQueryResults(string queryString)
        {
            return [
                new() {
                    AdditionalChecks = (queryString) =>
                        string.IsNullOrWhiteSpace(queryString) &&
                        IsRunningTaskPresent(),
                    Title = "Stop Currently Running Task",
                    Description =
                        GetNumberOfCurrentRunningTasks() > 1
                            ? "Stops all currently running tasks."
                            : "Stops the currently running task '" + GetRunningTasksName() + "'.",
                    IconName = "",
                    Action = (_) => AddEndTimeToAllRunningTasks()
                },
                new() {
                    AdditionalChecks = (queryString) =>
                        !string.IsNullOrWhiteSpace(queryString),
                    Title = "Start New Task",
                    Description =
                        IsRunningTaskPresent()
                            ? GetNumberOfCurrentRunningTasks() > 1
                                ? "Stops currently running tasks and starts a new one named '" + queryString + "'."
                                : "Stops the currently running task '" + GetRunningTasksName() + "' and starts a new one named '" + queryString + "'."
                            : "Starts a new task named '" + queryString + "'.",
                    IconName = "",
                    Action = (queryString) => {
                        AddEndTimeToAllRunningTasks();
                        AddNewTrackerEntry(queryString);
                    }
                },
                new() {
                    AdditionalChecks = string.IsNullOrWhiteSpace,
                    Title = "Show Time Tracker Summary",
                    Description = "",
                    IconName = "",
                    Action = (_) => CreateAndOpenTimeTrackerSummary()
                }
            ];
        }

        private void WriteTrackerEntriesToFile()
        {
            string jsonString = JsonSerializer.Serialize(trackerEntries, JSON_SERIALIZER_OPTIONS);
            File.WriteAllText(Path.Combine(SettingsManager.PLUGIN_PATH, SettingsManager.SAVES_NAME), jsonString);
        }

        private void AddNewTrackerEntry(string name)
        {
            if (!trackerEntries?.ContainsKey(DateOnly.FromDateTime(DateTime.Now)) ?? true)
            {
                trackerEntries?.Add(DateOnly.FromDateTime(DateTime.Now), [new TimeTracker.TrackerEntry(name)]);
            }
            else
            {
                trackerEntries?[DateOnly.FromDateTime(DateTime.Now)].Add(new TimeTracker.TrackerEntry(name));
            }

            WriteTrackerEntriesToFile();
        }

        private void AddEndTimeToAllRunningTasks()
        {
            if (trackerEntries != null)
            {
                foreach (var entryList in trackerEntries.Values)
                {
                    entryList
                        .Where(entry => entry.End == null)
                        .ToList()
                        .ForEach(entry => entry.End = DateTime.Now);
                }

                WriteTrackerEntriesToFile();
            }
        }

        private string? GetRunningTasksName()
        {
            if (trackerEntries?.ContainsKey(DateOnly.FromDateTime(DateTime.Now)) ?? false)
            {
                return trackerEntries?[DateOnly.FromDateTime(DateTime.Now)]
                    .Where(entry => entry.End == null)
                    .FirstOrDefault()?.Name;
            }

            return null;
        }

        private int GetNumberOfCurrentRunningTasks()
        {
            if (trackerEntries?.ContainsKey(DateOnly.FromDateTime(DateTime.Now)) ?? false)
            {
                return trackerEntries?[DateOnly.FromDateTime(DateTime.Now)].Where(entry => entry.End == null).Count() ?? 0;
            }

            return 0;
        }

        private bool IsRunningTaskPresent()
        {
            return GetNumberOfCurrentRunningTasks() > 0;
        }

        private void CreateAndOpenTimeTrackerSummary()
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

            using StreamWriter outputFile = new(Path.Combine(SettingsManager.PLUGIN_PATH, @"summary.md"));

            foreach (var day in dateToSummaryEntries ?? [])
            {
                outputFile.WriteLine("# Time Tracker Summary");
                outputFile.WriteLine();
                outputFile.WriteLine("## " + day.Key.ToString("dddd, d. MMMM yyyy"));
                outputFile.WriteLine();
                outputFile.WriteLine("|Name|Start|End|Duration|");
                outputFile.WriteLine("|-----|-----|-----|-----|");

                foreach (var task in day.Value)
                {
                    outputFile.WriteLine(
                        "|" +
                        task.Name +
                        "|" +
                        (task.Start?.ToString("HH:mm") ?? "") +
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
                        outputFile.WriteLine(
                        "||" +
                        (child.Start?.ToString("HH:mm") ?? "") +
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

                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = Path.Combine(SettingsManager.PLUGIN_PATH, @"summary.md"),
                        UseShellExecute = true
                    }
                );
            }
        }

        /*
        HELPER CLASSES
        */
        public class QueryResult
        {
            public Regex? Regex { get; set; }
            public Func<string, bool>? AdditionalChecks;
            public required string Title { get; set; }
            public string? Description { get; set; }
            public required string IconName { get; set; }
            public required Action<string>? Action { get; set; }
            public List<ContextData>? ContextData { get; set; }
            public string? ToolTip { get; set; }

            public bool ShouldShowResult(string queryString)
            {
                return (Regex is null || Regex.IsMatch(queryString)) &&
                    (AdditionalChecks is null || AdditionalChecks(queryString));
            }

            public Result GetResult(string queryString, SettingsManager settingsManager)
            {
                return new Result
                {
                    Title = Title,
                    SubTitle = Description,
                    IcoPath = settingsManager.IconPath + IconName,
                    Action = _ =>
                    {
                        if (Action is not null)
                            Action(queryString);

                        return true;
                    },
                    ContextData = ContextData?
                        .Select(contextMenu => contextMenu.GetContextMenuResult(queryString))
                        .ToList()
                        ?? new List<ContextMenuResult>(),
                    ToolTipData = ToolTip is not null ? new ToolTipData(ToolTip, null) : null
                };
            }
        }

        public class ContextData
        {
            /*
            Icon Font:
                - https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-ui-symbol-font
                - https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-fluent-icons-font
            */

            public required string Title { get; set; }
            public required string Glyph { get; set; }
            public required Key Key { get; set; }
            public ModifierKeys? ModifierKey { get; set; }
            public required Action<string> Action { get; set; }

            public ContextMenuResult GetContextMenuResult(string queryString)
            {
                return new ContextMenuResult
                {
                    Title = Title,
                    FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                    Glyph = Glyph,
                    AcceleratorKey = Key,
                    AcceleratorModifiers = ModifierKey ?? ModifierKeys.None,
                    Action = _ =>
                    {
                        Action(queryString);
                        return true;
                    }
                };
            }
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