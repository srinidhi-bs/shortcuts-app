# TODO List - Shortcuts App

## Completed Tasks ✅

### 1. Project Setup and Foundation
- [x] Initialize WinUI 3 project structure
- [x] Add required NuGet packages (System.Text.Json, WinUIEx)
- [x] Configure project for .NET 8 target framework
- [x] Set up basic application architecture

### 2. Settings System
- [x] Create AppSettings model with JSON serialization
- [x] Implement SettingsService for persistent storage
- [x] Configure settings storage in AppData folder
- [x] Add support for hotkey, shortcuts, and appearance settings

### 3. Main Settings Window
- [x] Design settings UI with modern Fluent Design
- [x] Implement hotkey configuration interface
- [x] Add shortcuts management (add/remove functionality)
- [x] Create appearance customization controls (grid columns, icon size, opacity)
- [x] Integrate file picker for adding shortcuts
- [x] Connect UI to settings service with auto-save

### 4. System Tray Integration
- [x] Create SystemTrayService class with Win32 APIs
- [x] Implement system tray icon using Shell_NotifyIcon
- [x] Add Show/Hide window functionality via minimize to tray
- [x] Configure tray icon and tooltip
- [x] Integrate SystemTrayService with App.xaml.cs
- [x] Connect minimize to tray button functionality

### 5. Global Hotkey Registration
- [x] Create HotkeyService using Win32 APIs
- [x] Register/unregister global hotkeys dynamically
- [x] Handle hotkey activation events through custom window procedure
- [x] Integrate HotkeyService with App.xaml.cs and service container
- [x] Implement Win32 message handling in MainWindow for WM_HOTKEY
- [x] Add comprehensive error handling and logging
- [x] Support configurable modifier keys and key combinations

## In Progress 🔄

## Pending Tasks ⏳

### 6. Enhanced Hotkey System
- [ ] Improve hotkey capture UI for better user experience
- [ ] Add validation for hotkey conflicts
- [ ] Implement visual feedback during hotkey selection
- [ ] Add user-friendly hotkey conflict resolution

### 7. Popup Launcher Window
- [ ] Create PopupWindow with borderless, always-on-top styling
- [ ] Implement popup positioning (center screen or cursor)
- [ ] Add smooth show/hide animations
- [ ] Configure popup opacity and backdrop effects

### 8. Icon Grid Layout
- [ ] Design responsive grid layout for shortcuts
- [ ] Implement keyboard navigation (arrow keys)
- [ ] Add visual selection indicators
- [ ] Support dynamic grid sizing based on user preferences

### 9. Enhanced Shortcuts Management
- [ ] Improve shortcut adding workflow
- [ ] Add drag-and-drop support for shortcuts
- [ ] Implement shortcut reordering
- [ ] Add shortcut editing capabilities (rename, change icon)

### 10. Application Launching
- [ ] Implement secure process launching
- [ ] Add support for different file types (.exe, .lnk, documents)
- [ ] Handle launch errors gracefully
- [ ] Track recently used shortcuts

### 11. Icon Extraction and Display
- [ ] Create IconExtractionService
- [ ] Extract icons from executables and shortcuts
- [ ] Cache icons for performance
- [ ] Provide fallback icons for unsupported file types
- [ ] Support different icon sizes

## Technical Debt and Improvements

### Code Quality
- [ ] Add comprehensive error handling
- [ ] Implement proper logging system
- [ ] Add unit tests for services
- [ ] Improve null reference handling (address compiler warnings)

### Performance Optimizations
- [ ] Implement lazy loading for shortcuts
- [ ] Optimize icon loading and caching
- [ ] Minimize popup show/hide latency
- [ ] Background loading of settings and shortcuts

### User Experience Enhancements
- [ ] Add keyboard shortcuts for settings window
- [ ] Implement search/filter functionality in popup
- [ ] Add recent items or frequently used tracking
- [ ] Provide visual themes (light/dark mode support)

### Accessibility
- [ ] Add screen reader support
- [ ] Implement high contrast mode support
- [ ] Ensure full keyboard navigation
- [ ] Add tooltips and help text

## Next Steps
1. ✅ Complete system tray integration
2. ✅ Implement global hotkey registration (HotkeyService)
3. Create popup launcher window with keyboard navigation
4. Implement icon extraction service
5. Test end-to-end functionality
6. Polish user experience and handle edge cases

## Git Workflow
- Commit after each major task completion
- Push to GitHub with descriptive commit messages
- Update this TODO.md before each commit
- Tag releases for major milestones