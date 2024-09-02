using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Wox.Plugin;

namespace Community.Powertoys.Run.Plugin.TimeTracker
{
    public partial class QueryManager(SettingsManager settingsManager)
    {
        private const string COPY_GLYPH = "\xE8C8";

        public List<Result> CheckQueryAndReturnResults(string queryString)
        {
            return GetQueryResults()
                .Where(result => result.ShouldShowResult(queryString))
                .Select(result => result.GetResult(queryString, settingsManager))
                .ToList();
        }

        public List<QueryResult> GetQueryResults() {
            return [];
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