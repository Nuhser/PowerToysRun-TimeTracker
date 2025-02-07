using Community.Powertoys.Run.Plugin.TimeTracker.Data;
using Community.Powertoys.Run.Plugin.TimeTracker.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Wox.Plugin.Logger;
using static Community.Powertoys.Run.Plugin.TimeTracker.Platform.Utility;

namespace Community.Powertoys.Run.Plugin.TimeTracker
{
    public class ExportService(SettingsManager settingsManager, DataManager dataManager)
    {
        private readonly SettingsManager _settingsManager = settingsManager;
        private readonly DataManager _dataManager = dataManager;

        private static HashSet<string> GetYearsFromDateList(List<DateOnly> dates)
        {
            return dates.Select(date => date.ToString("yyyy")).ToHashSet();
        }

        private static HashSet<string> GetMonthsFromDateListByYear(List<DateOnly> dates, string year)
        {
            return dates.Where(date => date.ToString("yyyy") == year).Select(date => date.ToString("MMMM")).ToHashSet();
        }

        private static HashSet<DateOnly> GetDatesFromListByYearAndMonth(List<DateOnly> dates, string year, string month)
        {
            return dates.Where(date => date.ToString("yyyy") == year).Where(date => date.ToString("MMMM") == month).ToHashSet();
        }

        private string? CheckRegexAndGetLinkForTaskName(string name)
        {
            if ((_settingsManager.TaskLinkRegexSetting.ValueAsList?.Count % 2) != 0)
            {
                Log.Warn("The number of regex and link lines in setting '" + _settingsManager.TaskLinkRegexSetting.Label + "' doesn't match. This setting's number of lines should always be an even number. The Matching will therefore be skipped.", GetType());
                return null;
            }

            for (int i = 0; i < _settingsManager.TaskLinkRegexSetting.ValueAsList?.Count; i += 2)
            {
                string regex = _settingsManager.TaskLinkRegexSetting.ValueAsList?[i]!;
                string link = _settingsManager.TaskLinkRegexSetting.ValueAsList?[i + 1]!;

                if (!Uri.TryCreate(link, UriKind.Absolute, out _))
                {
                    Log.Warn("The link '" + link + "' from the setting '" + _settingsManager.TaskLinkRegexSetting.Label + "' doesn't seem to be valid and will therefore be skipped.", GetType());
                    continue;
                }

                if (string.IsNullOrWhiteSpace(regex))
                {
                    Log.Warn("The RegEx '" + regex + "' from the setting '" + _settingsManager.TaskLinkRegexSetting.Label + "' doesn't seem to be valid and will therefore be skipped.", GetType());
                    continue;
                }

                try
                {
                    if (Regex.IsMatch(name, (regex.StartsWith('^') ? "" : "^") + regex + (regex.EndsWith('$') ? "" : "$")))
                    {
                        return link.Replace("§", name);
                    }
                }
                catch (ArgumentException)
                {
                    Log.Warn("The RegEx '" + regex + "' from the setting '" + _settingsManager.TaskLinkRegexSetting.Label + "' doesn't seem to be valid and will therefore be skipped.", GetType());
                    continue;
                }
            }

            return null;
        }

        public string ExportToMarkdown()
        {
            string exportFileName = Path.Combine(_settingsManager.PluginInstallationPath!, @"summary.md");

            using StreamWriter exportFile = new(exportFileName);

            exportFile.WriteLine("# Time Tracker Summary");
            exportFile.WriteLine();

            foreach (var day in _dataManager.Data?.TrackerEntries ?? [])
            {
                TimeSpan? totalDuration = _dataManager.GetTotalDurationForDay(day.Key, _settingsManager.ShowRunningDurationsSetting.Value);

                exportFile.WriteLine(
                    "## " +
                    day.Key.ToString("dddd, d. MMMM yyyy") +
                    ((totalDuration != null)
                        ? (
                            " (Total: " +
                            ((_dataManager.IsTaskRunningForDate(day.Key) && _settingsManager.ShowRunningDurationsSetting.Value) ? "~" : "") +
                            GetDurationAsString(totalDuration) +
                            ")"
                        )
                        : "")
                );
                exportFile.WriteLine();
                exportFile.WriteLine("|Name|Start|End|Duration|");
                exportFile.WriteLine("|-----|-----|-----|-----|");

                foreach (var task in day.Value)
                {
                    string? link;
                    string nameOrLink = (link = CheckRegexAndGetLinkForTaskName(task.Name)) != null
                        ? "[" + task.Name + "](" + link + ")"
                        : task.Name;

                    exportFile.WriteLine(
                        "|" +
                        nameOrLink +
                        "|" +
                        (task.GetStart()?.ToString("HH:mm") ?? " ") +
                        "|" +
                        (task.GetEnd()?.ToString("HH:mm") ?? " ") +
                        "|" +
                        (task.Running && _settingsManager.ShowRunningDurationsSetting.Value ? "~" : "") +
                        GetDurationAsString(task.GetDuration(_settingsManager.ShowRunningDurationsSetting.Value)) +
                        "|"
                    );

                    if (task.HasSubEntries())
                    {
                        foreach (var child in task.SubEntries)
                        {
                            exportFile.WriteLine(
                                "||" +
                                (child.Start.ToString("HH:mm") ?? " ") +
                                "|" +
                                (child.End?.ToString("HH:mm") ?? " ") +
                                "|" +
                                (child.Running && _settingsManager.ShowRunningDurationsSetting.Value ? "~" : "") +
                                GetDurationAsString(child.GetDuration(_settingsManager.ShowRunningDurationsSetting.Value)) +
                                "|"
                            );
                        }
                    }
                }

                exportFile.WriteLine();
            }

            return exportFileName;
        }

