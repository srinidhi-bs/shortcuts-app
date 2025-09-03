using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using ShortcutsApp.Models;
using ShortcutsApp.Services;
using System.Collections.ObjectModel;
using Windows.Storage.Pickers;
using Windows.Storage;

namespace ShortcutsApp.Views
{
    public partial class MainPage : Page
    {
        private readonly SettingsService _settingsService;
        private AppSettings? _currentSettings;
        private readonly List<string> _pressedKeys = new();
        
        public ObservableCollection<ShortcutItem> Shortcuts { get; } = new();

        public MainPage()
        {
            this.InitializeComponent();
            _settingsService = new SettingsService();
            LoadSettings();
        }

        private async void LoadSettings()
        {
            try
            {
                _currentSettings = await _settingsService.LoadSettingsAsync();
                
                // Load hotkey
                var hotkeyText = string.Join(" + ", _currentSettings.HotKey.Modifiers) + " + " + _currentSettings.HotKey.Key;
                HotkeyTextBox.Text = hotkeyText;
                EnableHotkeyCheckBox.IsChecked = _currentSettings.HotKey.Enabled;
                
                // Load shortcuts
                Shortcuts.Clear();
                foreach (var shortcut in _currentSettings.Shortcuts.OrderBy(s => s.Order))
                {
                    Shortcuts.Add(shortcut);
                }
                
                // Load appearance
                GridColumnsSlider.Value = _currentSettings.Appearance.GridColumns;
                IconSizeSlider.Value = _currentSettings.Appearance.IconSize;
                PopupOpacitySlider.Value = _currentSettings.Appearance.PopupOpacity;
                
                UpdateShortcutsVisibility();
            }
            catch (Exception ex)
            {
                // Handle error - could show error dialog
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        private async void SaveSettings()
        {
            try
            {
                await _settingsService.SaveSettingsAsync(_currentSettings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private void HotkeyTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
            
            var key = e.Key.ToString();
            var modifiers = new List<string>();
            
            // For now, we'll use a simplified approach for modifier keys
            // The user will need to manually type the combination
            if (key.Length == 1 && char.IsLetter(key[0]))
            {
                modifiers.Add("Control"); // Default to Ctrl for letters
            }
            
            // Skip if only modifier keys are pressed
            if (key == "Control" || key == "Shift" || key == "Menu" || key == "LeftWindows" || key == "RightWindows")
                return;
                
            if (modifiers.Any())
            {
                _currentSettings.HotKey.Modifiers = modifiers;
                _currentSettings.HotKey.Key = key;
                
                var hotkeyText = string.Join(" + ", modifiers) + " + " + key;
                HotkeyTextBox.Text = hotkeyText;
                
                SaveSettings();
            }
        }

        private void ClearHotkey_Click(object sender, RoutedEventArgs e)
        {
            HotkeyTextBox.Text = "";
            _currentSettings.HotKey.Modifiers.Clear();
            _currentSettings.HotKey.Key = "";
            SaveSettings();
        }

        private void EnableHotkey_Changed(object sender, RoutedEventArgs e)
        {
            if (_currentSettings != null)
            {
                _currentSettings.HotKey.Enabled = EnableHotkeyCheckBox.IsChecked ?? false;
                SaveSettings();
            }
        }

        private async void AddShortcut_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };
            
            picker.FileTypeFilter.Add(".exe");
            picker.FileTypeFilter.Add(".lnk");
            picker.FileTypeFilter.Add("*");
            
            var window = ((App)App.Current).GetService<MainWindow>();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            
            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var shortcut = new ShortcutItem
                {
                    Name = Path.GetFileNameWithoutExtension(file.Path),
                    Path = file.Path,
                    Order = Shortcuts.Count
                };
                
                await _settingsService.AddShortcutAsync(shortcut);
                Shortcuts.Add(shortcut);
                UpdateShortcutsVisibility();
            }
        }

        private async void RemoveShortcut_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string id)
            {
                await _settingsService.RemoveShortcutAsync(id);
                var shortcut = Shortcuts.FirstOrDefault(s => s.Id == id);
                if (shortcut != null)
                {
                    Shortcuts.Remove(shortcut);
                }
                UpdateShortcutsVisibility();
            }
        }

        private void UpdateShortcutsVisibility()
        {
            EmptyShortcutsText.Visibility = Shortcuts.Any() ? Visibility.Collapsed : Visibility.Visible;
        }

        private void GridColumnsSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_currentSettings != null)
            {
                _currentSettings.Appearance.GridColumns = (int)e.NewValue;
                GridColumnsText.Text = $"{(int)e.NewValue} columns";
                SaveSettings();
            }
        }

        private void IconSizeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_currentSettings != null)
            {
                _currentSettings.Appearance.IconSize = (int)e.NewValue;
                IconSizeText.Text = $"{(int)e.NewValue} pixels";
                SaveSettings();
            }
        }

        private void PopupOpacitySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_currentSettings != null)
            {
                _currentSettings.Appearance.PopupOpacity = e.NewValue;
                PopupOpacityText.Text = $"{(int)(e.NewValue * 100)}%";
                SaveSettings();
            }
        }

        /// <summary>
        /// Handles the "Minimize to Tray" button click event.
        /// This method hides the main window and keeps the application running in the system tray.
        /// 
        /// Learning Note: Minimizing to tray is a common pattern in Windows applications.
        /// Instead of closing the application, it hides the main window and continues running
        /// with only the system tray icon visible. Users can restore the window by interacting
        /// with the system tray icon.
        /// </summary>
        /// <param name="sender">The button that was clicked</param>
        /// <param name="e">Event arguments containing click details</param>
        private void MinimizeToTray_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the main window instance from the application's service container
                // This uses dependency injection to access the window reference
                var window = ((App)App.Current).GetService<MainWindow>();
                
                if (window != null)
                {
                    // Hide the main window instead of minimizing it
                    // This removes it from the taskbar and Alt+Tab menu
                    // The window can be restored through the system tray icon
                    window.AppWindow.Hide();
                    
                    // Optional: You could also use window.Minimize() if you want it to appear in taskbar
                    // but for true "minimize to tray" behavior, hiding is more appropriate
                    
                    // Learning Note: AppWindow.Hide() completely removes the window from view
                    // while keeping the application process running. This is different from
                    // window.Close() which would terminate the application.
                }
                else
                {
                    // Handle the case where window reference is not available
                    // This shouldn't normally happen, but defensive programming is good practice
                    System.Diagnostics.Debug.WriteLine("Warning: Could not access main window for minimize to tray operation");
                }
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during the minimize operation
                // This prevents the application from crashing if something goes wrong
                System.Diagnostics.Debug.WriteLine($"Error during minimize to tray: {ex.Message}");
                
                // In a production application, you might want to:
                // 1. Log the error to a file
                // 2. Show a user-friendly error message
                // 3. Attempt fallback behavior (like regular minimize)
            }
        }

        private void TestPopup_Click(object sender, RoutedEventArgs e)
        {
            // This will be implemented when we create the popup window
            System.Diagnostics.Debug.WriteLine("Test popup clicked - not implemented yet");
        }
    }
}
