using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using ShortcutsApp.Models;
using ShortcutsApp.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WinUIEx;

namespace ShortcutsApp.Views
{
    /// <summary>
    /// Popup window that displays shortcuts in a grid layout with keyboard navigation
    /// This window appears when the global hotkey is pressed and allows users to 
    /// navigate and launch their configured shortcuts
    /// </summary>
    public sealed partial class PopupWindow : WindowEx
    {
        #region Private Fields
        
        private readonly SettingsService _settingsService;
        private AppSettings _settings;
        private List<ShortcutItem> _shortcuts;
        private int _selectedIndex = 0;
        
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the PopupWindow
        /// </summary>
        /// <param name="settingsService">Service for loading settings and shortcuts</param>
        public PopupWindow(SettingsService settingsService)
        {
            this.InitializeComponent();
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            
            // Configure window properties for popup behavior
            ConfigureWindowProperties();
            
            // Load settings and shortcuts
            LoadSettings();
            LoadShortcuts();
            
            // Set up keyboard handling
            this.KeyDown += PopupWindow_KeyDown;
            
            // Set initial focus to the grid
            this.Loaded += PopupWindow_Loaded;
        }

        #endregion

        #region Window Configuration

        /// <summary>
        /// Configures window properties for popup behavior:
        /// - Always on top
        /// - No taskbar appearance
        /// - Borderless style
        /// - No resize capability
        /// </summary>
        private void ConfigureWindowProperties()
        {
            // Set window to always stay on top
            this.IsAlwaysOnTop = true;
            
            // Hide from taskbar
            this.IsShownInSwitchers = false;
            
            // Configure window styling
            this.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
            this.IsResizable = false;
            this.IsMaximizable = false;
            this.IsMinimizable = false;
            
            // Center the window on screen
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        #endregion

        #region Settings and Data Loading

        /// <summary>
        /// Loads application settings and applies them to the popup window
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                _settings = _settingsService.LoadSettings();
                ApplySettings();
            }
            catch (Exception ex)
            {
                // Log error and use default settings
                Debug.WriteLine($"Error loading settings: {ex.Message}");
                _settings = new AppSettings();
            }
        }

        /// <summary>
        /// Applies loaded settings to the popup window UI
        /// </summary>
        private void ApplySettings()
        {
            // Apply popup opacity
            PopupBackground.Opacity = _settings.PopupOpacity;
            
            // Apply grid columns
            if (ShortcutsUniformGrid != null)
            {
                ShortcutsUniformGrid.Columns = _settings.GridColumns;
            }
            
            // Apply icon size to all shortcut icons (will be handled in data template)
            // Icon size will be applied when shortcuts are loaded
        }