        public string ExportToCSV()
        {
            string exportFileName = Path.Combine(_settingsManager.PluginInstallationPath!, @"summary.csv");

            using StreamWriter exportFile = new(exportFileName);

            exportFile.WriteLine("Date,Name,Start,End,Duration");

            foreach (var day in _dataManager.Data?.TrackerEntries ?? [])
            {
                foreach (var task in day.Value)
                {
                    exportFile.WriteLine(string.Join(",", [
                        day.Key.ToString("dd.MM.yyyy"),
                        task.Name,
                        (task.GetStart()?.ToString("HH:mm") ?? ""),
                        (task.GetEnd()?.ToString("HH:mm") ?? ""),
                        (task.Running && _settingsManager.ShowRunningDurationsSetting.Value ? "~" : "") +
                        GetDurationAsString(task.GetDuration(_settingsManager.ShowRunningDurationsSetting.Value))
                    ]));

                    if (task.HasSubEntries())
                    {
                        foreach (var child in task.SubEntries)
                        {
                            exportFile.WriteLine(string.Join(",", [
                                "",
                            "",
                            (child.Start.ToString("HH:mm") ?? ""),
                            (child.End?.ToString("HH:mm") ?? ""),
                            (child.Running && _settingsManager.ShowRunningDurationsSetting.Value ? "~" : "") +
                            GetDurationAsString(child.GetDuration(_settingsManager.ShowRunningDurationsSetting.Value))
                            ]));
                        }
                    }
                }
            }

            return exportFileName;
        }

        public string ExportToHTML()
        {
            string theme = _settingsManager.HtmlExportTheme!;
            string exportFileName = Path.Combine(_settingsManager.PluginInstallationPath!, @"summary.html");
            using StreamWriter exportFile = new(exportFileName);

            exportFile.WriteLine(FillAndReturnSummaryTemplate(_dataManager.Data, theme));

            return exportFileName;
        }

        private string FillAndReturnSummaryTemplate(DataHolder? data, string theme)
        {
            const string YEAR_BUTTON_PLACEHOLDER = "%%YEAR-BUTTON-TEMPLATE%%";
            const string YEAR_PLACEHOLDER = "%%YEAR-TEMPLATE%%";
            const string THEME_PLACEHOLDER = "%%THEME%%";
            const string HEADER_STYLE_PLACEHOLDER = "%%CUSTOM-HEADER-STYLE%%";
            const string CUSTOM_HEADER_PLACEHOLDER = "%%CUSTOM-HEADER%%";
            const string FOOTER_STYLE_PLACEHOLDER = "%%CUSTOM-FOOTER-STYLE%%";
            const string CUSTOM_FOOTER_PLACEHOLDER = "%%CUSTOM-FOOTER%%";

            HashSet<string> years = GetYearsFromDateList([.. data?.TrackerEntries.Keys]);

            using StreamReader summaryTemplateFile = new(Path.Combine(_settingsManager.PluginInstallationPath!, @"util", @"html_templates", @"summary_template.html"));

            string exportLines = "";

            string? line;
            while ((line = summaryTemplateFile.ReadLine()) != null)
            {
                switch (line.Trim())
                {
                    case CUSTOM_HEADER_PLACEHOLDER:
                        exportLines += _settingsManager.CustomHtmlHeaderSetting.Value;
                        break;
                    case CUSTOM_FOOTER_PLACEHOLDER:
                        exportLines += _settingsManager.CustomHtmlFooterSetting.Value;
                        break;
                    case YEAR_BUTTON_PLACEHOLDER:
                        years.ToList().ForEach(year => exportLines += FillAndReturnYearButtonTemplate(year, year == years.Last()));
                        break;
                    case YEAR_PLACEHOLDER:
                        years.ToList().ForEach(year => exportLines += FillAndReturnYearTemplate(data, year, year == years.Last()));
                        break;
                    default:
                        exportLines += line
                            .Replace(THEME_PLACEHOLDER, theme)
                            .Replace(HEADER_STYLE_PLACEHOLDER, string.IsNullOrWhiteSpace(_settingsManager.CustomHtmlHeaderSetting.Value) ? "display: none;" : "")
                            .Replace(FOOTER_STYLE_PLACEHOLDER, string.IsNullOrWhiteSpace(_settingsManager.CustomHtmlFooterSetting.Value) ? "display: none;" : "");
                        break;
                }
            }

            return exportLines;
        }

