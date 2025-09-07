using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ShortcutsApp.Services
{
    /// <summary>
    /// Service responsible for launching applications, files, and shortcuts with comprehensive error handling
    /// Supports various file types including executables, shortcuts, documents, and scripts
    /// </summary>
    public class LaunchingService
    {
        #region Private Fields
        
        // Supported executable file extensions
        private readonly string[] _executableExtensions = { ".exe", ".com", ".bat", ".cmd", ".msi" };
        
        // Document file extensions that should open with default applications
        private readonly string[] _documentExtensions = { ".txt", ".doc", ".docx", ".pdf", ".xls", ".xlsx", ".ppt", ".pptx", ".jpg", ".png", ".gif", ".mp4", ".avi" };
        
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the LaunchingService
        /// </summary>
        public LaunchingService()
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Launches an application or file based on the provided path
        /// Automatically detects file type and uses appropriate launching method
        /// </summary>
        /// <param name="filePath">Full path to the file or application to launch</param>
        /// <param name="arguments">Optional command-line arguments for executables</param>
        /// <returns>LaunchResult containing success status and any error information</returns>
        public async Task<LaunchResult> LaunchAsync(string filePath, string? arguments = null)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return LaunchResult.CreateFailure("File path is empty or null");
                }

                // Normalize path and check if file exists
                var normalizedPath = Path.GetFullPath(filePath);
                if (!File.Exists(normalizedPath) && !Directory.Exists(normalizedPath))
                {
                    return LaunchResult.CreateFailure($"File or directory not found: {normalizedPath}");
                }

                Debug.WriteLine($"Attempting to launch: {normalizedPath}");

                // Determine launch method based on file extension
                var extension = Path.GetExtension(normalizedPath).ToLowerInvariant();
                
                LaunchResult result;
                if (extension == ".lnk")
                {
                    // Handle Windows shortcuts (.lnk files)
                    result = await LaunchShortcutAsync(normalizedPath);
                }
                else if (Array.Exists(_executableExtensions, ext => ext == extension))
                {
                    // Handle executable files
                    result = await LaunchExecutableAsync(normalizedPath, arguments);
                }
                else if (Array.Exists(_documentExtensions, ext => ext == extension) || string.IsNullOrEmpty(extension))
                {
                    // Handle documents and files without extensions
                    result = await LaunchDocumentAsync(normalizedPath);
                }
                else
                {
                    // Try to launch with default application for unknown file types
                    result = await LaunchWithDefaultAppAsync(normalizedPath);
                }

                if (result.Success)
                {
                    Debug.WriteLine($"Successfully launched: {normalizedPath}");
                }
                else
                {
                    Debug.WriteLine($"Failed to launch {normalizedPath}: {result.ErrorMessage}");
                }

                return result;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Unexpected error launching {filePath}: {ex.Message}";
                Debug.WriteLine($"Unexpected error launching file {filePath}: {ex.Message}");
                return LaunchResult.CreateFailure(errorMessage);
            }
        }

        /// <summary>
        /// Checks if a file path is valid and can potentially be launched
        /// </summary>
        /// <param name="filePath">Path to validate</param>
        /// <returns>True if the path appears valid for launching</returns>
        public bool IsValidLaunchPath(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return false;

                var normalizedPath = Path.GetFullPath(filePath);
                return File.Exists(normalizedPath) || Directory.Exists(normalizedPath);
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Private Launch Methods

        /// <summary>
        /// Launches a Windows shortcut (.lnk) file
        /// </summary>
        private async Task<LaunchResult> LaunchShortcutAsync(string shortcutPath)
        {
            try
            {
                // Use Process.Start to launch .lnk files - Windows will resolve the target
                var startInfo = new ProcessStartInfo
                {
                    FileName = shortcutPath,
                    UseShellExecute = true, // Required for .lnk files
                    WorkingDirectory = Path.GetDirectoryName(shortcutPath) ?? Environment.CurrentDirectory
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        return LaunchResult.CreateFailure("Failed to start shortcut - process returned null");
                    }
                    
                    // Don't wait for shortcuts as they might launch other processes
                    await Task.Delay(100); // Brief delay to check if process started
                    return LaunchResult.CreateSuccess();
                }
            }
            catch (Exception ex)
            {
                return LaunchResult.CreateFailure($"Error launching shortcut: {ex.Message}");
            }
        }

        /// <summary>
        /// Launches an executable file with optional arguments
        /// </summary>
        private async Task<LaunchResult> LaunchExecutableAsync(string executablePath, string? arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = arguments ?? string.Empty,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(executablePath) ?? Environment.CurrentDirectory
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        return LaunchResult.CreateFailure("Failed to start executable - process returned null");
                    }
                    
                    // Brief delay to check if process started successfully
                    await Task.Delay(100);
                    
                    // For some executables, check if they're still running (not all processes stay alive)
                    if (process.HasExited && process.ExitCode != 0)
                    {
                        return LaunchResult.CreateFailure($"Executable exited with error code: {process.ExitCode}");
                    }
                    
                    return LaunchResult.CreateSuccess();
                }
            }
            catch (Exception ex)
            {
                return LaunchResult.CreateFailure($"Error launching executable: {ex.Message}");
            }
        }

        /// <summary>
        /// Launches a document or media file with the default associated application
        /// </summary>
        private async Task<LaunchResult> LaunchDocumentAsync(string documentPath)
        {
            try
            {
                // Use shell execute to open with default application
                var startInfo = new ProcessStartInfo
                {
                    FileName = documentPath,
                    UseShellExecute = true,
                    Verb = "open" // Explicitly use "open" verb
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        return LaunchResult.CreateFailure("Failed to open document - no default application found");
                    }
                    
                    await Task.Delay(100);
                    return LaunchResult.CreateSuccess();
                }
            }
            catch (Exception ex)
            {
                return LaunchResult.CreateFailure($"Error opening document: {ex.Message}");
            }
        }

        /// <summary>
        /// Attempts to launch unknown file types with default application
        /// </summary>
        private async Task<LaunchResult> LaunchWithDefaultAppAsync(string filePath)
        {
            try
            {
                // Try using Windows shell to determine default application
                var startInfo = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{filePath}\"",
                    UseShellExecute = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        return LaunchResult.CreateFailure("Failed to launch with default application");
                    }
                    
                    await Task.Delay(100);
                    return LaunchResult.CreateSuccess();
                }
            }
            catch (Exception ex)
            {
                return LaunchResult.CreateFailure($"Error launching with default app: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Result of a launch operation, containing success status and error information
    /// </summary>
    public class LaunchResult
    {
        /// <summary>
        /// Whether the launch operation was successful
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// Error message if the launch failed, null if successful
        /// </summary>
        public string? ErrorMessage { get; private set; }

        /// <summary>
        /// Timestamp when the launch was attempted
        /// </summary>
        public DateTime LaunchTime { get; private set; }

        private LaunchResult(bool success, string? errorMessage = null)
        {
            Success = success;
            ErrorMessage = errorMessage;
            LaunchTime = DateTime.Now;
        }

        /// <summary>
        /// Creates a successful launch result
        /// </summary>
        public static LaunchResult CreateSuccess() => new LaunchResult(true);

        /// <summary>
        /// Creates a failed launch result with error message
        /// </summary>
        public static LaunchResult CreateFailure(string errorMessage) => new LaunchResult(false, errorMessage);
    }
}