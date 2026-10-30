using Qrid.Common.Client.Api;
using Qrs.App.Analytics.Client.Models;

namespace Qrs.App.Analytics.Client
{
    public class AnalyticsService : CommonContext
    {
        // this class is like entire Application but without UI
        // we will have 2 UIs using this class:
        // - WinForms app
        // - Console app (cross-platform)
        
        public List<AnalyticsContext> Contexts { get; set; }

        public AnalyticsService()
        {
            Contexts = new List<AnalyticsContext>();
        }

        public async Task InitSettingsAsync()
        {
            // Settings

            if (Settings == null)
            {
                // Try Loading Settings
                await LoadSettings();
            }

            if (Settings == null)
            {
                // create and save Default Settings 
                await LoadDefaultSettings();
                await SaveSettings();
            }

        }

        #region Settings

        public static string ENVIRONMENT_NAME = "Dev";
        public Settings Settings { get; set; }
        public string AppRootPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QrsInternalAnalytics", ENVIRONMENT_NAME);
        public string SettingsFilePath => Path.Combine(AppRootPath, "Settings.json");
        public string DefaultDataPath => AppRootPath;
        public string CookieFolderPath => AppRootPath;

        public void EnsureAppRootPath()
        {
            if (Directory.Exists(AppRootPath)) return;
            
        }
        public async Task SaveSettings()
        {
            // 1) creates all dir and subdir in the path, unless already exists
            Directory.CreateDirectory(AppRootPath);

            // 2) save the settings file
            var json = System.Text.Json.JsonSerializer.Serialize(Settings);
            await File.WriteAllTextAsync(SettingsFilePath, json);
        }

        public async Task LoadSettings()
        {
            if (!File.Exists(SettingsFilePath))
            {
                Settings = null;
                return;
            }

            var json = await File.ReadAllTextAsync(SettingsFilePath);
            Settings = System.Text.Json.JsonSerializer.Deserialize<Settings>(json);
        }

        public async Task LoadDefaultSettings()
        {
            Settings = new Settings();
            Settings.TimerPeriod = 60;
            var contextSettings = new ContextSettings()
            {
                Name = "qrid.com",
                BaseUri = "https://qrid.qrid.com/qrs-tunnel-api/", // TODO - this is bad; need to perform Discovery of TunnelService base URI using Name as Domain
                SQLiteFile = Path.Combine(DefaultDataPath, "qrid.com.sqlite"),
                IsActive = true
            };
            Settings.ContextSettings = new List<ContextSettings> { contextSettings };
        }

        #endregion


        public async Task UpdateAllContexts()
        {
            foreach (var context in Contexts) 
            {
                await context.UpdateAndLog();
            }
        }


    }
}
