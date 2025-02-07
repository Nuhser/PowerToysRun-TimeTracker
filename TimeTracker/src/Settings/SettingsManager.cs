using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Community.Powertoys.Run.Plugin.TimeTracker.Platform;
using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using Wox.Plugin;

namespace Community.Powertoys.Run.Plugin.TimeTracker.Settings
{
    public class SettingsManager : AbstractDataManager<SettingsHolder>
    {
        public static readonly string SETTINGS_DIRECTORY_PATH =
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
            @"\Microsoft\PowerToys\PowerToys Run\Settings\Plugins\Community.PowerToys.Run.Plugin.TimeTracker\";

        public static readonly string DATA_PATH = Path.Combine(SETTINGS_DIRECTORY_PATH, "data.json");

        private string? iconPath;
        public string? IconPath
        {
            get { return iconPath ?? throw new UnreachableException("SettingsManager.IconPath was read before first init during TimeTracker#Init"); }
            set { iconPath = value; }
        }

        private string? htmlExportTheme;
        public string? HtmlExportTheme
        {
            get { return htmlExportTheme ?? throw new UnreachableException("SettingsManager.HtmlExportTheme was read before first init during TimeTracker#Init"); }
            set { htmlExportTheme = value; }
        }

        private string? pluginInstallationPath;
        public string? PluginInstallationPath
        {
            get { return pluginInstallationPath ?? throw new UnreachableException("SettingsManager.PluginInstallationPath was read before first init during TimeTracker#Init"); }

            set { pluginInstallationPath = value; }
        }

        public SettingsManager()
        {
            if (!Directory.Exists(SETTINGS_DIRECTORY_PATH))
            {
                Directory.CreateDirectory(SETTINGS_DIRECTORY_PATH);
            }
        }

        public override string GetDataFilePath()
        {
            return Path.Combine(DATA_BASE_DIRECTORY_PATH, "settings.json");
        }

        public enum SummaryExportType
        {
            CSV,
            Markdown,
            HTML
        }

        public DropdownSetting SummaryExportTypeSetting = new()
        {
            Key = "summary_export_type",
            Label = "Summary Export Type",
            Description = "The file type to use when creating a time tracker summary.",
            SelectedOption = (int)SummaryExportType.HTML,
            SelectableOptions = Enum.GetNames(typeof(SummaryExportType))
        };

        public BooleanSetting ShowRunningDurationsSetting = new()
        {
            Key = "show_running_durations",
            Label = "Show Duration for Running Tasks in Summary",
            Description = "Show/hide current duration for still running tasks in the summaries. Still running tasks will be marked."
        };

        public BooleanSetting ShowNotificationsSetting = new()
        {
            Key = "show_notifications",
            Label = "Enable Pop-up Notifications",
            Description = "Enables/disables pop-ups when starting or stopping a task.",
            Value = false
        };

        public BooleanSetting ShowSavesFileSetting = new()
        {
            Key = "show_saves_file",
            Label = "Enable Data-File Editing",
            Description = "Show/hide option to open the saves-file. Can be used to edit the data.",
            Value = false
        };

        public MultilineTextSetting TaskAliasSettings = new()
        {
            Key = "task_aliases",
            Label = "Task Name Aliases",
            Description = "A list of task names and their possible aliases in the following format: <ALIAS>|<TASK-NAME>"
        };

        public MultilineTextSetting TaskLinkRegexSetting = new()
        {
            Key = "task_link_regex",
            Label = "Task Name RegEx Matching"
        };

        public MultilineTextSetting CustomHtmlHeaderSetting = new()
        {
            Key = "custom_html_header",
            Label = "Custom HTML-Summary Header",
            Description = "Custom HTML code that should be displayed in HTML-sumary exports between the title and the buttons."
        };

        public MultilineTextSetting CustomHtmlFooterSetting = new()
        {
            Key = "custom_html_footer",
            Label = "Custom HTML-Summary Footer",
            Description = "Custom HTML code that should be displayed in HTML-sumary exports below the table containing the days."
        };

        public List<Setting> GetSettings()
        {
            return [
                ShowNotificationsSetting,
                ShowSavesFileSetting,
                TaskAliasSettings,
                SummaryExportTypeSetting,
                ShowRunningDurationsSetting,
                TaskLinkRegexSetting,
                CustomHtmlHeaderSetting,
                CustomHtmlFooterSetting
            ];
        }

        public void InitFromContext(PluginInitContext context)
        {
            PluginInstallationPath = Path.GetDirectoryName(context.CurrentPluginMetadata.ExecuteFilePath);
            SetPluginTheme(context.API.GetCurrentTheme());
        }

