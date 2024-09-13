using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Wox.Plugin;

namespace Community.Powertoys.Run.Plugin.TimeTracker
{
    public class TimeTracker : IPlugin, ISettingProvider, IContextMenu
    {
        private PluginInitContext? _context;
        private readonly SettingsManager _settingsManager;
        private readonly QueryService _queryService;
        private readonly ExportService _exportService;

        // plugin properties
        public static string PluginID => "EF1B799F615144E3B0E1AA15878B2077";
        public string Name => "Time Tracker";
        public string Description => "Tracks time spend on different tasks";
        public IEnumerable<PluginAdditionalOption> AdditionalOptions => _settingsManager.GetOptions();

        public TimeTracker()
        {
            _settingsManager = new SettingsManager();
            _exportService = new ExportService(_settingsManager);
            _queryService = new QueryService(_settingsManager, _exportService);
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
            _settingsManager.InitFromContext(_context);
            _context.API.ThemeChanged += OnThemeChanged;
        }

        public List<Result> Query(Query query)
        {
            var queryString = query.Search;
            return _queryService.CheckQueryAndReturnResults(queryString);
        }

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            return selectedResult?.ContextData is List<ContextMenuResult> contextData
                ? contextData
                : [];
        }

        private void OnThemeChanged(Theme currentTheme, Theme newTheme)
        {
            _settingsManager.SetPluginTheme(newTheme);
        }
    }
}