        private string FillAndReturnYearButtonTemplate(string year, bool active)
        {
            const string YEAR_ID_PLACEHOLDER = "%%YEAR-ID%%";
            const string YEAR_NAME_PLACEHOLDER = "%%YEAR-NAME%%";
            const string ACTIVE_PLACEHOLDER = "%%ACTIVE%%";

            using StreamReader yearButtonTemplateFile = new(Path.Combine(_settingsManager.PluginInstallationPath!, @"util", @"html_templates", @"year_button_template.html"));

            string exportLines = "";

            string? line;
            while ((line = yearButtonTemplateFile.ReadLine()) != null)
            {
                exportLines += line
                    .Replace(YEAR_ID_PLACEHOLDER, year)
                    .Replace(YEAR_NAME_PLACEHOLDER, year)
                    .Replace(ACTIVE_PLACEHOLDER, active ? "active" : "");
            }

            return exportLines;
        }

        private string FillAndReturnYearTemplate(DataHolder? data, string year, bool active)
        {
            const string YEAR_ID_PLACEHOLDER = "%%YEAR-ID%%";
            const string MONTH_BUTTON_PLACEHOLDER = "%%MONTH-BUTTON-TEMPLATE%%";
            const string MONTH_PLACEHOLDER = "%%MONTH-TEMPLATE%%";
            const string SHOW_ACTIVE_PLACEHOLDER = "%%SHOW-ACTIVE%%";

            HashSet<string> months = GetMonthsFromDateListByYear([.. data?.TrackerEntries.Keys], year);

            using StreamReader yearTemplateFile = new(Path.Combine(_settingsManager.PluginInstallationPath!, @"util", @"html_templates", @"year_template.html"));

            string exportLines = "";

            string? line;
            while ((line = yearTemplateFile.ReadLine()) != null)
            {
                switch (line.Trim())
                {
                    case MONTH_BUTTON_PLACEHOLDER:
                        months.ToList().ForEach(month => exportLines += FillAndReturnMonthButtonTemplate(month, year, (active && month == months.Last()) || (!active && month == months.First())));
                        break;
                    case MONTH_PLACEHOLDER:
                        months.ToList().ForEach(month => exportLines += FillAndReturnMonthTemplate(data, month, year, (active && month == months.Last()) || (!active && month == months.First())));
                        break;
                    default:
                        exportLines += line
                            .Replace(YEAR_ID_PLACEHOLDER, year)
                            .Replace(SHOW_ACTIVE_PLACEHOLDER, active ? "show active" : "");
                        break;
                }
            }

            return exportLines;
        }

        private string FillAndReturnMonthButtonTemplate(string month, string year, bool active)
        {
            const string YEAR_MONTH_ID_PLACEHOLDER = "%%YEAR-MONTH-ID%%";
            const string MONTH_NAME_PLACEHOLDER = "%%MONTH-NAME%%";
            const string ACTIVE_PLACEHOLDER = "%%ACTIVE%%";

            using StreamReader monthButtonTemplateFile = new(Path.Combine(_settingsManager.PluginInstallationPath!, @"util", @"html_templates", @"month_button_template.html"));

            string exportLines = "";

            string? line;
            while ((line = monthButtonTemplateFile.ReadLine()) != null)
            {
                exportLines += line.Trim() switch
                {
                    MONTH_NAME_PLACEHOLDER => month + "\n",
                    _ => line.Replace(YEAR_MONTH_ID_PLACEHOLDER, string.Join("-", [year, month])).Replace(ACTIVE_PLACEHOLDER, active ? "active" : ""),
                };
            }

            return exportLines;
        }

