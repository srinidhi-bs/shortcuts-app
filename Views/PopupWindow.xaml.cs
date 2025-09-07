using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ShortcutsApp.Models;
using ShortcutsApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Windows.System;
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
        private readonly LaunchingService _launchingService;
        private ObservableCollection<ShortcutDisplayItem> _shortcuts;
        private int _currentSelectedIndex = 0;
        private int _gridColumns = 6; // Default, will be updated from settings
        
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the PopupWindow
        /// </summary>
        /// <param name="settingsService">Service for loading settings and shortcuts</param>
        /// <param name="launchingService">Service for launching applications and files</param>
        public PopupWindow(SettingsService settingsService, LaunchingService launchingService)
        {
            this.InitializeComponent();
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _launchingService = launchingService ?? throw new ArgumentNullException(nameof(launchingService));
            
            // Initialize shortcuts collection
            _shortcuts = new ObservableCollection<ShortcutDisplayItem>();
            
            // Configure window properties for popup behavior
            ConfigureWindowProperties();
            
            // Load shortcuts and configure grid
            LoadShortcuts();
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
            // Window properties are already set in XAML using WinUIEx
            // Additional configuration can be done here if needed
            
            // Configure window styling
            this.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the popup window and refreshes shortcuts
        /// </summary>
        public void ShowPopup()
        {
            try
            {
                // Refresh shortcuts from settings
                LoadShortcuts();
                
                // Reset selection to first item
                if (_shortcuts.Count > 0)
                {
                    _currentSelectedIndex = 0;
                    ShortcutsGridView.SelectedIndex = 0;
                    ShortcutsGridView.Focus(FocusState.Programmatic);
                }
                
                this.Activate();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing popup: {ex.Message}");
            }
        }

        /// <summary>
        /// Hides the popup window
        /// </summary>
        public void HidePopup()
        {
            try
            {
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
        /// Loads shortcuts from settings and updates the grid view
        /// </summary>
        private async void LoadShortcuts()
        {
            try
            {
                var settings = await _settingsService.LoadSettingsAsync();
                _gridColumns = settings.Appearance.GridColumns;
                
                // Clear existing shortcuts
                _shortcuts.Clear();
                
                // Convert settings shortcuts to display items
                var displayShortcuts = settings.Shortcuts
                    .OrderBy(s => s.Order)
                    .Select(s => new ShortcutDisplayItem
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Path = s.Path,
                        IconPath = s.IconPath,
                        Order = s.Order
                    });
                
                foreach (var shortcut in displayShortcuts)
                {
                    _shortcuts.Add(shortcut);
                }
                
                // Update grid view data source
                ShortcutsGridView.ItemsSource = _shortcuts;
                
                // Show/hide empty state
                if (_shortcuts.Count == 0)
                {
                    EmptyStatePanel.Visibility = Visibility.Visible;
                    ShortcutsScrollViewer.Visibility = Visibility.Collapsed;
                }
                else
                {
                    EmptyStatePanel.Visibility = Visibility.Collapsed;
                    ShortcutsScrollViewer.Visibility = Visibility.Visible;
                }
                
                // Configure dynamic grid sizing based on user preferences
                ConfigureGridLayout(settings.Appearance);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading shortcuts: {ex.Message}");
                
                // Show empty state on error
                EmptyStatePanel.Visibility = Visibility.Visible;
                ShortcutsScrollViewer.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Configures the grid layout based on appearance settings
        /// </summary>
        /// <param name="appearance">Appearance settings from user configuration</param>
        private void ConfigureGridLayout(AppearanceSettings appearance)
        {
            try
            {
                // Set grid columns based on user preference
                _gridColumns = Math.Max(1, appearance.GridColumns);
                
                // Configure item size based on icon size setting
                var itemSize = Math.Max(60, appearance.IconSize + 16); // Icon size + padding
                
                // Update opacity
                MainBorder.Opacity = Math.Max(0.1, Math.Min(1.0, appearance.PopupOpacity));
                
                Debug.WriteLine($"Grid configured: {_gridColumns} columns, {itemSize}px items, {appearance.PopupOpacity} opacity");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error configuring grid layout: {ex.Message}");
            }
        }

        /// <summary>
        /// Launches the selected shortcut application using the improved LaunchingService
        /// Provides comprehensive error handling and user feedback for failed launches
        /// </summary>
        /// <param name="shortcut">The shortcut to launch</param>
        private async void LaunchShortcut(ShortcutDisplayItem shortcut)
        {
            try
            {
                // Validate shortcut data before attempting launch
                if (string.IsNullOrEmpty(shortcut.Path))
                {
                    await ShowErrorDialog("Launch Error", "Cannot launch shortcut: Path is empty");
                    return;
                }

                // Use the improved LaunchingService for better file type support and error handling
                var launchResult = await _launchingService.LaunchAsync(shortcut.Path);
                
                if (launchResult.Success)
                {
                    Debug.WriteLine($"Successfully launched: {shortcut.Name} at {launchResult.LaunchTime}");
                    
                    // Hide popup after successful launch
                    HidePopup();
                    
                    // TODO: Track usage for recently used shortcuts feature
                    // await TrackShortcutUsage(shortcut);
                }
                else
                {
                    // Show user-friendly error dialog with specific error information
                    Debug.WriteLine($"Failed to launch {shortcut.Name}: {launchResult.ErrorMessage}");
                    await ShowErrorDialog($"Failed to Launch '{shortcut.Name}'", 
                        launchResult.ErrorMessage ?? "Unknown error occurred");
                }
            }
            catch (Exception ex)
            {
                // Handle any unexpected errors that weren't caught by LaunchingService
                var errorMessage = $"Unexpected error launching '{shortcut.Name}': {ex.Message}";
                Debug.WriteLine(errorMessage);
                await ShowErrorDialog("Unexpected Error", errorMessage);
            }
        }

        /// <summary>
        /// Shows an error dialog to inform the user about launch failures
        /// Provides a better user experience than silent failures
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Error message to display</param>
        private async Task ShowErrorDialog(string title, string message)
        {
            try
            {
                var dialog = new ContentDialog()
                {
                    Title = title,
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                // Fallback to debug output if dialog fails
                Debug.WriteLine($"Error showing dialog: {ex.Message}. Original error: {title} - {message}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles grid view item clicks (mouse/touch selection)
        /// </summary>
        private void ShortcutsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                if (e.ClickedItem is ShortcutDisplayItem shortcut)
                {
                    LaunchShortcut(shortcut);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling item click: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles selection changes in the grid view
        /// </summary>
        private void ShortcutsGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                _currentSelectedIndex = ShortcutsGridView.SelectedIndex;
                Debug.WriteLine($"Selection changed to index: {_currentSelectedIndex}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling selection change: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles keyboard navigation in the grid view
        /// </summary>
        private void ShortcutsGridView_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            try
            {
                switch (e.Key)
                {
                    case VirtualKey.Enter:
                    case VirtualKey.Space:
                        // Launch selected shortcut
                        if (ShortcutsGridView.SelectedItem is ShortcutDisplayItem selectedShortcut)
                        {
                            LaunchShortcut(selectedShortcut);
                        }
                        e.Handled = true;
                        break;
                        
                    case VirtualKey.Escape:
                        // Hide popup
                        HidePopup();
                        e.Handled = true;
                        break;
                        
                    case VirtualKey.Up:
                        // Move up by one row (gridColumns items)
                        NavigateUp();
                        e.Handled = true;
                        break;
                        
                    case VirtualKey.Down:
                        // Move down by one row (gridColumns items)
                        NavigateDown();
                        e.Handled = true;
                        break;
                        
                    case VirtualKey.Left:
                        // Move left by one item
                        NavigateLeft();
                        e.Handled = true;
                        break;
                        
                    case VirtualKey.Right:
                        // Move right by one item
                        NavigateRight();
                        e.Handled = true;
                        break;
                        
                    case VirtualKey.Home:
                        // Go to first item
                        NavigateToFirst();
                        e.Handled = true;
                        break;
                        
                    case VirtualKey.End:
                        // Go to last item
                        NavigateToLast();
                        e.Handled = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling key down: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles focus events to ensure proper keyboard navigation
        /// </summary>
        private void ShortcutsGridView_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ensure an item is selected when focus is received
                if (ShortcutsGridView.SelectedIndex == -1 && _shortcuts.Count > 0)
                {
                    ShortcutsGridView.SelectedIndex = 0;
                    _currentSelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling got focus: {ex.Message}");
            }
        }

        #endregion

        #region Navigation Methods

        /// <summary>
        /// Navigates up by one row in the grid
        /// </summary>
        private void NavigateUp()
        {
            if (_shortcuts.Count == 0) return;
            
            var newIndex = _currentSelectedIndex - _gridColumns;
            if (newIndex >= 0)
            {
                _currentSelectedIndex = newIndex;
                ShortcutsGridView.SelectedIndex = _currentSelectedIndex;
            }
        }

        /// <summary>
        /// Navigates down by one row in the grid
        /// </summary>
        private void NavigateDown()
        {
            if (_shortcuts.Count == 0) return;
            
            var newIndex = _currentSelectedIndex + _gridColumns;
            if (newIndex < _shortcuts.Count)
            {
                _currentSelectedIndex = newIndex;
                ShortcutsGridView.SelectedIndex = _currentSelectedIndex;
            }
        }

        /// <summary>
        /// Navigates left by one item in the grid
        /// </summary>
        private void NavigateLeft()
        {
            if (_shortcuts.Count == 0) return;
            
            var newIndex = _currentSelectedIndex - 1;
            if (newIndex >= 0)
            {
                _currentSelectedIndex = newIndex;
                ShortcutsGridView.SelectedIndex = _currentSelectedIndex;
            }
        }

        /// <summary>
        /// Navigates right by one item in the grid
        /// </summary>
        private void NavigateRight()
        {
            if (_shortcuts.Count == 0) return;
            
            var newIndex = _currentSelectedIndex + 1;
            if (newIndex < _shortcuts.Count)
            {
                _currentSelectedIndex = newIndex;
                ShortcutsGridView.SelectedIndex = _currentSelectedIndex;
            }
        }

        /// <summary>
        /// Navigates to the first item in the grid
        /// </summary>
        private void NavigateToFirst()
        {
            if (_shortcuts.Count == 0) return;
            
            _currentSelectedIndex = 0;
            ShortcutsGridView.SelectedIndex = _currentSelectedIndex;
        }

        /// <summary>
        /// Navigates to the last item in the grid
        /// </summary>
        private void NavigateToLast()
        {
            if (_shortcuts.Count == 0) return;
            
            _currentSelectedIndex = _shortcuts.Count - 1;
            ShortcutsGridView.SelectedIndex = _currentSelectedIndex;
        }

        #endregion
    }

    /// <summary>
    /// Display model for shortcuts in the popup grid
    /// </summary>
    public class ShortcutDisplayItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string? IconPath { get; set; }
        public int Order { get; set; } = 0;
    }
}