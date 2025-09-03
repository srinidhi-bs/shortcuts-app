using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ShortcutsApp.Models;
using ShortcutsApp.Services;
using System;
using System.Diagnostics;
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
        /// Shows the popup window
        /// </summary>
        public void ShowPopup()
        {
            try
            {
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
    }
}