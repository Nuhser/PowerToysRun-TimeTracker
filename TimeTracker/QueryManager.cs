using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Wox.Plugin;

namespace Community.Powertoys.Run.Plugin.TimeTracker
{
    public class QueryManager(SettingsManager settingsManager)
    {
        private const string COPY_GLYPH = "\xE8C8";
        private readonly JsonSerializerOptions JSON_SERIALIZER_OPTIONS = new() { WriteIndented = true };

        private Dictionary<DateOnly, List<TimeTracker.TrackerEntry>>? _trackerEntries = [];

        public List<Result> CheckQueryAndReturnResults(string queryString)
        {
            ReadTrackerEntriesFromFile();

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
                    IconName = "stop.png",
                    Action = (_) => ShowNotificationsForStoppedAndStartedTasks(AddEndTimeToAllRunningTasks(), null)
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
                    IconName = "start.png",
                    Action = (queryString) => {
                        List<TimeTracker.TrackerEntry> stoppedTasks = AddEndTimeToAllRunningTasks();
                        AddNewTrackerEntry(queryString);
                        ShowNotificationsForStoppedAndStartedTasks(stoppedTasks, queryString);
                    }
                },
                new() {
                    AdditionalChecks = (queryString) =>
                        string.IsNullOrWhiteSpace(queryString) &&
                        _trackerEntries?.Count > 0,
                    Title = "Show Time Tracker Summary",
                    IconName = "summary.png",
                    Action = (_) => CreateAndOpenTimeTrackerSummary()
                },
                new() {
                    AdditionalChecks = (queryString) =>
                        string.IsNullOrWhiteSpace(queryString) &&
                        settingsManager.ShowSavesFileSetting.Value,
                    Title = "Open Saved Tracker Entries",
                    Description = "Opens the JSON-file in which the tracked times are saved.",
                    IconName = "open.png",
                    Action = (_) => {
                        Process.Start(
                            new ProcessStartInfo
                            {
                                FileName = Path.Combine(SettingsManager.PLUGIN_PATH, SettingsManager.SAVES_NAME),
                                UseShellExecute = true
                            }
                        );
                    }
                }
            ];
        }

        private void ReadTrackerEntriesFromFile()
        {
            string jsonString;

            if (!File.Exists(Path.Combine(SettingsManager.PLUGIN_PATH, SettingsManager.SAVES_NAME)))
            {
                jsonString = JsonSerializer.Serialize(new Dictionary<DateOnly, List<TimeTracker.TrackerEntry>>());
                File.WriteAllText(Path.Combine(SettingsManager.PLUGIN_PATH, SettingsManager.SAVES_NAME), jsonString);
            }
            else
            {
                jsonString = File.ReadAllText(Path.Combine(SettingsManager.PLUGIN_PATH, SettingsManager.SAVES_NAME));
            }

            _trackerEntries = JsonSerializer.Deserialize<Dictionary<DateOnly, List<TimeTracker.TrackerEntry>>>(jsonString);
        }

        private void WriteTrackerEntriesToFile()
        {
            string jsonString = JsonSerializer.Serialize(_trackerEntries, JSON_SERIALIZER_OPTIONS);
            File.WriteAllText(Path.Combine(SettingsManager.PLUGIN_PATH, SettingsManager.SAVES_NAME), jsonString);
        }

        private void AddNewTrackerEntry(string name)
        {
            if (!_trackerEntries?.ContainsKey(DateOnly.FromDateTime(DateTime.Now)) ?? true)
            {
                _trackerEntries?.Add(DateOnly.FromDateTime(DateTime.Now), [new TimeTracker.TrackerEntry(name)]);
            }
            else
            {
                _trackerEntries?[DateOnly.FromDateTime(DateTime.Now)].Add(new TimeTracker.TrackerEntry(name));
            }

            WriteTrackerEntriesToFile();
        }

        private List<TimeTracker.TrackerEntry> AddEndTimeToAllRunningTasks()
        {
            List<TimeTracker.TrackerEntry> stoppedTasks = [];

            if (_trackerEntries != null)
            {
                foreach (var entryList in _trackerEntries.Values)
                {
                    entryList
                        .Where(entry => entry.End == null)
                        .ToList()
                        .ForEach(entry =>
                        {
                            entry.End = DateTime.Now;
                            stoppedTasks.Add(entry);
                        });
                }

                WriteTrackerEntriesToFile();
            }

            return stoppedTasks;
        }

        private void ShowNotificationsForStoppedAndStartedTasks(List<TimeTracker.TrackerEntry> stoppedTasks, string? newTasksName)
        {
            if (settingsManager.ShowNotificationsSetting.Value)
            {
                if (stoppedTasks.Count > 0)
                {
                    TimeTracker.TrackerEntry stoppedTask = stoppedTasks.First();
                    TimeSpan? duration = stoppedTask.End - stoppedTask.Start;

                    if (newTasksName == null)
                    {
                        MessageBox.Show(
                            "Stopped task '" + stoppedTask.Name + "' after " + duration?.Hours + "h " + duration?.Minutes + "m " + duration?.Seconds + "s.",
                            "Task Stopped",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }
                    else
                    {
                        MessageBox.Show(
                            "Stopped task '" + stoppedTask.Name + "' after " + duration?.Hours + "h " + duration?.Minutes + "m " + duration?.Seconds + "s.\nStarted task named '" + newTasksName + "'.",
                            "Task Stopped & New Task Started",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }
                }
                else if (newTasksName != null)
                {
                    MessageBox.Show(
                        "Started task named '" + newTasksName + "'.",
                        "New Task Started",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
        }

        private string? GetRunningTasksName()
        {
            if (_trackerEntries?.ContainsKey(DateOnly.FromDateTime(DateTime.Now)) ?? false)
            {
                return _trackerEntries?[DateOnly.FromDateTime(DateTime.Now)]
                    .Where(entry => entry.End == null)
                    .FirstOrDefault()?.Name;
            }

            return null;
        }

        private int GetNumberOfCurrentRunningTasks()
        {
            if (_trackerEntries?.ContainsKey(DateOnly.FromDateTime(DateTime.Now)) ?? false)
            {
                return _trackerEntries?[DateOnly.FromDateTime(DateTime.Now)].Where(entry => entry.End == null).Count() ?? 0;
            }

            return 0;
        }

        private bool IsRunningTaskPresent()
        {
            return GetNumberOfCurrentRunningTasks() > 0;
        }

        private void CreateAndOpenTimeTrackerSummary()
        {
            string? exportFile = null;

            switch (settingsManager.SummaryExportTypeSetting.SelectedOption)
            {
                case (int)SettingsManager.SummaryExportType.CSV:
                    exportFile = ExportManager.ExportToCSV(_trackerEntries);
                    break;
                case (int)SettingsManager.SummaryExportType.Markdown:
                    exportFile = ExportManager.ExportToMarkdown(_trackerEntries);
                    break;
                case (int)SettingsManager.SummaryExportType.HTML:
                    exportFile = ExportManager.ExportToHTML(_trackerEntries, settingsManager.HtmlExportTheme);
                    break;
            }

            if (exportFile != null)
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = exportFile,
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
    }
}