using System.Text.Json;
using ShortcutsApp.Models;

namespace ShortcutsApp.Services
{
    public class SettingsService
    {
        private readonly string _settingsPath;
        private AppSettings? _cachedSettings;
        private readonly JsonSerializerOptions _jsonOptions;

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "ShortcutsApp");
            Directory.CreateDirectory(appFolder);
            _settingsPath = Path.Combine(appFolder, "settings.json");

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<AppSettings> LoadSettingsAsync()
        {
            if (_cachedSettings != null)
                return _cachedSettings;

            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = await File.ReadAllTextAsync(_settingsPath);
                    _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
                }
                else
                {
                    _cachedSettings = new AppSettings();
                    await SaveSettingsAsync(_cachedSettings);
                }
            }
            catch (Exception)
            {
                _cachedSettings = new AppSettings();
            }

            return _cachedSettings;
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, _jsonOptions);
                await File.WriteAllTextAsync(_settingsPath, json);
                _cachedSettings = settings;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save settings: {ex.Message}", ex);
            }
        }

        public AppSettings GetSettings()
        {
            return _cachedSettings ?? LoadSettingsAsync().GetAwaiter().GetResult();
        }

        public async Task<bool> AddShortcutAsync(ShortcutItem shortcut)
        {
            var settings = await LoadSettingsAsync();
            shortcut.Order = settings.Shortcuts.Count;
            settings.Shortcuts.Add(shortcut);
            await SaveSettingsAsync(settings);
            return true;
        }

        public async Task<bool> RemoveShortcutAsync(string id)
        {
            var settings = await LoadSettingsAsync();
            var shortcut = settings.Shortcuts.FirstOrDefault(s => s.Id == id);
            if (shortcut != null)
            {
                settings.Shortcuts.Remove(shortcut);
                await SaveSettingsAsync(settings);
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateShortcutAsync(ShortcutItem shortcut)
        {
            var settings = await LoadSettingsAsync();
            var index = settings.Shortcuts.FindIndex(s => s.Id == shortcut.Id);
            if (index >= 0)
            {
                settings.Shortcuts[index] = shortcut;
                await SaveSettingsAsync(settings);
                return true;
            }
            return false;
        }
    }
}