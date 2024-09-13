using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Community.Powertoys.Run.Plugin.TimeTracker.Utility;

namespace Community.Powertoys.Run.Plugin.TimeTracker
{
    public class ExportService(SettingsManager settingsManager)
    {
        private readonly SettingsManager _settingsManager = settingsManager;

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

        public string ExportToMarkdown(
            Dictionary<DateOnly, List<Data.TrackerEntry>>? trackerEntries
        )
        {
            string exportFileName = Path.Combine(_settingsManager.PluginInstallationPath!, @"summary.md");

            using StreamWriter exportFile = new(exportFileName);

            exportFile.WriteLine("# Time Tracker Summary");
            exportFile.WriteLine();

            foreach (var day in trackerEntries ?? [])
            {
                TimeSpan? totalDuration = day.Value.Select(entry => entry.GetDuration(true)).Aggregate((a, b) => a?.Add(b ?? TimeSpan.Zero) ?? b?.Add(a ?? TimeSpan.Zero));

                exportFile.WriteLine("## " + day.Key.ToString("dddd, d. MMMM yyyy") + ((totalDuration != null) ? (" (Total: " + GetDurationAsString(totalDuration) + ")") : ""));
                exportFile.WriteLine();
                exportFile.WriteLine("|Name|Start|End|Duration|");
                exportFile.WriteLine("|-----|-----|-----|-----|");

                foreach (var task in day.Value)
                {
                    exportFile.WriteLine(
                        "|" +
                        task.Name +
                        "|" +
                        (task.GetStart()?.ToString("HH:mm") ?? " ") +
                        "|" +
                        (task.GetEnd()?.ToString("HH:mm") ?? " ") +
                        "|" +
                        GetDurationAsString(task.GetDuration(true)) +
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
                                GetDurationAsString(child.GetDuration(true)) +
                                "|"
                            );
                        }
                    }
                }

                exportFile.WriteLine();
            }

            return exportFileName;
        }

        public string ExportToCSV(
            Dictionary<DateOnly, List<Data.TrackerEntry>>? trackerEntries
        )
        {
            string exportFileName = Path.Combine(_settingsManager.PluginInstallationPath!, @"summary.csv");

            using StreamWriter exportFile = new(exportFileName);

            exportFile.WriteLine("Date,Name,Start,End,Duration");

            foreach (var day in trackerEntries ?? [])
            {
                foreach (var task in day.Value)
                {
                    exportFile.WriteLine(string.Join(",", [
                        day.Key.ToString("dd.MM.yyyy"),
                        task.Name,
                        (task.GetStart()?.ToString("HH:mm") ?? ""),
                        (task.GetEnd()?.ToString("HH:mm") ?? ""),
                        GetDurationAsString(task.GetDuration(true))
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
                            GetDurationAsString(child.GetDuration(true))
                            ]));
                        }
                    }
                }
            }

            return exportFileName;
        }

        public string ExportToHTML(
            Data? data,
            string theme
        )
        {
            string exportFileName = Path.Combine(_settingsManager.PluginInstallationPath!, @"summary.html");
            using StreamWriter exportFile = new(exportFileName);

            exportFile.WriteLine(FillAndReturnSummaryTemplate(data, theme));

            return exportFileName;
        }

        private string FillAndReturnSummaryTemplate(Data? data, string theme)
        {
            const string YEAR_BUTTON_PLACEHOLDER = "%%YEAR-BUTTON-TEMPLATE%%";
            const string YEAR_PLACEHOLDER = "%%YEAR-TEMPLATE%%";
            const string THEME_PLACEHOLDER = "%%THEME%%";

            HashSet<string> years = GetYearsFromDateList([.. data?.TrackerEntries.Keys]);

            using StreamReader summaryTemplateFile = new(Path.Combine(_settingsManager.PluginInstallationPath!, @"util", @"html_templates", @"summary_template.html"));

            string exportLines = "";

            string? line;
            while ((line = summaryTemplateFile.ReadLine()) != null)
            {
                switch (line.Trim())
                {
                    case YEAR_BUTTON_PLACEHOLDER:
                        years.ToList().ForEach(year => exportLines += FillAndReturnYearButtonTemplate(year, year == years.Last()));
                        break;
                    case YEAR_PLACEHOLDER:
                        years.ToList().ForEach(year => exportLines += FillAndReturnYearTemplate(data, year, year == years.Last()));
                        break;
                    default:
                        exportLines += line.Replace(THEME_PLACEHOLDER, theme);
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

        private string FillAndReturnYearTemplate(Data? data, string year, bool active)
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

        private string FillAndReturnMonthTemplate(Data? data, string month, string year, bool active)
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

        private string FillAndReturnDateTemplate(DateOnly date, Data? data, bool active)
        {
            const string DATE_ID_PLACEHOLDER = "%%DATE-ID%%";
            const string DATE_NAME_PLACEHOLDER = "%%DATE-NAME%%";
            const string TABLE_ENTRIES_PLACEHOLDER = "%%TABLE-ENTRIES%%";
            const string SHOW_PLACEHOLDER = "%%SHOW%%";
            const string COLLAPSED_PLACEHOLDER = "%%COLLAPSED%%";

            TimeSpan? totalDuration = data?.GetTotalDurationForDay(date, true);

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
                                ((data?.IsTaskRunningForDate(date) ?? false) ? "<span class='material-symbols-outlined ms-2'>acute</span>" : "") +
                                ")")
                            : "") +
                        "\n";
                        break;
                    case TABLE_ENTRIES_PLACEHOLDER:
                        data?.TrackerEntries[date].ForEach(entry =>
                        {
                            exportLines += "<tr>";
                            exportLines += "<td>" + entry.Name + "</td>";
                            exportLines += "<td>" + (entry.GetStart()?.ToString("HH:mm") ?? "") + "</td>";
                            exportLines += "<td>" + (entry.GetEnd()?.ToString("HH:mm") ?? "") + "</td>";
                            exportLines +=
                                "<td>" +
                                GetDurationAsString(entry.GetDuration(true)) +
                                (entry.Running ? "<span class='material-symbols-outlined ms-2'>acute</span>" : "") +
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
                                        GetDurationAsString(child.GetDuration(true)) +
                                        (child.Running ? "<span class='material-symbols-outlined ms-2'>acute</span>" : "") +
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