        public void SetPluginTheme(Theme theme)
        {
            if (theme is Theme.Light or Theme.HighContrastWhite)
            {
                IconPath = "icons/light/";
                HtmlExportTheme = "light";
            }
            else
            {
                IconPath = "icons/dark/";
                HtmlExportTheme = "dark";
            }
        }

        public List<PluginAdditionalOption> GetOptions()
        {
            return GetSettings().Select<Setting, PluginAdditionalOption>(s => s.GetAsOption()).ToList();
        }

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            if (settings?.AdditionalOptions is null)
            {
                return;
            }

            foreach (var option in settings.AdditionalOptions)
            {
                GetSettings()
                    .Where(setting => setting.Key == option.Key)
                    .ToList()
                    .ForEach(setting => setting.SetValue(option));
            }
        }

        /* HELPER CLASSES */
        public abstract class Setting
        {
            public required string Key { get; set; }
            public required string Label { get; set; }
            public string? Description { get; set; }
            public abstract PluginAdditionalOption.AdditionalOptionType OptionType { get; }

            public abstract void SetValue(PluginAdditionalOption option);

            protected abstract void AddTypeSpecificOptionProperties(PluginAdditionalOption option);

            public PluginAdditionalOption GetAsOption()
            {
                PluginAdditionalOption option = new()
                {
                    Key = Key,
                    DisplayLabel = Label,
                    DisplayDescription = Description,
                    PluginOptionType = OptionType
                };

                AddTypeSpecificOptionProperties(option);
                return option;
            }
        }

        public class BooleanSetting : Setting
        {
            public override PluginAdditionalOption.AdditionalOptionType OptionType { get; } = PluginAdditionalOption.AdditionalOptionType.Checkbox;
            public bool Value { get; set; } = true;

            public override void SetValue(PluginAdditionalOption option)
            {
                Value = option.Value;
            }

            protected override void AddTypeSpecificOptionProperties(PluginAdditionalOption option)
            {
                option.Value = Value;
            }
        }

        public class NumberSetting : Setting
        {
            public override PluginAdditionalOption.AdditionalOptionType OptionType { get; } = PluginAdditionalOption.AdditionalOptionType.Numberbox;
            public required int Value { get; set; }
            public int MinValue { get; set; }
            public int MaxValue { get; set; }

            public override void SetValue(PluginAdditionalOption option)
            {
                Value = (int)option.NumberValue;
            }

            protected override void AddTypeSpecificOptionProperties(PluginAdditionalOption option)
            {
                option.NumberValue = Value;
                option.NumberBoxMin = MinValue;
                option.NumberBoxMax = MaxValue;
            }
        }

        public class TextSetting : Setting
        {
            public override PluginAdditionalOption.AdditionalOptionType OptionType { get; } = PluginAdditionalOption.AdditionalOptionType.Textbox;
            public string? Value { get; set; }
            public string? PlaceHolder { get; set; }
            public int? MaxLength { get; set; }

            public override void SetValue(PluginAdditionalOption option)
            {
                Value = option.TextValue;
            }

            protected override void AddTypeSpecificOptionProperties(PluginAdditionalOption option)
            {
                option.TextValue = Value;
                option.PlaceholderText = PlaceHolder;
                option.TextBoxMaxLength = MaxLength;
            }
        }

        public class MultilineTextSetting : TextSetting
        {
            public override PluginAdditionalOption.AdditionalOptionType OptionType { get; } = PluginAdditionalOption.AdditionalOptionType.MultilineTextbox;
            public List<string>? ValueAsList { get; set; }

            public override void SetValue(PluginAdditionalOption option)
            {
                base.SetValue(option);
                ValueAsList = option.TextValueAsMultilineList;
            }

            protected override void AddTypeSpecificOptionProperties(PluginAdditionalOption option)
            {
                base.AddTypeSpecificOptionProperties(option);
                option.TextValueAsMultilineList = ValueAsList;
            }
        }

        public class DropdownSetting : Setting
        {
            public override PluginAdditionalOption.AdditionalOptionType OptionType { get; } = PluginAdditionalOption.AdditionalOptionType.Combobox;
            public int SelectedOption { get; set; }
            public required string[] SelectableOptions { get; set; }

            public override void SetValue(PluginAdditionalOption option)
            {
                SelectedOption = option.ComboBoxValue;
            }

            protected override void AddTypeSpecificOptionProperties(PluginAdditionalOption option)
            {
                option.ComboBoxValue = SelectedOption;

                List<KeyValuePair<string, string>> items = [];
                for (var i = 0; i < SelectableOptions.Length; i++)
                {
                    items.Add(new(SelectableOptions[i], i.ToString()));
                }

                option.ComboBoxItems = items;
            }
        }
    }
}