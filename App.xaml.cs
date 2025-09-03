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
            
            // Initialize system tray functionality
            InitializeSystemTray();
            
            // Show the main window
            _mainWindow.Activate();
        }
        
        /// <summary>
        /// Initializes the system tray service and sets up event handlers.
        /// This method creates the system tray icon and configures its behavior.
        /// 
        /// Learning Note: Separating initialization logic into dedicated methods
        /// makes the code more organized and easier to understand and maintain.
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
            }
            catch (Exception ex)
            {
                // Handle any errors during system tray initialization
                // This prevents the entire application from crashing if system tray setup fails
                
                // In a production app, you might want to log this error
                // For now, we'll just continue without system tray functionality
                System.Diagnostics.Debug.WriteLine($"Failed to initialize system tray: {ex.Message}");
                
                // Clean up if partially initialized
                _systemTrayService?.Dispose();
                _systemTrayService = null;
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
                // Dispose of the system tray service
                if (_systemTrayService != null)
                {
                    _systemTrayService.ShowMainWindowRequested -= OnShowMainWindowRequested;
                    _systemTrayService.ExitApplicationRequested -= OnExitApplicationRequested;
                    _systemTrayService.Dispose();
                    _systemTrayService = null;
                }
                
                // Clean up other services as needed
                // In the future, we might have other services that need disposal
                foreach (var service in _services.Values)
                {
                    if (service is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                
                // Clear the services container
                _services.Clear();
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

    public class MainWindow : WindowEx
    {
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
        }
    }
}
