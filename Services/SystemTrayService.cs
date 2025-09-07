using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace ShortcutsApp.Services;

/// <summary>
/// Service responsible for managing the system tray (notification area) integration.
/// This service handles:
/// - Creating and managing the system tray icon using Win32 APIs
/// - Providing right-click context menu functionality
/// - Showing/hiding the main application window
/// - Handling application shutdown from the system tray
/// 
/// Learning Note: This implementation uses Win32 APIs through P/Invoke to create
/// a system tray icon that's compatible with WinUI 3 applications.
/// Win32 APIs provide direct access to Windows system functionality.
/// </summary>
public class SystemTrayService : IDisposable
{
    #region Win32 API Declarations
    
    /// <summary>
    /// Win32 API constants and structures for system tray functionality.
    /// These are the low-level Windows APIs that control the notification area.
    /// 
    /// Learning Note: P/Invoke allows .NET applications to call functions
    /// in unmanaged libraries like Windows system DLLs.
    /// </summary>
    private const int WM_USER = 0x0400;
    private const int WM_TRAYICON = WM_USER + 1;
    private const int NIM_ADD = 0x00000000;
    private const int NIM_DELETE = 0x00000002;
    private const int NIF_MESSAGE = 0x00000001;
    private const int NIF_ICON = 0x00000002;
    private const int NIF_TIP = 0x00000004;
    
