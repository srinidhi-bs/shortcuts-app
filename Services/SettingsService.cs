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

        /// <summary>
        /// Records usage of a shortcut by updating usage count and timestamps
        /// This enables recently used and frequently used features
        /// </summary>
        /// <param name="shortcutId">ID of the shortcut that was used</param>
        /// <returns>True if the usage was successfully recorded</returns>
        public async Task<bool> TrackShortcutUsageAsync(string shortcutId)
        {
            try
            {
                var settings = await LoadSettingsAsync();
                var shortcut = settings.Shortcuts.FirstOrDefault(s => s.Id == shortcutId);
                
                if (shortcut != null)
                {
                    // Update usage statistics
                    shortcut.UsageCount++;
                    shortcut.LastUsedAt = DateTime.Now;
                    
                    // Set first used time if this is the first use
                    if (shortcut.FirstUsedAt == null)
                    {
                        shortcut.FirstUsedAt = DateTime.Now;
                    }
                    
                    await SaveSettingsAsync(settings);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to track shortcut usage: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets shortcuts ordered by recent usage (most recent first)
        /// Useful for showing recently used shortcuts at the top of lists
        /// </summary>
        /// <param name="maxCount">Maximum number of shortcuts to return (default: 10)</param>
        /// <returns>List of recently used shortcuts</returns>
        public async Task<List<ShortcutItem>> GetRecentlyUsedShortcutsAsync(int maxCount = 10)
        {
            try
            {
                var settings = await LoadSettingsAsync();
                
                return settings.Shortcuts
                    .Where(s => s.LastUsedAt != null) // Only include shortcuts that have been used
                    .OrderByDescending(s => s.LastUsedAt) // Most recent first
                    .Take(maxCount)
                    .ToList();
            }
            catch (Exception)
            {
                // Return empty list on error instead of throwing
                return new List<ShortcutItem>();
            }
        }

        /// <summary>
        /// Gets shortcuts ordered by usage frequency (most used first)
        /// Useful for showing frequently used shortcuts
        /// </summary>
        /// <param name="maxCount">Maximum number of shortcuts to return (default: 10)</param>
        /// <returns>List of frequently used shortcuts</returns>
        public async Task<List<ShortcutItem>> GetMostUsedShortcutsAsync(int maxCount = 10)
        {
            try
            {
                var settings = await LoadSettingsAsync();
                
                return settings.Shortcuts
                    .Where(s => s.UsageCount > 0) // Only include shortcuts that have been used
                    .OrderByDescending(s => s.UsageCount) // Most used first
                    .ThenByDescending(s => s.LastUsedAt) // Recent as tiebreaker
                    .Take(maxCount)
                    .ToList();
            }
            catch (Exception)
            {
                // Return empty list on error instead of throwing
                return new List<ShortcutItem>();
            }
        }
    }
}