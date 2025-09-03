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
│   └── AppSettings.cs          # Configuration data models
├── Services/
│   ├── SettingsService.cs      # JSON settings management
│   ├── SystemTrayService.cs    # System tray functionality
│   └── HotkeyService.cs        # Global hotkey registration
├── Views/
│   ├── MainPage.xaml           # Settings window UI
│   ├── MainPage.xaml.cs        # Settings window logic
│   ├── PopupWindow.xaml        # Launcher popup UI
│   └── PopupWindow.xaml.cs     # Launcher popup logic
└── App.xaml.cs                 # Application entry point
```

## 🎮 Usage

### Keyboard Shortcuts
- **Global Hotkey**: Open/close launcher (configurable, default: Ctrl+Space)
- **Arrow Keys**: Navigate between shortcuts in the popup
- **Enter**: Launch selected shortcut
- **Escape**: Close popup window

### System Tray Menu
- **Show Settings**: Open configuration window
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
- **Dependency Injection**: Service-based architecture
- **Win32 Interop**: Native Windows API integration
- **Error Handling**: Comprehensive exception handling throughout

## 🎯 Roadmap

### ✅ Completed
- [x] Project setup and WinUI 3 integration
- [x] Settings system with JSON persistence
- [x] Modern settings UI with Fluent Design
- [x] System tray integration with Win32 APIs
- [x] Global hotkey registration and handling
- [x] Popup launcher window with animations

### 🔄 In Progress
- [ ] Enhanced hotkey conflict detection
- [ ] Icon extraction from executables
- [ ] Application launching improvements

### 📋 Planned Features
- [ ] Icon extraction and caching service
- [ ] Drag-and-drop shortcut management
- [ ] Search/filter functionality in popup
- [ ] Recently used shortcuts tracking
- [ ] Themes and customization options
- [ ] Accessibility improvements

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

---

⭐ **Like this project? Give it a star to show your support!**