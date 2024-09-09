using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using Wox.Plugin;
using Wox.Plugin.Logger;

namespace Community.Powertoys.Run.Plugin.TimeTracker
{
    public class TimeTracker : IPlugin, ISettingProvider, IContextMenu
    {
        private PluginInitContext? _context;
        private SettingsManager _settingsManager;
        private QueryManager _queryManager;

        // plugin properties
        public static string PluginID => "EF1B799F615144E3B0E1AA15878B2077";
        public string Name => "Time Tracker";
        public string Description => "Tracks time spend on different tasks";
        public IEnumerable<PluginAdditionalOption> AdditionalOptions => _settingsManager.GetOptions();

        public TimeTracker()
        {
            _settingsManager = new SettingsManager();
            _queryManager = new QueryManager(_settingsManager);

            Log.Info(
                "Time Tracker started. Plugin installed in " + (Directory.Exists(@".\RunPlugins\TimeTracker") ? "main plugin directory." : "community plugin directory."),
                GetType()
            );
        }

        public Control CreateSettingPanel()
        {
            throw new NotImplementedException();
        }

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            _settingsManager.UpdateSettings(settings);
        }

        public void Init(PluginInitContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _context = context;
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());
        }

        public List<Result> Query(Query query)
        {
            var queryString = query.Search;
            return _queryManager.CheckQueryAndReturnResults(queryString);
        }

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            return selectedResult?.ContextData is List<ContextMenuResult> contextData
                ? contextData
                : [];
        }

        private void OnThemeChanged(Theme currentTheme, Theme newTheme)
        {
            UpdateIconPath(newTheme);
        }

        private void UpdateIconPath(Theme theme)
        {
            if (theme is Theme.Light or Theme.HighContrastWhite)
            {
                _settingsManager.IconPath = SettingsManager.LIGHT_ICON_PATH;
                _settingsManager.HtmlExportTheme = "light";
            }
            else
            {
                _settingsManager.IconPath = SettingsManager.DARK_ICON_PATH;
                _settingsManager.HtmlExportTheme = "dark";
            }
        }
    }
}