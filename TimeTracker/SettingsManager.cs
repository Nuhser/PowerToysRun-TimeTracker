using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.PowerToys.Settings.UI.Library;

namespace Community.Powertoys.Run.Plugin.TimeTracker
{
    public class SettingsManager
    {
        public static readonly string PLUGIN_PATH =
            Directory.Exists(@".\RunPlugins\TimeTracker")
            ? @"RunPlugins\TimeTracker"
            : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\PowerToys\PowerToys Run\Plugins\TimeTracker";

        public static readonly string SAVES_NAME = "data.json";

        public static readonly string LIGHT_ICON_PATH = "icons/light/";
        public static readonly string DARK_ICON_PATH = "icons/dark/";
        public string IconPath { get; set; } = LIGHT_ICON_PATH;

        public string HtmlExportTheme { get; set; } = "light";

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

        public TextSetting DataPathSetting = new()
        {
            Key = "data_path",
            Label = "Path to Save Location",
            Description = "Folder in which the tracker's data should be saved.",
            Validator = new DirectoryPathValidator(true)
        };

        public TextSetting ExportPathSetting = new()
        {
            Key = "export_path",
            Label = "Path to Export Location",
            Description = "Folder in which the tracker's summary files should be saved.",
            Validator = new DirectoryPathValidator(true)
        };

        public List<Setting> GetSettings()
        {
            return [
                SummaryExportTypeSetting,
                ShowNotificationsSetting,
                ShowSavesFileSetting,
                DataPathSetting,
                ExportPathSetting
            ];
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
                    .ForEach(setting =>
                        {
                            if (setting.Validator == null || setting.Validator.ValidateSetting(setting, option))
                                setting.SetValue(option);
                        }
                    );
            }
        }

        /* HELPER CLASSES */
        public abstract class Setting
        {
            public required string Key { get; set; }
            public required string Label { get; set; }
            public string? Description { get; set; }
            public SettingValidator? Validator { get; set; }
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

            public override void SetValue(PluginAdditionalOption option)
            {
                Value = (int)option.NumberValue;
            }

            protected override void AddTypeSpecificOptionProperties(PluginAdditionalOption option)
            {
                option.NumberValue = Value;
                option.NumberBoxMin = MinValue;
            }
        }

        public class TextSetting : Setting
        {
            public override PluginAdditionalOption.AdditionalOptionType OptionType { get; } = PluginAdditionalOption.AdditionalOptionType.Textbox;
            public string? Value { get; set; }
            public string? PlaceHolder { get; set; }

            public override void SetValue(PluginAdditionalOption option)
            {
                Value = option.TextValue;
            }

            protected override void AddTypeSpecificOptionProperties(PluginAdditionalOption option)
            {
                option.TextValue = Value;
                option.PlaceholderText = PlaceHolder;
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

        public abstract class SettingValidator
        {
            public bool ValidateSetting(Setting setting, PluginAdditionalOption option)
            {
                if (ValidationMethod(setting, option))
                {
                    if (!IsValueAlreadyChecked(option))
                        ValidationSuccessMethod(setting, option);

                    return true;
                }
                else
                {
                    if (!IsValueAlreadyChecked(option))
                        ValidationFailureMethod(setting, option);

                    return false;
                }
            }

            protected abstract bool IsValueAlreadyChecked(PluginAdditionalOption option);

            protected abstract bool ValidationMethod(Setting setting, PluginAdditionalOption option);

            protected virtual void ValidationSuccessMethod(Setting setting, PluginAdditionalOption option)
            {
                return;
            }

            protected virtual void ValidationFailureMethod(Setting setting, PluginAdditionalOption option)
            {
                return;
            }
        }

        public class DirectoryPathValidator(bool acceptNull) : SettingValidator
        {
            private string? _lastCheckedValue = null;

            protected override bool IsValueAlreadyChecked(PluginAdditionalOption option)
            {
                bool alreadyChecked = option.TextValue == _lastCheckedValue;

                _lastCheckedValue = option.TextValue;

                return alreadyChecked;
            }

            protected override bool ValidationMethod(Setting setting, PluginAdditionalOption option)
            {
                return (acceptNull && string.IsNullOrEmpty(option.TextValue)) || Directory.Exists(option.TextValue);
            }

            protected override void ValidationFailureMethod(Setting setting, PluginAdditionalOption option)
            {
                MessageBox.Show(
                    "The path you set for setting '" + setting.Label + "' is not valid.",
                    "Invalid Folder Path",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}