        private string FillAndReturnMonthTemplate(DataHolder? data, string month, string year, bool active)
        {
            const string YEAR_MONTH_ID_PLACEHOLDER = "%%YEAR-MONTH-ID%%";
            const string DATE_PLACEHOLDER = "%%DATE-TEMPLATE%%";
            const string SHOW_ACTIVE_PLACEHOLDER = "%%SHOW-ACTIVE%%";

            HashSet<DateOnly> dates = GetDatesFromListByYearAndMonth([.. data?.TrackerEntries.Keys], year, month);

            using StreamReader monthTemplateFile = new(Path.Combine(_settingsManager.PluginInstallationPath!, @"util", @"html_templates", @"month_template.html"));

            string exportLines = "";

            string? line;
            while ((line = monthTemplateFile.ReadLine()) != null)
            {
                switch (line.Trim())
                {
                    case DATE_PLACEHOLDER:
                        dates.ToList().ForEach(date => exportLines += FillAndReturnDateTemplate(date, data, active && date == dates.Last()));
                        break;
                    default:
                        exportLines += line.Replace(YEAR_MONTH_ID_PLACEHOLDER, string.Join("-", [year, month])).Replace(SHOW_ACTIVE_PLACEHOLDER, active ? "show active" : "");
                        break;
                }
            }

            return exportLines;
        }

        private string FillAndReturnDateTemplate(DateOnly date, DataHolder? data, bool active)
        {
            const string DATE_ID_PLACEHOLDER = "%%DATE-ID%%";
            const string DATE_NAME_PLACEHOLDER = "%%DATE-NAME%%";
            const string TABLE_ENTRIES_PLACEHOLDER = "%%TABLE-ENTRIES%%";
            const string SHOW_PLACEHOLDER = "%%SHOW%%";
            const string COLLAPSED_PLACEHOLDER = "%%COLLAPSED%%";

            TimeSpan? totalDuration = _dataManager.GetTotalDurationForDay(date, _settingsManager.ShowRunningDurationsSetting.Value);

            using StreamReader dateTemplateFile = new(Path.Combine(_settingsManager.PluginInstallationPath!, @"util", @"html_templates", @"date_template.html"));

            string exportLines = "";

            string? line;
            while ((line = dateTemplateFile.ReadLine()) != null)
            {
                switch (line.Trim())
                {
                    case DATE_NAME_PLACEHOLDER:
                        exportLines += date.ToString("dddd, d. MMMM yyyy") +
                        ((totalDuration != null)
                            ? (" (Total: " +
                                GetDurationAsString(totalDuration) +
                                ((_dataManager.IsTaskRunningForDate(date) && _settingsManager.ShowRunningDurationsSetting.Value) ? "<span class='material-symbols-outlined ms-2'>acute</span>" : "") +
                                ")")
                            : "") +
                        "\n";
                        break;
                    case TABLE_ENTRIES_PLACEHOLDER:
                        data?.TrackerEntries[date].ForEach(entry =>
                        {
                            string? link;
                            string nameOrLink = (link = CheckRegexAndGetLinkForTaskName(entry.Name)) != null
                                ? "<a class='link-offset-2 link-offset-2-hover link-underline link-underline-opacity-0 link-underline-opacity-75-hover' href='" + link + "' target='_blank'>" + entry.Name + "</a>"
                                : entry.Name;

                            exportLines += "<tr>";
                            exportLines += "<td>" + nameOrLink + "</td>";
                            exportLines += "<td>" + (entry.GetStart()?.ToString("HH:mm") ?? "") + "</td>";
                            exportLines += "<td>" + (entry.GetEnd()?.ToString("HH:mm") ?? "") + "</td>";
                            exportLines +=
                                "<td>" +
                                GetDurationAsString(entry.GetDuration(_settingsManager.ShowRunningDurationsSetting.Value)) +
                                (entry.Running && _settingsManager.ShowRunningDurationsSetting.Value ? "<span class='material-symbols-outlined ms-2'>acute</span>" : "") +
                                "</td>";
                            exportLines += "</tr>";

                            if (entry.HasSubEntries())
                            {
                                foreach (var child in entry.SubEntries)
                                {
                                    exportLines += "<tr>";
                                    exportLines += "<td></td>";
                                    exportLines += "<td>" + (child.Start.ToString("HH:mm") ?? "") + "</td>";
                                    exportLines += "<td>" + (child.End?.ToString("HH:mm") ?? "") + "</td>";
                                    exportLines +=
                                        "<td>" +
                                        GetDurationAsString(child.GetDuration(_settingsManager.ShowRunningDurationsSetting.Value)) +
                                        (child.Running && _settingsManager.ShowRunningDurationsSetting.Value ? "<span class='material-symbols-outlined ms-2'>acute</span>" : "") +
                                        "</td>";
                                    exportLines += "</tr>";
                                }
                            }
                        });
                        break;
                    default:
                        exportLines += line.Replace(DATE_ID_PLACEHOLDER, date.ToString("yyyy-MM-dd"))
                            .Replace(SHOW_PLACEHOLDER, active ? "show" : "")
                            .Replace(COLLAPSED_PLACEHOLDER, !active ? "collapsed" : "");
                        break;
                }
            }

            return exportLines;
        }
    }
}