        /// <summary>
        /// Loads shortcuts from settings and populates the grid
        /// </summary>
        private void LoadShortcuts()
        {
            try
            {
                _shortcuts = _settings?.Shortcuts?.ToList() ?? new List<ShortcutItem>();
                ShortcutsGridView.ItemsSource = _shortcuts;
                
                // Select the first item if available
                if (_shortcuts.Count > 0)
                {
                    _selectedIndex = 0;
                    ShortcutsGridView.SelectedIndex = _selectedIndex;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading shortcuts: {ex.Message}");
                _shortcuts = new List<ShortcutItem>();
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the window loaded event to set initial focus
        /// </summary>
        private void PopupWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Set focus to the shortcuts grid for immediate keyboard navigation
            ShortcutsGridView.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// Handles global keyboard input for the popup window
        /// </summary>
        private void PopupWindow_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.Escape:
                    // Close popup on Escape
                    HidePopup();
                    e.Handled = true;
                    break;
                    
                case Windows.System.VirtualKey.Enter:
                    // Launch selected shortcut on Enter
                    LaunchSelectedShortcut();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Handles keyboard navigation within the shortcuts grid
        /// </summary>
        private void ShortcutsGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (_shortcuts.Count == 0) return;

            int newIndex = _selectedIndex;
            int columns = _settings.GridColumns;
            int rows = (int)Math.Ceiling((double)_shortcuts.Count / columns);

            switch (e.Key)
            {
                case Windows.System.VirtualKey.Left:
                    newIndex = Math.Max(0, _selectedIndex - 1);
                    break;
                    
                case Windows.System.VirtualKey.Right:
                    newIndex = Math.Min(_shortcuts.Count - 1, _selectedIndex + 1);
                    break;
                    
                case Windows.System.VirtualKey.Up:
                    newIndex = Math.Max(0, _selectedIndex - columns);
                    break;
                    
                case Windows.System.VirtualKey.Down:
                    newIndex = Math.Min(_shortcuts.Count - 1, _selectedIndex + columns);
                    break;
                    
                default:
                    return; // Don't handle other keys
            }

            if (newIndex != _selectedIndex)
            {
                _selectedIndex = newIndex;
                ShortcutsGridView.SelectedIndex = _selectedIndex;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles selection changes in the shortcuts grid
        /// </summary>
        private void ShortcutsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ShortcutsGridView.SelectedIndex >= 0)
            {
                _selectedIndex = ShortcutsGridView.SelectedIndex;
            }
        }

        /// <summary>
        /// Handles shortcut item clicks (mouse/touch interaction)
        /// </summary>
        private void ShortcutItem_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ShortcutItem shortcut)
            {
                LaunchShortcut(shortcut);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the popup window with smooth animation
        /// Centers the window on screen and brings it to focus
        /// </summary>
        public async void ShowPopup()
        {
            try
            {
                // Reload settings and shortcuts in case they changed
                LoadSettings();
                LoadShortcuts();
                
                // Center window on screen
                CenterOnScreen();
                
                // Show window (initially transparent)
                this.Opacity = 0;
                this.Activate();
                
                // Animate fade in
                await AnimateShowAsync();
                
                // Set focus to grid for keyboard navigation
                ShortcutsGridView.Focus(FocusState.Programmatic);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing popup: {ex.Message}");
            }
        }

        /// <summary>
        /// Hides the popup window with smooth animation
        /// </summary>
        public async void HidePopup()
        {
            try
            {
                // Animate fade out then hide
                await AnimateHideAsync();
                this.Hide();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error hiding popup: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Centers the popup window on the primary screen
        /// </summary>
        private void CenterOnScreen()
        {
            // Get screen dimensions
            var displayArea = Windows.Graphics.RectInt32.Empty;
            
            try
            {
                // Try to get the current display area
                var displayId = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(this.AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
                displayArea = displayId.WorkArea;
            }
            catch
            {
                // Fallback to default positioning
                return;
            }

            // Calculate center position
            int centerX = displayArea.X + (displayArea.Width - (int)this.Width) / 2;
            int centerY = displayArea.Y + (displayArea.Height - (int)this.Height) / 2;

            // Set window position
            this.AppWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));
        }

        /// <summary>
        /// Launches the currently selected shortcut
        /// </summary>
        private void LaunchSelectedShortcut()
        {
            if (_selectedIndex >= 0 && _selectedIndex < _shortcuts.Count)
            {
                var selectedShortcut = _shortcuts[_selectedIndex];
                LaunchShortcut(selectedShortcut);
            }
        }

        /// <summary>
        /// Launches a specific shortcut and hides the popup
        /// </summary>
        /// <param name="shortcut">The shortcut to launch</param>
        private void LaunchShortcut(ShortcutItem shortcut)
        {
            try
            {
                // Hide popup first for immediate feedback
                HidePopup();
                
                // Launch the application
                var startInfo = new ProcessStartInfo
                {
                    FileName = shortcut.Path,
                    UseShellExecute = true, // This allows launching various file types
                    ErrorDialog = true
                };
                
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error launching shortcut '{shortcut.Name}': {ex.Message}");
                
                // Show error dialog to user
                // Note: In a production app, you might want to show a more user-friendly error
                var dialog = new ContentDialog
                {
                    Title = "Launch Error",
                    Content = $"Could not launch '{shortcut.Name}':\n{ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                
                _ = dialog.ShowAsync();
            }
        }

        /// <summary>
        /// Animates the popup window showing with fade-in and scale-up effect
        /// </summary>
        private async Task AnimateShowAsync()
        {
            try
            {
                // Create fade-in animation
                var fadeInStoryboard = new Storyboard();
                
                var opacityAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                
                Storyboard.SetTarget(opacityAnimation, this);
                Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
                fadeInStoryboard.Children.Add(opacityAnimation);
                
                // Create scale animation for the main grid
                var scaleAnimation = new DoubleAnimationUsingKeyFrames();
                scaleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
                {
                    KeyTime = TimeSpan.FromMilliseconds(0),
                    Value = 0.9
                });
                scaleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
                {
                    KeyTime = TimeSpan.FromMilliseconds(200),
                    Value = 1.0,
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                });
                
                // Apply scale transform to the main content
                var scaleTransform = new Microsoft.UI.Xaml.Media.ScaleTransform();
                this.Content.RenderTransform = scaleTransform;
                this.Content.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
                
                Storyboard.SetTarget(scaleAnimation, scaleTransform);
                Storyboard.SetTargetProperty(scaleAnimation, "ScaleX");
                fadeInStoryboard.Children.Add(scaleAnimation);
                
                var scaleYAnimation = new DoubleAnimationUsingKeyFrames();
                scaleYAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
                {
                    KeyTime = TimeSpan.FromMilliseconds(0),
                    Value = 0.9
                });
                scaleYAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
                {
                    KeyTime = TimeSpan.FromMilliseconds(200),
                    Value = 1.0,
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                });
                
                Storyboard.SetTarget(scaleYAnimation, scaleTransform);
                Storyboard.SetTargetProperty(scaleYAnimation, "ScaleY");
                fadeInStoryboard.Children.Add(scaleYAnimation);
                
                // Start animation
                fadeInStoryboard.Begin();
                
                // Wait for animation to complete
                await Task.Delay(200);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in show animation: {ex.Message}");
                // Fallback: just set opacity to 1
                this.Opacity = 1;
            }
        }

