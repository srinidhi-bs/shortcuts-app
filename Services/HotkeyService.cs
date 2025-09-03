using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinUIEx;
using ShortcutsApp.Models;

namespace ShortcutsApp.Services;

/// <summary>
/// Service responsible for managing global hotkey registration and handling.
/// This service provides:
/// - Registration and unregistration of global system hotkeys using Win32 APIs
/// - Handling of hotkey activation events from Windows
/// - Management of hotkey conflicts and validation
/// - Support for configurable modifier keys and key combinations
/// 
/// Learning Note: Global hotkeys work by registering key combinations with Windows
/// that will be captured system-wide, even when the application is not in focus.
/// This uses Win32 APIs through P/Invoke for direct system integration.
/// </summary>
public class HotkeyService : IDisposable
{
    #region Win32 API Declarations
    
    /// <summary>
    /// Win32 API constants and functions for global hotkey registration.
    /// These are the low-level Windows APIs that control global hotkey functionality.
    /// 
    /// Learning Note: RegisterHotKey and UnregisterHotKey are the core Win32 functions
    /// for managing global hotkeys. They require a window handle to receive messages.
    /// </summary>
    private const int WM_HOTKEY = 0x0312;
    
    /// <summary>
    /// Virtual key codes for common modifier keys.
    /// These constants represent the keyboard modifier keys in Windows.
    /// </summary>
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    
    /// <summary>
    /// Registers a global hotkey with Windows.
    /// This function tells Windows to send WM_HOTKEY messages when the specified
    /// key combination is pressed, regardless of which application has focus.
    /// </summary>
    /// <param name="hWnd">Handle to window that will receive WM_HOTKEY messages</param>
    /// <param name="id">Unique identifier for this hotkey (0-0xBFFF)</param>
    /// <param name="fsModifiers">Modifier keys (Ctrl, Alt, Shift, Win)</param>
    /// <param name="vk">Virtual key code of the key to register</param>
    /// <returns>True if successful, false if registration failed</returns>
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    
    /// <summary>
    /// Unregisters a previously registered global hotkey.
    /// This removes the hotkey registration and stops Windows from sending
    /// WM_HOTKEY messages for this key combination.
    /// </summary>
    /// <param name="hWnd">Handle to window that registered the hotkey</param>
    /// <param name="id">Unique identifier of the hotkey to unregister</param>
    /// <returns>True if successful, false if unregistration failed</returns>
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    
    /// <summary>
    /// Converts a string key name to a virtual key code.
    /// This function maps key names (like "A", "Space", "F1") to their
    /// corresponding Windows virtual key codes.
    /// </summary>
    /// <param name="lpszName">Name of the key (e.g., "A", "Space", "F1")</param>
    /// <returns>Virtual key code, or 0 if the key name is invalid</returns>
    [DllImport("user32.dll")]
    private static extern uint VkKeyScan(char ch);
    
    #endregion
    
    #region Private Fields
    
    /// <summary>
    /// Reference to the main application window.
    /// We need this to get the window handle for hotkey registration.
    /// </summary>
    private WindowEx? _mainWindow;
    
    /// <summary>
    /// Handle to the window used for receiving hotkey messages.
    /// Win32 hotkey registration requires a window handle to send messages to.
    /// </summary>
    private IntPtr _windowHandle;
    
    /// <summary>
    /// Current hotkey settings from the application configuration.
    /// This contains the key combination and modifier settings.
    /// </summary>
    private HotKeySettings? _currentHotkey;
    
    /// <summary>
    /// Flag to track if the service has been disposed.
    /// Prevents double disposal and ensures proper cleanup.
    /// </summary>
    private bool _disposed = false;
    
    /// <summary>
    /// Unique identifier for our registered hotkey.
    /// This distinguishes our hotkey from other applications' hotkeys.
    /// Windows requires each hotkey to have a unique ID per window.
    /// </summary>
    private readonly int _hotkeyId = 9000;
    
