using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using ShortcutsApp.Models;
using ShortcutsApp.Services;
using System.Collections.ObjectModel;
using Windows.Storage.Pickers;
using Windows.Storage;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.UI.Text;

namespace ShortcutsApp.Views
{
    public partial class MainPage : Page
    {
        private readonly SettingsService _settingsService;
        private AppSettings? _currentSettings;
        private bool _isRecordingHotkey = false;
        private readonly HashSet<string> _pressedKeys = new();
        private readonly List<string> _conflictingHotkeys = new()
        {
            // System reserved hotkeys that should trigger conflict warnings
            "Control + C", "Control + V", "Control + X", "Control + Z", "Control + Y",
            "Control + A", "Control + S", "Control + O", "Control + N", "Control + P",
            "Alt + Tab", "Alt + F4", "Windows + L", "Windows + D", "Windows + R",
            "Control + Alt + Delete", "Control + Shift + Escape"
        };
        
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

        /// <summary>
        /// Handles the "Record" button click to start hotkey capture mode.
        /// This method enables visual feedback and prepares the UI for hotkey input.
        /// </summary>
        private void RecordHotkey_Click(object sender, RoutedEventArgs e)
        {
            if (_isRecordingHotkey)
            {
                StopHotkeyRecording();
                return;
            }

            StartHotkeyRecording();
        }

        /// <summary>
        /// Starts the hotkey recording mode with visual feedback and keyboard focus.
        /// </summary>
        private void StartHotkeyRecording()
        {
            _isRecordingHotkey = true;
            _pressedKeys.Clear();
            
            // Update UI to show recording state
            RecordHotkeyButton.Content = "Stop";
            HotkeyRecordingOverlay.Visibility = Visibility.Visible;
            HotkeyStatusText.Text = "Recording... Press your desired key combination";
            HotkeyConflictInfo.IsOpen = false;
            
            // Set focus to enable key capture
            HotkeyTextBox.Focus(FocusState.Keyboard);
            
            // Subscribe to global key events for better capture
            this.KeyDown += MainPage_KeyDown;
            this.KeyUp += MainPage_KeyUp;
        }

        /// <summary>
        /// Stops hotkey recording and restores normal UI state.
        /// </summary>
        private void StopHotkeyRecording()
        {
            _isRecordingHotkey = false;
            
            // Update UI to normal state
            RecordHotkeyButton.Content = "Record";
            HotkeyRecordingOverlay.Visibility = Visibility.Collapsed;
            HotkeyStatusText.Text = "Configure a global hotkey to quickly access your shortcuts";
            
            // Unsubscribe from key events
            this.KeyDown -= MainPage_KeyDown;
            this.KeyUp -= MainPage_KeyUp;
        }

        /// <summary>
        /// Handles key down events during hotkey recording.
        /// Captures modifier keys and builds the hotkey combination.
        /// </summary>
        private void MainPage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!_isRecordingHotkey) return;
            
            e.Handled = true;
            
