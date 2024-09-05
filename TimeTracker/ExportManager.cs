using System;
using System.Collections.Generic;
using System.Globalization;
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

        public static string ExportToHTML(
            Dictionary<DateOnly, List<TimeTracker.TrackerEntry>>? trackerEntries
        )
        {
            string exportFileName = Path.Combine(SettingsManager.PLUGIN_PATH, @"summary.html");
            using StreamWriter exportFile = new(exportFileName);

            exportFile.WriteLine(FillAndReturnSummaryTemplate(GetDateToSummaryEntriesDict(trackerEntries)));

            return exportFileName;
        }

        private static string FillAndReturnSummaryTemplate(Dictionary<DateOnly, List<SummaryEntry>> summaryEntries)
        {
            const string YEAR_BUTTON_PLACEHOLDER = "%%YEAR-BUTTON-TEMPLATE%%";
            const string YEAR_PLACEHOLDER = "%%YEAR-TEMPLATE%%";

            HashSet<string> years = GetYearsFromDateList([.. summaryEntries.Keys]);

            using StreamReader summaryTemplateFile = new(Path.Combine(SettingsManager.PLUGIN_PATH, @"util", @"html_templates", @"summary_template.html"));

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
                        years.ToList().ForEach(year => exportLines += FillAndReturnYearTemplate(summaryEntries, year, year == years.Last()));
                        break;
                    default:
                        exportLines += line;
                        break;
                }
            }

            return exportLines;
        }

        private static string FillAndReturnYearButtonTemplate(string year, bool active)
        {
            const string YEAR_ID_PLACEHOLDER = "%%YEAR-ID%%";
            const string YEAR_NAME_PLACEHOLDER = "%%YEAR-NAME%%";
            const string ACTIVE_PLACEHOLDER = "%%ACTIVE%%";

            using StreamReader yearButtonTemplateFile = new(Path.Combine(SettingsManager.PLUGIN_PATH, @"util", @"html_templates", @"year_button_template.html"));

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

        private static string FillAndReturnYearTemplate(Dictionary<DateOnly, List<SummaryEntry>> summaryEntries, string year, bool active)
        {
            const string YEAR_ID_PLACEHOLDER = "%%YEAR-ID%%";
            const string MONTH_BUTTON_PLACEHOLDER = "%%MONTH-BUTTON-TEMPLATE%%";
            const string MONTH_PLACEHOLDER = "%%MONTH-TEMPLATE%%";
            const string SHOW_ACTIVE_PLACEHOLDER = "%%SHOW-ACTIVE%%";

            HashSet<string> months = GetMonthsFromDateListByYear([.. summaryEntries.Keys], year);

            using StreamReader yearTemplateFile = new(Path.Combine(SettingsManager.PLUGIN_PATH, @"util", @"html_templates", @"year_template.html"));

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
                        months.ToList().ForEach(month => exportLines += FillAndReturnMonthTemplate(summaryEntries, month, year, (active && month == months.Last()) || (!active && month == months.First())));
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

        private static string FillAndReturnMonthButtonTemplate(string month, string year, bool active)
        {
            const string YEAR_MONTH_ID_PLACEHOLDER = "%%YEAR-MONTH-ID%%";
            const string MONTH_NAME_PLACEHOLDER = "%%MONTH-NAME%%";
            const string ACTIVE_PLACEHOLDER = "%%ACTIVE%%";

            using StreamReader monthButtonTemplateFile = new(Path.Combine(SettingsManager.PLUGIN_PATH, @"util", @"html_templates", @"month_button_template.html"));

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

        private static string FillAndReturnMonthTemplate(Dictionary<DateOnly, List<SummaryEntry>> summaryEntries, string month, string year, bool active)
        {
            const string YEAR_MONTH_ID_PLACEHOLDER = "%%YEAR-MONTH-ID%%";
            const string DATE_PLACEHOLDER = "%%DATE-TEMPLATE%%";
            const string SHOW_ACTIVE_PLACEHOLDER = "%%SHOW-ACTIVE%%";

            HashSet<DateOnly> dates = GetDatesFromListByYearAndMonth([.. summaryEntries.Keys], year, month);

            using StreamReader monthTemplateFile = new(Path.Combine(SettingsManager.PLUGIN_PATH, @"util", @"html_templates", @"month_template.html"));

            string exportLines = "";

            string? line;
            while ((line = monthTemplateFile.ReadLine()) != null)
            {
                switch (line.Trim())
                {
                    case DATE_PLACEHOLDER:
                        dates.ToList().ForEach(date => exportLines += FillAndReturnDateTemplate(date, summaryEntries[date]));
                        break;
                    default:
                        exportLines += line.Replace(YEAR_MONTH_ID_PLACEHOLDER, string.Join("-", [year, month])).Replace(SHOW_ACTIVE_PLACEHOLDER, active ? "show active" : "");
                        break;
                }
            }

            return exportLines;
        }

        private static string FillAndReturnDateTemplate(DateOnly date, List<SummaryEntry> summaryEntries)
        {
            const string DATE_ID_PLACEHOLDER = "%%DATE-ID%%";
            const string DATE_NAME_PLACEHOLDER = "%%DATE-NAME%%";
            const string TABLE_ENTRIES_PLACEHOLDER = "%%TABLE-ENTRIES%%";

            using StreamReader dateTemplateFile = new(Path.Combine(SettingsManager.PLUGIN_PATH, @"util", @"html_templates", @"date_template.html"));

            string exportLines = "";

            string? line;
            while ((line = dateTemplateFile.ReadLine()) != null)
            {
                switch (line.Trim())
                {
                    case DATE_NAME_PLACEHOLDER:
                        exportLines += date.ToString("dddd, d. MMMM yyyy") + "\n";
                        break;
                    case TABLE_ENTRIES_PLACEHOLDER:
                        summaryEntries.ForEach(entry =>
                        {
                            exportLines += "<tr>";
                            exportLines += "<td>" + entry.Name + "</td>";
                            exportLines += "<td>" + (entry.Start?.ToString("HH:mm") ?? "") + "</td>";
                            exportLines += "<td>" + (entry.End?.ToString("HH:mm") ?? "") + "</td>";
                            exportLines +=
                                "<td>" +
                                (entry.Duration != null
                                    ? (entry.Duration?.Hours + "h " + entry.Duration?.Minutes + "m " + entry.Duration?.Seconds + "s")
                                    : ""
                                ) +
                                "</td>";
                            exportLines += "</tr>";

                            foreach (var child in entry.ChildEntries)
                            {
                                exportLines += "<tr>";
                                exportLines += "<td></td>";
                                exportLines += "<td>" + (child.Start?.ToString("HH:mm") ?? "") + "</td>";
                                exportLines += "<td>" + (child.End?.ToString("HH:mm") ?? "") + "</td>";
                                exportLines +=
                                    "<td>" +
                                    (child.Duration != null
                                        ? (child.Duration?.Hours + "h " + child.Duration?.Minutes + "m " + child.Duration?.Seconds + "s")
                                        : ""
                                    ) +
                                    "</td>";
                                exportLines += "</tr>";
                            }
                        });
                        break;
                    default:
                        exportLines += line.Replace(DATE_ID_PLACEHOLDER, date.ToString("yyyy-MM-dd"));
                        break;
                }
            }

            return exportLines;
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