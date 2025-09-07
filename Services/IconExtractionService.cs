using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ShortcutsApp.Services
{
    /// <summary>
    /// Service responsible for extracting and caching icons from executables, shortcuts, and files
    /// Provides high-quality icons for the shortcuts popup and fallback icons for unsupported types
    /// </summary>
    public class IconExtractionService
    {
        #region Win32 API Imports
        
        // Import Win32 APIs for icon extraction
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, int nIcons);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        // Constants for SHGetFileInfo
        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_LARGEICON = 0x0;
        private const uint SHGFI_SMALLICON = 0x1;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x10;

        // Structure for file information
        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
            public uint dwAttributes;
        }

        #endregion

        #region Private Fields

        private readonly string _cacheDirectory;
        private readonly string[] _executableExtensions = { ".exe", ".com", ".bat", ".cmd", ".msi", ".lnk" };

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the IconExtractionService
        /// Sets up icon cache directory in AppData for persistent storage
        /// </summary>
        public IconExtractionService()
        {
            // Set up cache directory in AppData/ShortcutsApp/Icons
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "ShortcutsApp");
            _cacheDirectory = Path.Combine(appFolder, "Icons");
            
            // Create cache directory if it doesn't exist
            Directory.CreateDirectory(_cacheDirectory);
            
            Debug.WriteLine($"Icon cache directory: {_cacheDirectory}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Extracts and caches an icon for the specified file path
        /// Returns the path to the cached icon file or null if extraction failed
        /// </summary>
        /// <param name="filePath">Path to the file to extract icon from</param>
        /// <param name="iconSize">Desired icon size (16, 32, 48, 64, etc.)</param>
        /// <returns>Path to cached icon file or null if extraction failed</returns>
        public async Task<string?> ExtractAndCacheIconAsync(string filePath, int iconSize = 64)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    Debug.WriteLine($"File not found for icon extraction: {filePath}");
                    return await GetFallbackIconAsync(filePath, iconSize);
                }

                // Generate cache key based on file path and size
                var cacheKey = GenerateCacheKey(filePath, iconSize);
                var cachedIconPath = Path.Combine(_cacheDirectory, $"{cacheKey}.png");

                // Return cached icon if it exists and is newer than the source file
                if (File.Exists(cachedIconPath))
                {
                    var sourceLastWrite = File.GetLastWriteTime(filePath);
                    var cacheLastWrite = File.GetLastWriteTime(cachedIconPath);
                    
                    if (cacheLastWrite >= sourceLastWrite)
                    {
                        Debug.WriteLine($"Using cached icon: {cachedIconPath}");
                        return cachedIconPath;
                    }
                }

                // Extract icon based on file type
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                string? extractedIconPath = null;

                if (extension == ".lnk")
                {
                    // Handle Windows shortcuts (.lnk files)
                    extractedIconPath = await ExtractShortcutIconAsync(filePath, iconSize, cachedIconPath);
                }
                else if (Array.Exists(_executableExtensions, ext => ext == extension))
                {
                    // Handle executable files
                    extractedIconPath = await ExtractExecutableIconAsync(filePath, iconSize, cachedIconPath);
                }
                else
                {
                    // Handle other file types using system file association icons
                    extractedIconPath = await ExtractFileAssociationIconAsync(filePath, iconSize, cachedIconPath);
                }

                // Return extracted icon path or fallback
                return extractedIconPath ?? await GetFallbackIconAsync(filePath, iconSize);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error extracting icon from {filePath}: {ex.Message}");
                return await GetFallbackIconAsync(filePath, iconSize);
            }
        }

        /// <summary>
        /// Gets a fallback icon for unsupported file types or when extraction fails
        /// Creates generic icons based on file extension or type
        /// </summary>
        /// <param name="filePath">Original file path (for extension detection)</param>
        /// <param name="iconSize">Desired icon size</param>
        /// <returns>Path to fallback icon</returns>
        public async Task<string> GetFallbackIconAsync(string filePath, int iconSize = 64)
        {
            try
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                var fallbackType = GetFallbackIconType(extension);
                var cacheKey = $"fallback_{fallbackType}_{iconSize}";
                var fallbackIconPath = Path.Combine(_cacheDirectory, $"{cacheKey}.png");

                // Create fallback icon if it doesn't exist
                if (!File.Exists(fallbackIconPath))
                {
                    await CreateFallbackIconAsync(fallbackType, iconSize, fallbackIconPath);
                }

                return fallbackIconPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating fallback icon: {ex.Message}");
                // Return a default system icon path as last resort
                return CreateDefaultSystemIconPath();
            }
        }

        /// <summary>
        /// Clears the icon cache to free up disk space
        /// Should be called periodically or when user requests cache cleanup
        /// </summary>
        public async Task ClearCacheAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    if (Directory.Exists(_cacheDirectory))
                    {
                        var files = Directory.GetFiles(_cacheDirectory, "*.png");
                        foreach (var file in files)
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Failed to delete cached icon {file}: {ex.Message}");
                            }
                        }
                    }
                });
                
                Debug.WriteLine("Icon cache cleared successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing icon cache: {ex.Message}");
            }
        }

        #endregion

        #region Private Icon Extraction Methods

        /// <summary>
        /// Extracts icon from Windows shortcut (.lnk) files
        /// </summary>
        private async Task<string?> ExtractShortcutIconAsync(string shortcutPath, int iconSize, string outputPath)
        {
            try
            {
                // For .lnk files, we can use SHGetFileInfo to get the target's icon
                return await ExtractFileAssociationIconAsync(shortcutPath, iconSize, outputPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error extracting shortcut icon: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extracts icon from executable files (.exe, .com, etc.)
        /// </summary>
        private async Task<string?> ExtractExecutableIconAsync(string executablePath, int iconSize, string outputPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Use ExtractIconEx to get high-quality icons from executables
                    IntPtr[] largeIcons = new IntPtr[1];
                    IntPtr[] smallIcons = new IntPtr[1];

                    int iconCount = ExtractIconEx(executablePath, 0, largeIcons, smallIcons, 1);
                    
                    if (iconCount > 0)
                    {
                        IntPtr iconHandle = iconSize <= 32 ? smallIcons[0] : largeIcons[0];
                        
                        if (iconHandle != IntPtr.Zero)
                        {
                            // TODO: Convert HICON to PNG and save to outputPath
                            // This requires additional Win32 API calls or imaging libraries
                            // For now, return null to use fallback system
                            
                            // Clean up icon handles
                            if (largeIcons[0] != IntPtr.Zero)
                                DestroyIcon(largeIcons[0]);
                            if (smallIcons[0] != IntPtr.Zero)
                                DestroyIcon(smallIcons[0]);
                        }
                    }

                    return (string?)null; // Placeholder - implement icon conversion
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error extracting executable icon: {ex.Message}");
                    return null;
                }
            });
        }

        /// <summary>
        /// Extracts icon using Windows file association (works for most file types)
        /// </summary>
        private async Task<string?> ExtractFileAssociationIconAsync(string filePath, int iconSize, string outputPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    SHFILEINFO shFileInfo = new SHFILEINFO();
                    uint flags = SHGFI_ICON | (iconSize <= 32 ? SHGFI_SMALLICON : SHGFI_LARGEICON);

                    IntPtr result = SHGetFileInfo(filePath, 0, ref shFileInfo, (uint)Marshal.SizeOf(shFileInfo), flags);
                    
                    if (result != IntPtr.Zero && shFileInfo.hIcon != IntPtr.Zero)
                    {
                        // TODO: Convert HICON to PNG and save to outputPath
                        // This requires additional imaging code
                        
                        // Clean up icon handle
                        DestroyIcon(shFileInfo.hIcon);
                        
                        // Placeholder - implement icon conversion
                        return (string?)null;
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error extracting file association icon: {ex.Message}");
                    return null;
                }
            });
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Generates a unique cache key for a file and icon size
        /// </summary>
        private string GenerateCacheKey(string filePath, int iconSize)
        {
            var fileName = Path.GetFileName(filePath);
            var fileHash = filePath.GetHashCode().ToString("X");
            return $"{fileName}_{fileHash}_{iconSize}";
        }

        /// <summary>
        /// Determines the type of fallback icon needed based on file extension
        /// </summary>
        private string GetFallbackIconType(string extension)
        {
            return extension switch
            {
                ".exe" or ".com" or ".bat" or ".cmd" => "executable",
                ".msi" => "installer",
                ".lnk" => "shortcut",
                ".txt" or ".log" => "text",
                ".doc" or ".docx" => "document",
                ".pdf" => "pdf",
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "image",
                ".mp3" or ".wav" or ".flac" => "audio",
                ".mp4" or ".avi" or ".mkv" or ".mov" => "video",
                ".zip" or ".rar" or ".7z" => "archive",
                _ => "unknown"
            };
        }

        /// <summary>
        /// Creates a simple fallback icon for the specified type
        /// </summary>
        private async Task CreateFallbackIconAsync(string iconType, int iconSize, string outputPath)
        {
            await Task.Run(() =>
            {
                try
                {
                    // TODO: Create actual fallback icons using System.Drawing or similar
                    // For now, this is a placeholder that would create simple colored squares
                    // or use system stock icons
                    
                    Debug.WriteLine($"Creating fallback icon for type: {iconType}, size: {iconSize}");
                    
                    // Placeholder - would implement actual icon creation here
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating fallback icon: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Creates a path to a default system icon as the ultimate fallback
        /// </summary>
        private string CreateDefaultSystemIconPath()
        {
            // Return path to a system default icon
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");
        }

        #endregion
    }
}