            // Handle Escape key to cancel recording
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                StopHotkeyRecording();
                return;
            }
            
            var keyName = GetFriendlyKeyName(e.Key);
            
            // Add to pressed keys if not already present
            if (!string.IsNullOrEmpty(keyName))
            {
                _pressedKeys.Add(keyName);
                UpdateHotkeyDisplay();
            }
        }

        /// <summary>
        /// Handles key up events during hotkey recording.
        /// Completes the hotkey capture when all keys are released.
        /// </summary>
        private void MainPage_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (!_isRecordingHotkey || _pressedKeys.Count == 0) return;
            
            // Small delay to allow for multiple key combinations
            var timer = new Microsoft.UI.Xaml.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                if (_isRecordingHotkey && _pressedKeys.Count > 0)
                {
                    CompleteHotkeyCapture();
                }
            };
            timer.Start();
        }

        /// <summary>
        /// Completes the hotkey capture process and validates the combination.
        /// </summary>
        private void CompleteHotkeyCapture()
        {
            var keys = _pressedKeys.ToList();
            
            // Separate modifiers from main key
            var modifiers = keys.Where(k => IsModifierKey(k)).ToList();
            var mainKey = keys.FirstOrDefault(k => !IsModifierKey(k));
            
            if (!string.IsNullOrEmpty(mainKey) && modifiers.Any())
            {
                // Update settings
                _currentSettings.HotKey.Modifiers = modifiers;
                _currentSettings.HotKey.Key = mainKey;
                
                // Check for conflicts
                var hotkeyText = string.Join(" + ", modifiers.OrderBy(m => m)) + " + " + mainKey;
                CheckHotkeyConflicts(hotkeyText);
                
                // Update UI
                HotkeyTextBox.Text = hotkeyText;
                HotkeyStatusText.Text = "Hotkey captured successfully!";
                
                SaveSettings();
            }
            else
            {
                HotkeyStatusText.Text = "Invalid combination. Please include at least one modifier key (Ctrl, Alt, Shift, or Windows).";
            }
            
            StopHotkeyRecording();
        }

        /// <summary>
        /// Updates the hotkey display during recording to show current key combination.
        /// </summary>
        private void UpdateHotkeyDisplay()
        {
            if (_pressedKeys.Any())
            {
                var sortedKeys = _pressedKeys.OrderBy(k => IsModifierKey(k) ? 0 : 1).ThenBy(k => k);
                HotkeyTextBox.Text = string.Join(" + ", sortedKeys);
            }
        }

        /// <summary>
        /// Converts system virtual key codes to user-friendly key names.
        /// </summary>
        private string GetFriendlyKeyName(Windows.System.VirtualKey key)
        {
            return key switch
            {
                // Modifier keys
                Windows.System.VirtualKey.Control or Windows.System.VirtualKey.LeftControl or Windows.System.VirtualKey.RightControl => "Control",
                Windows.System.VirtualKey.Shift or Windows.System.VirtualKey.LeftShift or Windows.System.VirtualKey.RightShift => "Shift",
                Windows.System.VirtualKey.Menu or Windows.System.VirtualKey.LeftMenu or Windows.System.VirtualKey.RightMenu => "Alt",
                Windows.System.VirtualKey.LeftWindows or Windows.System.VirtualKey.RightWindows => "Windows",
                
                // Function keys
                Windows.System.VirtualKey.F1 => "F1",
                Windows.System.VirtualKey.F2 => "F2",
                Windows.System.VirtualKey.F3 => "F3",
                Windows.System.VirtualKey.F4 => "F4",
                Windows.System.VirtualKey.F5 => "F5",
                Windows.System.VirtualKey.F6 => "F6",
                Windows.System.VirtualKey.F7 => "F7",
                Windows.System.VirtualKey.F8 => "F8",
                Windows.System.VirtualKey.F9 => "F9",
                Windows.System.VirtualKey.F10 => "F10",
                Windows.System.VirtualKey.F11 => "F11",
                Windows.System.VirtualKey.F12 => "F12",
                
                // Special keys
                Windows.System.VirtualKey.Space => "Space",
                Windows.System.VirtualKey.Enter => "Enter",
                Windows.System.VirtualKey.Tab => "Tab",
                Windows.System.VirtualKey.Back => "Backspace",
                Windows.System.VirtualKey.Delete => "Delete",
                Windows.System.VirtualKey.Insert => "Insert",
                Windows.System.VirtualKey.Home => "Home",
                Windows.System.VirtualKey.End => "End",
                Windows.System.VirtualKey.PageUp => "PageUp",
                Windows.System.VirtualKey.PageDown => "PageDown",
                
                // Arrow keys
                Windows.System.VirtualKey.Up => "Up",
                Windows.System.VirtualKey.Down => "Down",
                Windows.System.VirtualKey.Left => "Left",
                Windows.System.VirtualKey.Right => "Right",
                
                // Numbers and letters (convert to string)
                >= Windows.System.VirtualKey.A and <= Windows.System.VirtualKey.Z => key.ToString(),
                >= Windows.System.VirtualKey.Number0 and <= Windows.System.VirtualKey.Number9 => key.ToString().Replace("Number", ""),
                
                // Default case
                _ => key.ToString()
            };
        }

        /// <summary>
        /// Determines if a key is a modifier key (Ctrl, Alt, Shift, Windows).
        /// </summary>
        private bool IsModifierKey(string key)
        {
            return key is "Control" or "Alt" or "Shift" or "Windows";
        }

        /// <summary>
        /// Checks if the captured hotkey conflicts with common system shortcuts.
        /// </summary>
        private void CheckHotkeyConflicts(string hotkeyText)
        {
            if (_conflictingHotkeys.Contains(hotkeyText))
            {
                HotkeyConflictInfo.Message = $"The combination '{hotkeyText}' may conflict with system shortcuts. Consider using a different combination.";
                HotkeyConflictInfo.IsOpen = true;
            }
            else
            {
                HotkeyConflictInfo.IsOpen = false;
            }
        }

        /// <summary>
        /// Handles the "Clear" button click to remove the current hotkey configuration.
        /// </summary>
        private void ClearHotkey_Click(object sender, RoutedEventArgs e)
        {
            // Stop any active recording
            if (_isRecordingHotkey)
            {
                StopHotkeyRecording();
            }
            
            // Clear the hotkey configuration
            HotkeyTextBox.Text = "";
            _currentSettings.HotKey.Modifiers.Clear();
            _currentSettings.HotKey.Key = "";
            
            // Reset UI state
            HotkeyStatusText.Text = "Hotkey cleared. Click 'Record' to set a new hotkey.";
            HotkeyConflictInfo.IsOpen = false;
            
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

        /// <summary>
        /// Handles the edit shortcut button click to allow renaming shortcuts.
        /// Opens a dialog where users can modify the shortcut's display name.
        /// </summary>
        private async void EditShortcut_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string id)
            {
                var shortcut = Shortcuts.FirstOrDefault(s => s.Id == id);
                if (shortcut != null)
                {
                    await ShowEditShortcutDialog(shortcut);
                }
            }
        }

        /// <summary>
        /// Shows the edit shortcut dialog for renaming shortcuts.
        /// Provides a user-friendly interface for modifying shortcut properties.
        /// </summary>
        private async Task ShowEditShortcutDialog(ShortcutItem shortcut)
        {
            var stackPanel = new StackPanel { Spacing = 16 };
            
            // Name editing section
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = "Shortcut Name:", 
                FontWeight = FontWeights.Medium 
            });
            
            var nameTextBox = new TextBox 
            { 
                Text = shortcut.Name,
                PlaceholderText = "Enter shortcut name"
            };
            stackPanel.Children.Add(nameTextBox);
            
            // Path display section (read-only information)
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = "File Path:", 
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 8, 0, 0)
            });
            
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = shortcut.Path,
                Foreground = new SolidColorBrush(Colors.Gray),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12
            });

            var dialog = new ContentDialog
            {
                Title = "Edit Shortcut",
                Content = stackPanel,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var newName = nameTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(newName) && newName != shortcut.Name)
                {
                    // Update the shortcut name
                    shortcut.Name = newName;
                    await _settingsService.UpdateShortcutAsync(shortcut);
                    
                    // Refresh the UI by updating the observable collection
                    var index = Shortcuts.IndexOf(shortcut);
                    if (index >= 0)
                    {
                        // Trigger property change notification
                        Shortcuts.RemoveAt(index);
                        Shortcuts.Insert(index, shortcut);
                    }
                }
            }
        }

        /// <summary>
        /// Handles removing a shortcut with confirmation dialog.
        /// Provides a safety check before permanently removing shortcuts.
        /// </summary>
        private async void RemoveShortcut_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string id)
            {
                var shortcut = Shortcuts.FirstOrDefault(s => s.Id == id);
                if (shortcut != null)
                {
                    // Show confirmation dialog for better user experience
                    var dialog = new ContentDialog
                    {
                        Title = "Remove Shortcut",
                        Content = $"Are you sure you want to remove '{shortcut.Name}' from your shortcuts?",
                        PrimaryButtonText = "Remove",
                        CloseButtonText = "Cancel",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = this.XamlRoot
                    };

                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        await _settingsService.RemoveShortcutAsync(id);
                        Shortcuts.Remove(shortcut);
                        UpdateShortcutsVisibility();
                        
                        // Reorder remaining shortcuts to maintain consecutive order
                        await ReorderShortcuts();
                    }
                }
            }
        }

        /// <summary>
        /// Reorders all shortcuts to maintain consecutive order values after removals.
        /// Ensures that the shortcut ordering remains consistent in the settings.
        /// </summary>
        private async Task ReorderShortcuts()
        {
            for (int i = 0; i < Shortcuts.Count; i++)
            {
                if (Shortcuts[i].Order != i)
                {
                    Shortcuts[i].Order = i;
                    await _settingsService.UpdateShortcutAsync(Shortcuts[i]);
                }
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
