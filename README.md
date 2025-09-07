# Shortcuts App

A modern Windows desktop application that provides a global keyboard shortcut to launch a beautiful visual popup with customizable application shortcuts. Similar to Alfred for macOS or PowerToys Run, but with a focus on visual icons and intuitive keyboard navigation.

## ✨ Features

### 🎯 Core Functionality
- **Global Hotkey**: Configurable keyboard shortcut (default: Ctrl+Space) to instantly open the launcher
- **Visual Launcher**: Beautiful popup window with icon grid displaying your shortcuts
- **Keyboard Navigation**: Navigate with arrow keys, launch with Enter, close with Escape
- **System Tray Integration**: Minimizes to system tray with right-click context menu

### ⚙️ Customization
- **Hotkey Configuration**: Set your preferred key combination
- **Shortcut Management**: Add/remove applications and files with file picker
- **Visual Customization**: Adjust grid columns (3-10), icon size (32-128px), and popup opacity
- **Persistent Settings**: All preferences saved automatically in JSON format

### 🎨 Modern Design
- **Fluent Design**: Native Windows 11 appearance with acrylic effects
- **Smooth Animations**: Fade-in/fade-out with scale effects for polished UX  
- **Responsive Layout**: Grid adapts to your preferred column count
- **Always on Top**: Popup stays visible over other applications

## 🚀 Getting Started

### Prerequisites
- Windows 10 version 1809 (build 17763) or later
- Windows 11 recommended for best visual experience
- .NET 8 Runtime

### Installation
1. Download the latest release from the [Releases](https://github.com/srinidhi-bs/shortcuts-app/releases) page
2. Extract and run `ShortcutsApp.exe`
3. The app will start minimized to system tray

### First Use
1. Right-click the system tray icon and select "Show Settings"
2. Configure your preferred global hotkey
3. Add shortcuts using the "Add Shortcut" button
4. Test your setup with the "Test Popup" button
5. Press your configured hotkey to open the launcher!

## 🛠️ Technology Stack

**Built with C# and WinUI 3** for optimal Windows integration:

- **WinUI 3**: Microsoft's modern UI framework for native Windows apps
- **.NET 8**: Latest .NET runtime for performance and features  
- **Win32 APIs**: Direct system integration for hotkeys and system tray
- **JSON Storage**: Lightweight settings persistence

### Why WinUI 3?
- ✅ Native Windows 11 look and feel
- ✅ Excellent performance and responsiveness
- ✅ Built-in Fluent Design support
- ✅ Direct access to Windows APIs
- ✅ Future-proof Microsoft technology

## 📁 Project Structure

```
ShortcutsApp/
├── Models/
│   ├── AppSettings.cs          # Configuration data models
│   ├── ShortcutItem.cs         # Shortcut data structure
│   └── ShortcutDisplayItem.cs  # UI display model
├── Services/
│   ├── SettingsService.cs      # JSON settings management
│   ├── SystemTrayService.cs    # System tray functionality
│   ├── HotkeyService.cs        # Global hotkey registration
│   ├── IconExtractionService.cs # Icon extraction and caching
│   ├── LaunchingService.cs     # Application and file launching
│   └── UsageTrackingService.cs # Usage analytics and tracking
├── Views/
│   ├── MainPage.xaml           # Settings window UI
│   ├── MainPage.xaml.cs        # Settings window logic
│   ├── PopupWindow.xaml        # Launcher popup UI
│   └── PopupWindow.xaml.cs     # Launcher popup logic
└── App.xaml.cs                 # Application entry point and DI container
```

## 🎮 Usage

### Keyboard Shortcuts
- **Global Hotkey**: Open/close launcher (configurable, default: Ctrl+Space)
- **Arrow Keys**: Navigate between shortcuts in the popup
- **Home/End**: Jump to first/last shortcut
- **Enter**: Launch selected shortcut and close popup
- **Escape**: Close popup window without launching

### Shortcut Management
- **Add Shortcuts**: Use file picker to add applications and files
- **Edit Shortcuts**: Double-click or use edit button to rename shortcuts
- **Reorder Shortcuts**: Use up/down arrow buttons or drag-and-drop
- **Remove Shortcuts**: Click X button with confirmation dialog
- **Drag & Drop**: Drop files directly onto the shortcuts list

### System Tray Menu
- **Show Settings**: Open configuration window
- **Test Popup**: Quick test of popup functionality
- **Exit**: Close application completely

## 🔧 Development

### Building from Source
```bash
git clone https://github.com/srinidhi-bs/shortcuts-app.git
cd shortcuts-app
dotnet build
dotnet run
```

### Development Environment
- Visual Studio 2022 or Visual Studio Code
- Windows 11 SDK
- .NET 8 SDK

### Architecture Highlights
- **MVVM Pattern**: Clean separation of UI and business logic
- **Async/Await**: Non-blocking file operations and animations
- **Dependency Injection**: Service-based architecture with proper lifetime management
- **Win32 Interop**: Native Windows API integration for system-level features
- **Error Recovery**: Advanced exception handling with COM error recovery
- **Window Lifecycle Management**: Automatic window recreation for consistent functionality
- **Thread Safety**: Proper UI thread marshaling with DispatcherQueue
- **Resource Management**: Proper disposal patterns for system resources

## 🎯 Roadmap

### ✅ Core Features Complete
- [x] Project setup and WinUI 3 integration
- [x] Settings system with JSON persistence
- [x] Modern settings UI with Fluent Design
- [x] System tray integration with Win32 APIs
- [x] Global hotkey registration and handling
- [x] Popup launcher window with smooth animations
- [x] Icon grid layout with keyboard navigation
- [x] File and application launching system
- [x] Icon extraction and caching service
- [x] Enhanced shortcut management with editing
- [x] Drag-and-drop shortcut support
- [x] Usage tracking and analytics
- [x] Window lifecycle stability and error recovery

### ✅ Recently Completed
- [x] Enhanced hotkey conflict detection and resolution
- [x] Icon extraction from executables with caching
- [x] Comprehensive application launching system
- [x] Shortcut editing and management improvements
- [x] Drag-and-drop support for shortcuts
- [x] Shortcut reordering with up/down controls
- [x] Usage tracking and analytics
- [x] Window lifecycle management and stability fixes
- [x] Thread-safe popup handling and COM exception recovery

### 📋 Future Enhancements
- [ ] Search/filter functionality in popup launcher
- [ ] Recently used shortcuts prioritization
- [ ] Custom themes and visual customization
- [ ] Accessibility improvements (screen reader support)
- [ ] Backup and restore settings functionality
- [ ] Plugin system for extended functionality

## 🤝 Contributing

This is a learning project, but contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Code Style
- Extensive commenting for educational purposes
- Follow Microsoft C# coding conventions
- Include error handling and validation
- Write clear, descriptive commit messages

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👨‍💻 Author

**Srinidhi BS**
- GitHub: [@srinidhi-bs](https://github.com/srinidhi-bs)
- Email: mailsrinidhibs@gmail.com

*Built with learning in mind - extensive comments throughout the codebase for educational purposes.*

### Status: ✅ **Production Ready**
The application is fully functional and stable. All core features have been implemented and tested, including comprehensive bug fixes for window lifecycle management and threading issues.

---

⭐ **Like this project? Give it a star to show your support!**