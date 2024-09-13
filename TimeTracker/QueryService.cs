using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Wox.Plugin;
using static Community.Powertoys.Run.Plugin.TimeTracker.Utility;

namespace Community.Powertoys.Run.Plugin.TimeTracker
{
    public class QueryService(SettingsManager settingsManager, ExportService exportService)
    {
        private const string COPY_GLYPH = "\xE8C8";

        private readonly SettingsManager _settingsManager = settingsManager;
        private readonly ExportService _exportService = exportService;

        private Data? _data = null;
        private bool _jsonBroken = false;

        public List<Result> CheckQueryAndReturnResults(string queryString)
        {
            ReadTrackerEntriesFromFile();

            return GetQueryResults(queryString)
                .Where(result => result.ShouldShowResult(queryString))
                .Select(result => result.GetResult(queryString, _settingsManager))
                .ToList();
        }

        public List<QueryResult> GetQueryResults(string queryString)
        {
            return _jsonBroken
                ? []
                : [
                    new() {
                        AdditionalChecks = (queryString) =>
                            string.IsNullOrWhiteSpace(queryString) &&
                            (_data?.IsTaskRunning() ?? false),
                        Title = "Stop Currently Running Task",
                        Description = "Stops the currently running task(s) " + _data?.GetNamesOfRunningTasksAsString() + ".",
                        IconName = "stop.png",
                        Action = (_) => ShowNotificationsForStoppedAndStartedTasks(AddEndTimeToAllRunningTasks(), null)
                    },
                    new() {
                        AdditionalChecks = (queryString) =>
                            !string.IsNullOrWhiteSpace(queryString),
                        Title = "Start New Task",
                        Description =
                            (_data?.IsTaskRunning() ?? false)
                                ? "Stops the currently running task(s) " + _data?.GetNamesOfRunningTasksAsString() + " and starts a new one named '" + queryString + "'."
                                : "Starts a new task named '" + queryString + "'.",
                        IconName = "start.png",
                        Action = (queryString) => {
                            List<(string, TimeSpan?)> stoppedTasks = AddEndTimeToAllRunningTasks();
                            _data?.AddTrackerEntry(queryString);
                            ShowNotificationsForStoppedAndStartedTasks(stoppedTasks, queryString);
                        }
                    },
                    new() {
                        AdditionalChecks = (queryString) =>
                            string.IsNullOrWhiteSpace(queryString) &&
                            _data?.TrackerEntries.Count > 0,
                        Title = "Show Time Tracker Summary",
                        IconName = "summary.png",
                        Action = (_) => CreateAndOpenTimeTrackerSummary()
                    },
                    new() {
                        AdditionalChecks = (queryString) =>
                            string.IsNullOrWhiteSpace(queryString) &&
                            _settingsManager.ShowSavesFileSetting.Value,
                        Title = "Open Saved Tracker Entries",
                        Description = "Opens the JSON-file in which the tracked times are saved.",
                        IconName = "open.png",
                        Action = (_) => {
                            Process.Start(
                                new ProcessStartInfo
                                {
                                    FileName = SettingsManager.DATA_PATH,
                                    UseShellExecute = true
                                }
                            );
                        }
                    }
                ];
        }

        private void ReadTrackerEntriesFromFile()
        {
            if (Data.FromJson(out _data))
            {
                _jsonBroken = false;
            }
            else
            {
                if (!_jsonBroken)
                {
                    _jsonBroken = true;

                    if (MessageBoxResult.Yes == MessageBox.Show(
                        "The JSON containing your tracker data seems to be broken and needs fixing.\nDo you wan't to fix it now?",
                        "Data-JSON Needs Repair",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Error
                        ))
                    {
                        Process.Start(
                            new ProcessStartInfo
                            {
                                FileName = SettingsManager.DATA_PATH,
                                UseShellExecute = true
                            }
                        );
                    }
                }
            }
        }

        private List<(string, TimeSpan?)> AddEndTimeToAllRunningTasks()
        {
            List<(string, TimeSpan?)> stoppedTasks = [];

            if (_data != null)
            {
                foreach (var entryList in _data.TrackerEntries.Values)
                {
                    entryList.ForEach(entry =>
                    {
                        entry.SubEntries
                            .Where(subEntry => subEntry.Running)
                            .ToList()
                            .ForEach(subEntry =>
                            {
                                subEntry.End = DateTime.Now;
                                stoppedTasks.Add((entry.Name, subEntry.Duration));
                            });
                    });
                }

                _data.ToJson();
            }

            return stoppedTasks;
        }

        private void ShowNotificationsForStoppedAndStartedTasks(List<(string, TimeSpan?)> stoppedTasks, string? newTasksName)
        {
            if (_settingsManager.ShowNotificationsSetting.Value)
            {
                if (stoppedTasks.Count > 0)
                {
                    (string stoppedTaskName, TimeSpan? stoppedTaskDuration) = stoppedTasks.First();

                    if (newTasksName == null)
                    {
                        MessageBox.Show(
                            "Stopped task '" + stoppedTaskName + "' after " + GetDurationAsString(stoppedTaskDuration) + ".",
                            "Task Stopped",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }
                    else
                    {
                        MessageBox.Show(
                            "Stopped task '" + stoppedTaskName + "' after " + GetDurationAsString(stoppedTaskDuration) + ".\nStarted task named '" + newTasksName + "'.",
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

        private void CreateAndOpenTimeTrackerSummary()
        {
            string? exportFile = null;

            switch (_settingsManager.SummaryExportTypeSetting.SelectedOption)
            {
                case (int)SettingsManager.SummaryExportType.CSV:
                    exportFile = _exportService.ExportToCSV(_data?.TrackerEntries);
                    break;
                case (int)SettingsManager.SummaryExportType.Markdown:
                    exportFile = _exportService.ExportToMarkdown(_data?.TrackerEntries);
                    break;
                case (int)SettingsManager.SummaryExportType.HTML:
                    exportFile = _exportService.ExportToHTML(_data?.TrackerEntries, _settingsManager.HtmlExportTheme!);
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

            public Result GetResult(string queryString, SettingsManager _settingsManager)
            {
                return new Result
                {
                    Title = Title,
                    SubTitle = Description,
                    IcoPath = _settingsManager.IconPath + IconName,
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