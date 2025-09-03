using Microsoft.UI.Xaml.Navigation;
using WinUIEx;
using ShortcutsApp.Services;

namespace ShortcutsApp
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// This class manages the application lifecycle, services, and system tray integration.
    /// 
    /// Learning Note: The App class is the entry point for WinUI 3 applications.
    /// It handles application startup, manages global resources, and coordinates services.
    /// </summary>
    public partial class App : Application
    {
        #region Private Fields
        
        /// <summary>
        /// Reference to the main application window.
        /// This is the primary UI window that contains our settings interface.
        /// </summary>
        private MainWindow? _mainWindow;
        
        /// <summary>
        /// Service container for dependency injection.
        /// Stores instances of services that can be accessed throughout the application.
        /// 
        /// Learning Note: Dependency injection is a design pattern that helps manage
        /// object dependencies and makes code more testable and maintainable.
        /// </summary>
        private readonly Dictionary<Type, object> _services = new();
        
        /// <summary>
        /// Service responsible for system tray (notification area) functionality.
        /// Handles showing/hiding the app icon in the system tray and context menu interactions.
        /// </summary>
        private SystemTrayService? _systemTrayService;
        
        /// <summary>
        /// Service responsible for global hotkey registration and handling.
        /// Manages system-wide keyboard shortcuts that work even when the app is not in focus.
        /// </summary>
        private HotkeyService? _hotkeyService;
        
        /// <summary>
        /// Service responsible for loading and saving application settings.
        /// Handles persistent storage of user preferences and configuration.
        /// </summary>
        private SettingsService? _settingsService;
        
        /// <summary>
        /// Popup window that displays shortcuts when the global hotkey is activated.
        /// This window appears as an overlay and allows users to quickly launch applications.
        /// </summary>
        private Views.PopupWindow? _popupWindow;
        
        #endregion

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.
        /// This method initializes the main window, system tray, and application services.
        /// 
        /// Learning Note: OnLaunched is the main entry point for WinUI 3 applications.
        /// This is where we set up the UI, initialize services, and prepare the app for user interaction.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            // Create the main application window
            _mainWindow = new MainWindow();
            _services[typeof(MainWindow)] = _mainWindow;

            // Set up the navigation framework
            if (_mainWindow.Content is not Frame rootFrame)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                _mainWindow.Content = rootFrame;
            }

            // Navigate to the main settings page
            _ = rootFrame.Navigate(typeof(Views.MainPage), e.Arguments);
            
            // Configure the main window properties
            _mainWindow.Title = "Shortcuts App - Settings";
            _mainWindow.SetWindowSize(800, 700);
            _mainWindow.CenterOnScreen();
            
            // Initialize application services
            InitializeServices();
            
            // Show the main window
            _mainWindow.Activate();
        }
        
        /// <summary>
        /// Initializes all application services and sets up event handlers.
        /// This method creates and configures the settings service, system tray, and hotkey service.
        /// 
        /// Learning Note: Separating initialization logic into dedicated methods
        /// makes the code more organized and easier to understand and maintain.
        /// Services are initialized in dependency order.
        /// </summary>
        private void InitializeServices()
        {
            // Initialize services in dependency order: Settings first, then other services
            
            // 1. Initialize Settings Service (required by other services)
            InitializeSettingsService();
            
            // 2. Initialize Popup Window (requires settings service)
            InitializePopupWindow();
            
            // 3. Initialize System Tray Service
            InitializeSystemTray();
            
            // 4. Initialize Hotkey Service
            InitializeHotkeyService();
        }
        
        /// <summary>
        /// Initializes the settings service for loading and saving application configuration.
        /// This service must be initialized first as other services depend on it.
        /// </summary>
        private void InitializeSettingsService()
        {
            try
            {
                _settingsService = new SettingsService();
                _services[typeof(SettingsService)] = _settingsService;
                
                System.Diagnostics.Debug.WriteLine("Settings service initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize settings service: {ex.Message}");
                throw; // Settings service is critical, so we can't continue without it
            }
        }
        
        /// <summary>
        /// Initializes the popup window that displays shortcuts when the global hotkey is pressed.
        /// This window shows all configured shortcuts in a grid layout for quick access.
        /// </summary>
        private void InitializePopupWindow()
        {
            try
            {
                // Create the popup window instance with the settings service
                _popupWindow = new Views.PopupWindow(_settingsService!);
                
                // Add it to the services container for dependency injection
                _services[typeof(Views.PopupWindow)] = _popupWindow;
                
                System.Diagnostics.Debug.WriteLine("Popup window initialized successfully");
            }
            catch (Exception ex)
            {
                // Handle any errors during popup window initialization
                // The app can continue without popup functionality if needed
                System.Diagnostics.Debug.WriteLine($"Failed to initialize popup window: {ex.Message}");
                _popupWindow = null;
            }
        }
        
        /// <summary>
        /// Initializes the system tray service and sets up event handlers.
        /// This method creates the system tray icon and configures its behavior.
        /// </summary>
        private void InitializeSystemTray()
        {
            try
            {
                // Create the system tray service instance
                _systemTrayService = new SystemTrayService();
                
                // Add it to the services container for dependency injection
                _services[typeof(SystemTrayService)] = _systemTrayService;
                
                // Initialize the service with the main window
                _systemTrayService.Initialize(_mainWindow);
                
                // Set up event handlers for system tray interactions
                _systemTrayService.ShowMainWindowRequested += OnShowMainWindowRequested;
                _systemTrayService.ExitApplicationRequested += OnExitApplicationRequested;
                
                System.Diagnostics.Debug.WriteLine("System tray service initialized successfully");
            }
            catch (Exception ex)
            {
                // Handle any errors during system tray initialization
                // This prevents the entire application from crashing if system tray setup fails
                System.Diagnostics.Debug.WriteLine($"Failed to initialize system tray: {ex.Message}");
                
                // Clean up if partially initialized
                _systemTrayService?.Dispose();
                _systemTrayService = null;
            }
        }
        
        /// <summary>
        /// Initializes the hotkey service and registers the global hotkey.
        /// This method sets up global keyboard shortcut handling based on user settings.
        /// </summary>
        private void InitializeHotkeyService()
        {
            try
            {
                // Create the hotkey service instance
                _hotkeyService = new HotkeyService();
                
                // Add it to the services container for dependency injection
                _services[typeof(HotkeyService)] = _hotkeyService;
                
                // Initialize the service with the main window
                _hotkeyService.Initialize(_mainWindow);
                
                // Set up event handlers for hotkey interactions
                _hotkeyService.HotkeyActivated += OnHotkeyActivated;
                _hotkeyService.HotkeyRegistrationFailed += OnHotkeyRegistrationFailed;
                
                // Register the global hotkey based on current settings
                RegisterGlobalHotkey();
                
                System.Diagnostics.Debug.WriteLine("Hotkey service initialized successfully");
            }
            catch (Exception ex)
            {
                // Handle any errors during hotkey initialization
                // The app can continue without hotkey functionality if needed
                System.Diagnostics.Debug.WriteLine($"Failed to initialize hotkey service: {ex.Message}");
                
                // Clean up if partially initialized
                _hotkeyService?.Dispose();
                _hotkeyService = null;
            }
        }
        
        /// <summary>
        /// Registers the global hotkey based on current application settings.
        /// This method loads the hotkey configuration and attempts to register it with Windows.
        /// </summary>
        private async void RegisterGlobalHotkey()
        {
            if (_hotkeyService == null || _settingsService == null)
                return;
                
            try
            {
                // Load current settings to get hotkey configuration
                var settings = await _settingsService.LoadSettingsAsync();
                
                // Register the hotkey if enabled
                if (settings.HotKey.Enabled)
                {
                    bool success = _hotkeyService.RegisterHotkey(settings.HotKey);
                    if (success)
                    {
                        System.Diagnostics.Debug.WriteLine($"Global hotkey registered: {string.Join("+", settings.HotKey.Modifiers)} + {settings.HotKey.Key}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Failed to register global hotkey");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Global hotkey is disabled in settings");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error registering global hotkey: {ex.Message}");
            }
        }
        
        #region System Tray Event Handlers
        
        /// <summary>
        /// Handles the request to show the main window from the system tray.
        /// This can be triggered by double-clicking the system tray icon or selecting "Show" from the context menu.
        /// 
        /// Learning Note: Event handlers in Windows applications typically restore minimized windows
        /// and bring them to the foreground so users can interact with them.
        /// </summary>
        /// <param name="sender">The system tray service that raised the event</param>
        /// <param name="e">Event arguments (empty in this case)</param>
        private void OnShowMainWindowRequested(object? sender, EventArgs e)
        {
            if (_mainWindow != null)
            {
                // If the window is minimized, restore it
                if (_mainWindow.Visible == false)
                {
                    _mainWindow.Activate();
                }
                
                // Bring the window to the front and give it focus
                // This ensures the user can see and interact with the application
                _mainWindow.BringToFront();
                _mainWindow.Activate();
            }
        }
        
        /// <summary>
        /// Handles the request to exit the application from the system tray.
        /// This performs cleanup and shuts down the application gracefully.
        /// 
        /// Learning Note: Proper application shutdown includes cleaning up resources,
        /// saving any unsaved data, and disposing of services before termination.
        /// </summary>
        /// <param name="sender">The system tray service that raised the event</param>
        /// <param name="e">Event arguments (empty in this case)</param>
        private void OnExitApplicationRequested(object? sender, EventArgs e)
        {
            // Perform cleanup before exiting
            CleanupServices();
            
            // Exit the application
            // This will trigger the application shutdown process
            this.Exit();
        }
        
        #endregion
        
        #region Hotkey Event Handlers
        
        /// <summary>
        /// Handles the global hotkey activation event.
        /// This is called when the user presses the registered hotkey combination.
        /// Currently shows the main window, but will eventually show the shortcuts popup.
        /// 
        /// Learning Note: For now, we're showing the main window as a placeholder.
        /// In the final implementation, this will show the shortcuts popup launcher.
        /// </summary>
        /// <param name="sender">The hotkey service that raised the event</param>
        /// <param name="e">Event arguments (empty in this case)</param>
        private void OnHotkeyActivated(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Global hotkey activated!");
            
            // Show the shortcuts popup window
            if (_popupWindow != null)
            {
                try
                {
                    _popupWindow.ShowPopup();
                    System.Diagnostics.Debug.WriteLine("Popup window displayed successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to show popup window: {ex.Message}");
                    // Fallback to showing main window if popup fails
                    OnShowMainWindowRequested(sender, e);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Popup window not initialized, showing main window instead");
                // Fallback to showing main window if popup is not available
                OnShowMainWindowRequested(sender, e);
            }
        }
        
        /// <summary>
        /// Handles hotkey registration failure events.
        /// This is called when the system cannot register the requested hotkey,
        /// typically because another application is already using that key combination.
        /// 
        /// Learning Note: Hotkey conflicts are common, especially with popular
        /// combinations like Ctrl+Space. Good UX handles this gracefully.
        /// </summary>
        /// <param name="sender">The hotkey service that raised the event</param>
        /// <param name="errorMessage">Description of why registration failed</param>
        private void OnHotkeyRegistrationFailed(object? sender, string errorMessage)
        {
            System.Diagnostics.Debug.WriteLine($"Hotkey registration failed: {errorMessage}");
            
            // TODO: In the future, we might want to show a user-friendly notification
            // or suggest alternative hotkey combinations
        }
        
        #endregion
        
        #region Service Management
        
        /// <summary>
        /// Generic method to retrieve services from the dependency injection container.
        /// This allows other parts of the application to access shared services.
        /// 
        /// Learning Note: Dependency injection containers provide a centralized way
        /// to manage object instances and their dependencies throughout an application.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve</typeparam>
        /// <returns>The service instance if found, null otherwise</returns>
        public T? GetService<T>() where T : class
        {
            _services.TryGetValue(typeof(T), out var service);
            return service as T;
        }
        
        /// <summary>
        /// Cleans up all services and releases resources.
        /// This is called during application shutdown to ensure proper cleanup.
        /// 
        /// Learning Note: Proper resource cleanup prevents memory leaks and
        /// ensures that system resources are released when the application closes.
        /// </summary>
        private void CleanupServices()
        {
            try
            {
                // Dispose of the hotkey service first (to unregister hotkeys immediately)
                if (_hotkeyService != null)
                {
                    _hotkeyService.HotkeyActivated -= OnHotkeyActivated;
                    _hotkeyService.HotkeyRegistrationFailed -= OnHotkeyRegistrationFailed;
                    _hotkeyService.Dispose();
                    _hotkeyService = null;
                    System.Diagnostics.Debug.WriteLine("Hotkey service cleaned up");
                }
                
                // Dispose of the system tray service
                if (_systemTrayService != null)
                {
                    _systemTrayService.ShowMainWindowRequested -= OnShowMainWindowRequested;
                    _systemTrayService.ExitApplicationRequested -= OnExitApplicationRequested;
                    _systemTrayService.Dispose();
                    _systemTrayService = null;
                    System.Diagnostics.Debug.WriteLine("System tray service cleaned up");
                }
                
                // Settings service doesn't need special cleanup, but clear the reference
                _settingsService = null;
                
                // Clean up any other services in the container
                foreach (var service in _services.Values)
                {
                    if (service is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                
                // Clear the services container
                _services.Clear();
                System.Diagnostics.Debug.WriteLine("All services cleaned up successfully");
            }
            catch (Exception ex)
            {
                // Log cleanup errors but don't throw exceptions during shutdown
                System.Diagnostics.Debug.WriteLine($"Error during service cleanup: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Navigation Event Handlers

        /// <summary>
        /// Invoked when Navigation to a certain page fails.
        /// This is a critical error that indicates a problem with the application structure.
        /// 
        /// Learning Note: Navigation failures usually indicate missing pages or incorrect routing.
        /// In production apps, you might want to show a user-friendly error message instead of crashing.
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
        
        #endregion
    }

    /// <summary>
    /// Custom main window that handles Win32 messages for global hotkey functionality.
    /// This class extends WindowEx to provide message handling capabilities needed
    /// for processing system-wide hotkey activation messages.
    /// 
    /// Learning Note: WinUI 3 applications need to handle Win32 messages manually
    /// for system integrations like global hotkeys that operate outside the normal
    /// WinUI event system.
    /// </summary>
    public class MainWindow : WindowEx
    {
        #region Win32 API Declarations for Message Handling
        
        /// <summary>
        /// Win32 constant for hotkey messages.
        /// When a registered global hotkey is pressed, Windows sends this message.
        /// </summary>
        private const int WM_HOTKEY = 0x0312;
        
        /// <summary>
        /// Win32 API function to set a window procedure (message handler).
        /// This allows us to intercept and handle Windows messages.
        /// </summary>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        
        /// <summary>
        /// Win32 API function to call the default window procedure.
        /// This handles messages we don't process ourselves.
        /// </summary>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        
        /// <summary>
        /// Constant for setting window procedure.
        /// </summary>
        private const int GWL_WNDPROC = -4;
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// Reference to the original window procedure.
        /// We need this to call the default handler for messages we don't process.
        /// </summary>
        private IntPtr _originalWndProc;
        
        /// <summary>
        /// Our custom window procedure delegate.
        /// This will be called for all Windows messages sent to this window.
        /// </summary>
        private readonly WndProcDelegate _wndProcDelegate;
        
        /// <summary>
        /// Delegate type for window procedure functions.
        /// </summary>
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        
        #endregion
        
        /// <summary>
        /// Initializes the main window with custom message handling capabilities.
        /// </summary>
        public MainWindow()
        {
            try
            {
                this.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
            }
            catch
            {
                // Fallback if Mica is not available
            }
            
            // Initialize the window procedure delegate
            _wndProcDelegate = new WndProcDelegate(WndProc);
            
            // Set up message handling when the window is loaded
            this.Activated += OnMainWindowActivated;
        }
        
        /// <summary>
        /// Event handler called when the window is activated (first shown).
        /// This is where we set up the custom message handling.
        /// </summary>
        /// <param name="sender">The window being activated</param>
        /// <param name="args">Activation arguments</param>
        private void OnMainWindowActivated(object sender, WindowActivatedEventArgs args)
        {
            // Only set up message handling once
            if (_originalWndProc == IntPtr.Zero)
            {
                SetupMessageHandling();
            }
        }
        
        /// <summary>
        /// Sets up custom Windows message handling for this window.
        /// This is necessary to receive WM_HOTKEY messages from the system.
        /// 
        /// Learning Note: This is a common pattern for integrating Win32 APIs
        /// with modern UI frameworks that don't natively support all Windows messages.
        /// </summary>
        private void SetupMessageHandling()
        {
            try
            {
                // Get the window handle
                IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                
                // Set our custom window procedure and save the original
                _originalWndProc = SetWindowLongPtr(hWnd, GWL_WNDPROC, 
                    System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
                
                System.Diagnostics.Debug.WriteLine("Custom message handling set up successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set up message handling: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Custom window procedure that handles Windows messages.
        /// This method is called for every Windows message sent to our window.
        /// 
        /// Learning Note: Window procedures are the fundamental mechanism for
        /// handling Windows messages. Every window has one, and they form the
        /// basis of the Windows message system.
        /// </summary>
        /// <param name="hWnd">Handle to the window receiving the message</param>
        /// <param name="msg">The message identifier</param>
        /// <param name="wParam">First message parameter</param>
        /// <param name="lParam">Second message parameter</param>
        /// <returns>Result of message processing</returns>
        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                // Handle hotkey messages
                if (msg == WM_HOTKEY)
                {
                    System.Diagnostics.Debug.WriteLine($"WM_HOTKEY message received: wParam={wParam}, lParam={lParam}");
                    
                    // Get the hotkey service from the app and forward the message
                    var app = (App)Application.Current;
                    var hotkeyService = app.GetService<HotkeyService>();
                    
                    if (hotkeyService != null)
                    {
                        hotkeyService.HandleHotkeyMessage(msg, wParam, lParam);
                        return IntPtr.Zero; // Message handled
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in WndProc: {ex.Message}");
            }
            
            // For all other messages, call the original window procedure
            return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
        }
    }
}