        /// <summary>
        /// Animates the popup window hiding with fade-out and scale-down effect
        /// </summary>
        private async Task AnimateHideAsync()
        {
            try
            {
                // Create fade-out animation
                var fadeOutStoryboard = new Storyboard();
                
                var opacityAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(150),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                
                Storyboard.SetTarget(opacityAnimation, this);
                Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
                fadeOutStoryboard.Children.Add(opacityAnimation);
                
                // Create scale animation
                if (this.Content.RenderTransform is Microsoft.UI.Xaml.Media.ScaleTransform scaleTransform)
                {
                    var scaleAnimation = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 0.95,
                        Duration = TimeSpan.FromMilliseconds(150),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                    };
                    
                    Storyboard.SetTarget(scaleAnimation, scaleTransform);
                    Storyboard.SetTargetProperty(scaleAnimation, "ScaleX");
                    fadeOutStoryboard.Children.Add(scaleAnimation);
                    
                    var scaleYAnimation = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 0.95,
                        Duration = TimeSpan.FromMilliseconds(150),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                    };
                    
                    Storyboard.SetTarget(scaleYAnimation, scaleTransform);
                    Storyboard.SetTargetProperty(scaleYAnimation, "ScaleY");
                    fadeOutStoryboard.Children.Add(scaleYAnimation);
                }
                
                // Start animation
                fadeOutStoryboard.Begin();
                
                // Wait for animation to complete
                await Task.Delay(150);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in hide animation: {ex.Message}");
                // Fallback: just set opacity to 0
                this.Opacity = 0;
            }
        }

        #endregion
    }
}