    /// <summary>
    /// Flag indicating whether a hotkey is currently registered.
    /// This helps prevent duplicate registrations and ensures proper cleanup.
    /// </summary>
    private bool _isRegistered = false;
    
    #endregion
    
    #region Events
    
    /// <summary>
    /// Event raised when the registered global hotkey is activated.
    /// This is the main event that applications should subscribe to
    /// in order to respond to hotkey presses.
    /// 
    /// Learning Note: Events provide loose coupling between the hotkey service
    /// and the application logic that responds to hotkey activation.
    /// </summary>
    public event EventHandler? HotkeyActivated;
    
    /// <summary>
    /// Event raised when hotkey registration fails.
    /// This allows the application to handle registration errors gracefully,
    /// such as when the requested hotkey is already in use by another application.
    /// </summary>
    public event EventHandler<string>? HotkeyRegistrationFailed;
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Initializes the hotkey service with the specified main window.
    /// This method prepares the service for hotkey registration but doesn't
    /// register any hotkeys yet. Call RegisterHotkey() to actually register.
    /// 
    /// Learning Note: Separation of initialization and registration allows
    /// for better error handling and more flexible configuration.
    /// </summary>
    /// <param name="mainWindow">The main application window for message handling</param>
    public void Initialize(WindowEx mainWindow)
    {
        try
        {
            // Store reference to main window
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
            
            // Get the window handle for Win32 API calls
            // WinUI 3 applications need their Win32 handle for system integration
            _windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(_mainWindow);
            
            Debug.WriteLine("HotkeyService initialized successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize hotkey service: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Registers a global hotkey based on the provided settings.
    /// This method converts the high-level hotkey settings into Win32 API calls
    /// to register the hotkey with the Windows system.
    /// 
    /// Learning Note: This method demonstrates how to translate user-friendly
    /// settings (like "Control" + "Space") into low-level system calls.
    /// </summary>
    /// <param name="hotkeySettings">The hotkey configuration to register</param>
    /// <returns>True if registration was successful, false otherwise</returns>
    public bool RegisterHotkey(HotKeySettings hotkeySettings)
    {
        if (_disposed || _windowHandle == IntPtr.Zero)
        {
            Debug.WriteLine("Cannot register hotkey: Service not properly initialized");
            return false;
        }
        
        if (hotkeySettings == null || !hotkeySettings.Enabled)
        {
            Debug.WriteLine("Hotkey registration skipped: Settings null or disabled");
            return false;
        }
        
        try
        {
            // Unregister existing hotkey if one is already registered
            if (_isRegistered)
            {
                UnregisterHotkey();
            }
            
            // Convert settings to Win32 modifier flags
            uint modifiers = ConvertModifiersToWin32(hotkeySettings.Modifiers);
            
            // Convert key name to virtual key code
            uint virtualKeyCode = ConvertKeyNameToVirtualKey(hotkeySettings.Key);
            
            if (virtualKeyCode == 0)
            {
                string errorMsg = $"Invalid key name: {hotkeySettings.Key}";
                Debug.WriteLine(errorMsg);
                HotkeyRegistrationFailed?.Invoke(this, errorMsg);
                return false;
            }
            
            // Register the hotkey with Windows
            bool success = RegisterHotKey(_windowHandle, _hotkeyId, modifiers, virtualKeyCode);
            
            if (success)
            {
                _currentHotkey = hotkeySettings;
                _isRegistered = true;
                Debug.WriteLine($"Hotkey registered successfully: {string.Join("+", hotkeySettings.Modifiers)} + {hotkeySettings.Key}");
                return true;
            }
            else
            {
                // Get the specific error reason
                int error = Marshal.GetLastWin32Error();
                string errorMsg = $"Failed to register hotkey. Win32 Error: {error}. " +
                                $"Hotkey may already be in use by another application.";
                Debug.WriteLine(errorMsg);
                HotkeyRegistrationFailed?.Invoke(this, errorMsg);
                return false;
            }
        }
        catch (Exception ex)
        {
            string errorMsg = $"Exception during hotkey registration: {ex.Message}";
            Debug.WriteLine(errorMsg);
            HotkeyRegistrationFailed?.Invoke(this, errorMsg);
            return false;
        }
    }
    
    /// <summary>
    /// Unregisters the currently registered global hotkey.
    /// This removes the hotkey from the Windows system and stops
    /// receiving activation events.
    /// </summary>
    /// <returns>True if unregistration was successful, false otherwise</returns>
    public bool UnregisterHotkey()
    {
        if (_disposed || _windowHandle == IntPtr.Zero || !_isRegistered)
        {
            return true; // Already unregistered or not initialized
        }
        
        try
        {
            bool success = UnregisterHotKey(_windowHandle, _hotkeyId);
            
            if (success)
            {
                _isRegistered = false;
                _currentHotkey = null;
                Debug.WriteLine("Hotkey unregistered successfully");
                return true;
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                Debug.WriteLine($"Failed to unregister hotkey. Win32 Error: {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception during hotkey unregistration: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Gets information about the currently registered hotkey.
    /// This is useful for displaying current hotkey settings to the user.
    /// </summary>
    /// <returns>Current hotkey settings, or null if no hotkey is registered</returns>
    public HotKeySettings? GetCurrentHotkey()
    {
        return _currentHotkey;
    }
    
    /// <summary>
    /// Checks if a hotkey is currently registered and active.
    /// This can be used to display registration status in the UI.
    /// </summary>
    /// <returns>True if a hotkey is registered, false otherwise</returns>
    public bool IsHotkeyRegistered()
    {
        return _isRegistered && _currentHotkey != null;
    }
    
    #endregion
    
    #region Private Helper Methods
    
    /// <summary>
    /// Converts a list of modifier key names to Win32 modifier flags.
    /// This translates user-friendly modifier names like "Control" and "Alt"
    /// into the bit flags that the Win32 API expects.
    /// 
    /// Learning Note: Win32 APIs often use bit flags to represent multiple
    /// options in a single parameter. Each flag is a power of 2 so they
    /// can be combined using bitwise OR operations.
    /// </summary>
    /// <param name="modifiers">List of modifier key names (e.g., "Control", "Alt")</param>
    /// <returns>Combined Win32 modifier flags</returns>
    private uint ConvertModifiersToWin32(List<string> modifiers)
    {
        uint result = 0;
        
        foreach (string modifier in modifiers)
        {
            switch (modifier.ToLower())
            {
                case "control":
                case "ctrl":
                    result |= MOD_CONTROL;
                    break;
                case "alt":
                    result |= MOD_ALT;
                    break;
                case "shift":
                    result |= MOD_SHIFT;
                    break;
                case "win":
                case "windows":
                    result |= MOD_WIN;
                    break;
                default:
                    Debug.WriteLine($"Unknown modifier key: {modifier}");
                    break;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Converts a key name to a Windows virtual key code.
    /// This maps user-friendly key names to the numeric codes that
    /// Windows uses internally to identify keys.
    /// 
    /// Learning Note: Virtual key codes are Windows' way of uniquely
    /// identifying each key on the keyboard, independent of layout or language.
    /// </summary>
    /// <param name="keyName">The name of the key (e.g., "Space", "A", "F1")</param>
    /// <returns>Virtual key code, or 0 if the key name is not recognized</returns>
    private uint ConvertKeyNameToVirtualKey(string keyName)
    {
        if (string.IsNullOrEmpty(keyName))
            return 0;
        
        // Handle special keys that don't map directly to characters
        switch (keyName.ToLower())
        {
            case "space":
                return 0x20; // VK_SPACE
            case "enter":
            case "return":
                return 0x0D; // VK_RETURN
            case "tab":
                return 0x09; // VK_TAB
            case "escape":
            case "esc":
                return 0x1B; // VK_ESCAPE
            case "backspace":
                return 0x08; // VK_BACK
            case "delete":
            case "del":
                return 0x2E; // VK_DELETE
            case "insert":
            case "ins":
                return 0x2D; // VK_INSERT
            case "home":
                return 0x24; // VK_HOME
            case "end":
                return 0x23; // VK_END
            case "pageup":
            case "pgup":
                return 0x21; // VK_PRIOR
            case "pagedown":
            case "pgdn":
                return 0x22; // VK_NEXT
            case "up":
                return 0x26; // VK_UP
            case "down":
                return 0x28; // VK_DOWN
            case "left":
                return 0x25; // VK_LEFT
            case "right":
                return 0x27; // VK_RIGHT
            // Function keys
            case "f1": return 0x70;
            case "f2": return 0x71;
            case "f3": return 0x72;
            case "f4": return 0x73;
            case "f5": return 0x74;
            case "f6": return 0x75;
            case "f7": return 0x76;
            case "f8": return 0x77;
            case "f9": return 0x78;
            case "f10": return 0x79;
            case "f11": return 0x7A;
            case "f12": return 0x7B;
            // Number row keys
            case "0": return 0x30;
            case "1": return 0x31;
            case "2": return 0x32;
            case "3": return 0x33;
            case "4": return 0x34;
            case "5": return 0x35;
            case "6": return 0x36;
            case "7": return 0x37;
            case "8": return 0x38;
            case "9": return 0x39;
        }
        
        // For single character keys (A-Z), convert to virtual key code
        if (keyName.Length == 1)
        {
            char keyChar = char.ToUpper(keyName[0]);
            
            // Letter keys A-Z map directly to their ASCII values
            if (keyChar >= 'A' && keyChar <= 'Z')
            {
                return (uint)keyChar;
            }
            
            // For other single characters, try using VkKeyScan
            // Note: VkKeyScan returns both the virtual key and shift state,
            // we only want the virtual key (lower byte)
            short vkResult = (short)VkKeyScan(keyChar);
            if (vkResult != -1)
            {
                return (uint)(vkResult & 0xFF);
            }
        }
        
        Debug.WriteLine($"Could not convert key name to virtual key code: {keyName}");
        return 0; // Unknown key
    }
    
    #endregion
    
    #region Message Handling
    
    /// <summary>
    /// Handles hotkey activation messages from Windows.
    /// This method should be called from the window's message handler
    /// when WM_HOTKEY messages are received.
    /// 
    /// Learning Note: In a complete WinUI 3 implementation, you would need
    /// to subclass or hook the window procedure to receive these messages.
    /// For now, we're providing the structure for message handling.
    /// </summary>
    /// <param name="msg">The Windows message ID</param>
    /// <param name="wParam">Message parameter (contains hotkey ID)</param>
    /// <param name="lParam">Message parameter (contains key details)</param>
    public void HandleHotkeyMessage(uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == _hotkeyId)
        {
            Debug.WriteLine("Global hotkey activated!");
            
            // Raise the activation event
            HotkeyActivated?.Invoke(this, EventArgs.Empty);
        }
    }
    
    #endregion
    
    #region IDisposable Implementation
    
    /// <summary>
    /// Releases all resources used by the HotkeyService.
    /// This ensures proper cleanup of hotkey registrations and prevents
    /// system resource leaks.
    /// 
    /// Learning Note: When working with system-wide resources like global hotkeys,
    /// proper disposal is critical to ensure other applications can use those
    /// key combinations after your app exits.
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
                    // Unregister any active hotkey
                    UnregisterHotkey();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error during hotkey cleanup: {ex.Message}");
                }
            }
            
            // Clean up unmanaged resources
            // The hotkey unregistration above handles our main system resource
            
            _disposed = true;
        }
    }
    
    /// <summary>
    /// Finalizer to ensure hotkeys are unregistered if Dispose() is not called.
    /// This provides a safety net for resource cleanup.
    /// 
    /// Learning Note: Global hotkeys are system-wide resources that must be
    /// cleaned up properly. The finalizer ensures this happens even if the
    /// application doesn't dispose properly.
    /// </summary>
    ~HotkeyService()
    {
        Dispose(disposing: false);
    }
    
    #endregion
}