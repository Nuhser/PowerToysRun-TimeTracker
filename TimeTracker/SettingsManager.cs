using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerToys.Settings.UI.Library;

namespace Community.Powertoys.Run.Plugin.TimeTracker
{
    public class SettingsManager
    {
        public static readonly string LIGHT_ICON_PATH = "images/light/";
        public static readonly string DARK_ICON_PATH = "images/dark/";
        public string IconPath { get; set; } = LIGHT_ICON_PATH;

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

        public List<Setting> GetSettings()
        {
            return [];
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
    }

    public class SettingsManager
    {

    }
}