    /// <summary>
    /// Structure that contains information about a system tray icon.
    /// This mirrors the Windows NOTIFYICONDATA structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
    }
    
    /// <summary>
    /// Win32 API function to manage system tray icons.
    /// This is the core function for adding, removing, and modifying tray icons.
    /// </summary>
    [DllImport("shell32.dll")]
    private static extern bool Shell_NotifyIcon(uint dwMessage, [In] ref NOTIFYICONDATA pnid);
    
    #endregion
    
    #region Private Fields
    
    /// <summary>
    /// Reference to the main application window.
    /// We need this to show/hide the window and handle window interactions.
    /// </summary>
    private WindowEx? _mainWindow;
    
    /// <summary>
    /// Handle to the window used for receiving system tray messages.
    /// Win32 requires a window handle to send notification messages to.
    /// </summary>
    private IntPtr _windowHandle;
    
    /// <summary>
    /// System tray icon data structure.
    /// Contains all the information about our system tray icon.
    /// </summary>
    private NOTIFYICONDATA _notifyIconData;
    
    /// <summary>
    /// Icon used in the system tray.
    /// This is a System.Drawing.Icon that represents our application.
    /// </summary>
    private System.Drawing.Icon? _trayIcon;
    
    /// <summary>
    /// Flag to track if the service has been disposed.
    /// Prevents double disposal and ensures proper cleanup.
    /// </summary>
    private bool _disposed = false;
    
    /// <summary>
    /// Unique identifier for our system tray icon.
    /// This distinguishes our icon from other applications' icons.
    /// </summary>
    private readonly uint _iconId = 1000;
    
    #endregion
    
    #region Events
    
    /// <summary>
    /// Event raised when the user requests to show the main window.
    /// This can happen through double-click or other interactions.
    /// 
    /// Learning Note: Events provide loose coupling between components.
    /// The SystemTrayService doesn't need to know about specific window implementations.
    /// </summary>
    public event EventHandler? ShowMainWindowRequested;
    
    /// <summary>
    /// Event raised when the user requests application exit from the system tray.
    /// This allows the main application to perform cleanup before shutting down.
    /// </summary>
    public event EventHandler? ExitApplicationRequested;
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Initializes the system tray service with the specified main window.
    /// This method sets up the system tray icon using Win32 APIs.
    /// 
    /// Learning Note: Initialization methods separate object construction from setup,
    /// allowing for better error handling and more flexible configuration.
    /// </summary>
    /// <param name="mainWindow">The main application window to associate with the system tray</param>
    public void Initialize(WindowEx mainWindow)
    {
        try
        {
            // Store reference to main window
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
            
            // Get the window handle for Win32 API calls
            // WinUI 3 applications need to get their Win32 handle for system integration
            _windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(_mainWindow);
            
            // Create the system tray icon
            CreateTrayIcon();
            
            // Add the icon to the system tray
            AddToSystemTray();
        }
        catch (Exception ex)
        {
            // Handle initialization errors gracefully
            Debug.WriteLine($"Failed to initialize system tray: {ex.Message}");
            throw; // Re-throw to let caller handle the error
        }
    }
    
    /// <summary>
    /// Shows the system tray icon.
    /// This can be used to make the icon visible after hiding it.
    /// </summary>
    public void Show()
    {
        if (_disposed) return;
        
        try
        {
            // The icon is added during initialization, so this is mainly for re-showing
            // after it has been hidden (though we don't currently implement hiding)
            AddToSystemTray();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error showing system tray icon: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Hides the system tray icon.
    /// This removes the icon from the notification area.
    /// </summary>
    public void Hide()
    {
        if (_disposed) return;
        
        try
        {
            RemoveFromSystemTray();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error hiding system tray icon: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Private Helper Methods
    
    /// <summary>
    /// Creates the system tray icon using System.Drawing.
    /// This sets up the visual representation that will appear in the notification area.
    /// 
    /// Learning Note: System.Drawing.Icon is part of the Windows Forms library
    /// but can be used independently for system tray functionality.
    /// </summary>
    private void CreateTrayIcon()
    {
        try
        {
            // For now, use the default application icon
            // In a production app, you'd typically load a custom .ico file
            _trayIcon = System.Drawing.SystemIcons.Application;
            
            // Initialize the notification icon data structure
            _notifyIconData = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _windowHandle,
                uID = _iconId,
                uFlags = NIF_ICON | NIF_MESSAGE | NIF_TIP,
                uCallbackMessage = WM_TRAYICON,
                hIcon = _trayIcon.Handle,
                szTip = "Shortcuts App - Global Launcher"
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating tray icon: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Adds the system tray icon to the notification area.
    /// This makes the icon visible to the user and enables interaction.
    /// 
    /// Learning Note: Shell_NotifyIcon is the core Win32 function for managing
    /// system tray icons. NIM_ADD tells Windows to add a new icon.
    /// </summary>
    private void AddToSystemTray()
    {
        try
        {
            bool result = Shell_NotifyIcon(NIM_ADD, ref _notifyIconData);
            if (!result)
            {
                // Get the last Win32 error for debugging
                int error = Marshal.GetLastWin32Error();
                Debug.WriteLine($"Failed to add system tray icon. Win32 Error: {error}");
                throw new Win32Exception(error, "Failed to add system tray icon");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error adding to system tray: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Removes the system tray icon from the notification area.
    /// This is called during cleanup or when hiding the icon.
    /// </summary>
    private void RemoveFromSystemTray()
    {
        try
        {
            if (_windowHandle != IntPtr.Zero)
            {
                Shell_NotifyIcon(NIM_DELETE, ref _notifyIconData);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error removing from system tray: {ex.Message}");
            // Don't throw during cleanup operations
        }
    }
    
    #endregion
    
    #region Message Handling
    
    /// <summary>
    /// Handles system tray icon messages from Windows.
    /// This is called from the main window's message procedure to process tray icon interactions.
    /// </summary>
    /// <param name="msg">The Windows message</param>
    /// <param name="wParam">Message parameter</param>
    /// <param name="lParam">Message parameter</param>
    public void HandleTrayIconMessage(uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_TRAYICON && wParam.ToInt32() == _iconId)
        {
            uint mouseMessage = (uint)(lParam.ToInt32() & 0xFFFF);
            
            switch (mouseMessage)
            {
                case 0x0202: // WM_LBUTTONUP - Left mouse button up
                    // Single left click - show main window (common behavior for system tray apps)
                    Debug.WriteLine("System tray left click detected");
                    ShowMainWindowRequested?.Invoke(this, EventArgs.Empty);
                    break;
                    
                case 0x0203: // WM_LBUTTONDBLCLK - Double click
                    // Double click - also show main window (redundant but commonly expected)
                    Debug.WriteLine("System tray double click detected");
                    ShowMainWindowRequested?.Invoke(this, EventArgs.Empty);
                    break;
                    
                case 0x0205: // WM_RBUTTONUP - Right mouse button up
                    // Right click - show main window for now (could be context menu later)
                    Debug.WriteLine("System tray right click detected");
                    ShowMainWindowRequested?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }
    
    #endregion
    
    #region IDisposable Implementation
    
    /// <summary>
    /// Releases all resources used by the SystemTrayService.
    /// This ensures proper cleanup of Win32 resources and system tray icons.
    /// 
    /// Learning Note: When using Win32 APIs and system resources, proper disposal
    /// is critical to prevent resource leaks and ensure clean application shutdown.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// Protected dispose method that handles the actual cleanup.
    /// This follows the standard dispose pattern for proper resource management.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                try
                {
                    // Remove the system tray icon
                    RemoveFromSystemTray();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error during system tray cleanup: {ex.Message}");
                }
            }
            
            // Clean up unmanaged resources
            // The system tray icon removal above handles our main unmanaged resource
            
            _disposed = true;
        }
    }
    
    /// <summary>
    /// Finalizer to ensure resources are cleaned up if Dispose() is not called.
    /// This provides a safety net for resource cleanup.
    /// 
    /// Learning Note: Finalizers are important when working with unmanaged resources
    /// like Win32 APIs, but proper disposal should always be done explicitly.
    /// </summary>
    ~SystemTrayService()
    {
        Dispose(disposing: false);
    }
    
    #endregion
}