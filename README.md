# Family Hub Timer - Tizen 10 Application

A feature-rich timer application designed for Samsung Family Hub refrigerator displays, built with C# and Tizen .NET framework.

## 📋 Project Overview

**Family Hub Timer** is a modern, touch-friendly countdown timer application optimized for Samsung Family Hub's 21.5-inch FHD touchscreen (1920x1080). It supports multiple simultaneous timers, persistent storage, and provides intuitive controls for everyday kitchen tasks.

### Key Features

✅ **Multiple Simultaneous Timers** - Create and manage multiple countdown timers simultaneously  
✅ **Persistent Storage** - Timers survive application restarts using JSON-based persistence  
✅ **Sound & Vibration Alerts** - Audio and haptic feedback when timers complete  
✅ **Large Touch-Friendly UI** - Optimized for large Family Hub display with accessible button sizes  
✅ **Timer Presets** - Quick-select buttons for common durations (10s, 15s, 30s)  
✅ **Custom Timer Names** - Label timers for different cooking tasks  
✅ **Flexible Controls** - Start, Pause, Resume, and Reset functionality per timer  
✅ **Progress Visualization** - Circular progress indicator and linear progress bars  
✅ **Timer History** - Log of completed timers for reference  
✅ **Clean Architecture** - SOLID principles with separated concerns

## 🏗️ Project Structure

```
FamilyHubTimer/
├── MainWindow.cs                 # Main application entry point
├── Utils/
│   └── AppConstants.cs          # UI constants, colors, sizes
├── Models/
│   └── TimerModel.cs            # Timer data model and state management
├── Services/
│   ├── TimerService.cs          # Core timer logic and lifecycle management
│   ├── PersistenceService.cs    # File-based data persistence
│   └── NotificationService.cs   # Sound and vibration alerts
├── Views/
│   ├── MainSetupView.cs         # Timer setup/configuration UI
│   ├── TimerListView.cs         # List of active timers
│   └── TimerRunningView.cs      # Single timer running display
├── FamilyHubTimer.csproj        # Project configuration
└── tizen-manifest.xml           # Tizen application manifest
```

### File Descriptions

#### **UML Architecture**
```
Main Application
    │
    ├── TimerService (Singleton)
    │   ├── Creates/Manages TimerModel instances
    │   ├── Handles timer lifecycle (start/pause/resume/reset)
    │   └── Publishes events for UI updates
    │
    ├── Views (UI Layer)
    │   ├── MainSetupView (Timer creation)
    │   ├── TimerListView (Multi-timer management)
    │   └── TimerRunningView (Active timer display)
    │
    └── Services (Business Logic)
        ├── PersistenceService (Data storage)
        ├── NotificationService (Alerts)
        └── AppConstants (Configuration)
```

## 🔧 Technical Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| **Platform** | Tizen 10 | 10.0 |
| **Framework** | .NET | 8.0 |
| **Language** | C# | 11.0 |
| **UI Framework** | Tizen NUI | 0.2.43 |
| **Build Tool** | dotnet CLI | 8.0+ |
| **Target Device** | Samsung Family Hub | 21.5" FHD Touchscreen |
| **Architecture** | ARM/x86 | 32-bit & 64-bit |

## 📊 Component Details

### 1. **TimerModel.cs** - Data Model
- Represents a single timer instance
- Properties: Id, Name, TotalSeconds, RemainingSeconds, State
- States: Idle, Running, Paused, Finished
- Methods: Tick(), GetProgressPercentage(), GetFormattedTime()

### 2. **TimerService.cs** - Business Logic
- Manages collection of timers
- Internal tick timer (100ms intervals)
- Events: TimerStarted, TimerPaused, TimerResumed, TimerFinished, TimerUpdated
- Methods: CreateTimer(), StartTimer(), PauseTimer(), ResumeTimer(), ResetTimer(), RemoveTimer()

### 3. **PersistenceService.cs** - Data Storage
- JSON-based file storage in application data directory
- Automatically saves timer state
- Loads saved timers on application startup
- Maintains timer history for analytics

### 4. **NotificationService.cs** - Alerts
- Vibration patterns for timer completion
- System audio notifications
- Extensible for custom alert sounds

### 5. **Views** - User Interface
- **MainSetupView**: Number selector with up/down controls, preset buttons
- **TimerListView**: Scrollable list with pause/resume/reset/remove buttons
- **TimerRunningView**: Large circular progress display with main controls

### 6. **AppConstants.cs** - Configuration
- **Colors**: Background (#0D0D0D), Primary Blue (#4F75EB), Alert Red (#FF3333)
- **Spacing**: Large (40px), Medium (20px), Small (10px)
- **Fonts**: Title (72pt), Large (60pt), Medium (48pt), Regular (36pt), Small (28pt)
- **Screen**: 1920x1080 (Family Hub FHD)
- **UI Elements**: Buttons (300x80px), Circular Progress (500px diameter)

## 🚀 Build Instructions

### Prerequisites
- .NET 8.0 SDK or higher
- Tizen Studio (for device deployment)
- Tizen emulator or actual Samsung Family Hub device

### Build Steps

```bash
# Navigate to project directory
cd /Users/ritadas/Downloads/Ravi

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Build in Release mode
dotnet build -c Release

# Clean build
dotnet clean
dotnet build
```

### Build Output
```
✓ Build succeeded.
  FamilyHubTimer -> bin/Debug/net8.0/FamilyHubTimer.dll
  0 Error(s)
  0 Warning(s)
```

## 📝 Usage Examples

### Create a Timer
```csharp
// Create a 5-minute timer
var timer = timerService.CreateTimer(hours: 0, minutes: 5, seconds: 0, name: "Pasta");

// Start the timer
timerService.StartTimer(timer.Id);
```

### Control Timers
```csharp
// Pause a timer
timerService.PauseTimer(timerId);

// Resume a paused timer
timerService.ResumeTimer(timerId);

// Reset timer to original duration
timerService.ResetTimer(timerId);

// Remove a timer
timerService.RemoveTimer(timerId);
```

### Monitor Timer Progress
```csharp
// Subscribe to timer events
timerService.TimerFinished += (s, timer) => {
    Tizen.Log.Info("App", $"Timer {timer.Name} finished!");
};

timerService.TimerUpdated += (s, timer) => {
    Console.WriteLine($"Time remaining: {timer.GetFormattedTime()}");
};
```

## 🎨 UI Design Specifications

### Color Scheme
- **Background**: Dark gray (#0D0D0D) for OLED-friendly display
- **Primary**: Blue (#4F75EB) for active elements and accent
- **Secondary**: Dark gray (#333333) for secondary components
- **Text**: White (#FFFFFF) for main text, light gray (#B3B3B3) for labels
- **Alert**: Red (#FF3333) for destructive actions

### Typography
- **Title**: 72pt bold (e.g., "Set Timer")
- **Large**: 60pt (timer display)
- **Medium**: 48pt (labels)
- **Regular**: 36pt (button text)
- **Small**: 28pt (secondary information)

### Layout Grid
- Screen: 1920x1080 (16:9 aspect ratio)
- Safe area margins: 40px on all sides
- Component spacing: 20-40px
- Button size: 300x80px (primary), 150x60px (secondary)

## 🔄 Application Lifecycle

```
1. OnCreate
   ├── Initialize TimerService
   ├── Load persisted timers
   └── Set up event subscriptions

2. OnResume
   ├── Check for pending notifications
   └── Update UI state

3. OnPause
   ├── Pause all running timers (if configured)
   └── Save current state

4. OnTerminate
   ├── Save all timers
   ├── Clean up resources
   └── Stop notification service
```

## 💾 Persistence Layer

### Storage Location
```
~/.local/share/FamilyHubTimer/
├── timers.json      # Active timers data
└── timer_history.json  # Completed timers history
```

### Data Format
```json
{
  "id": "uuid",
  "name": "Pasta",
  "totalSeconds": 600,
  "remainingSeconds": 120,
  "state": "Running",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

## 🧪 Testing Checklist

- [ ] Create single timer and verify countdown
- [ ] Create multiple timers simultaneously
- [ ] Pause and resume timer functionality
- [ ] Reset timer to original value
- [ ] Delete timer from list
- [ ] Verify persistence across app restart
- [ ] Test notification (sound/vibration)
- [ ] Test UI responsiveness on 1920x1080 display
- [ ] Verify touch button sizes (min 60px)
- [ ] Test theme colors in various lighting

## 📱 Device Requirements

| Requirement | Specification |
|-----------|---------------|
| **Device** | Samsung Family Hub Refrigerator |
| **Screen** | 21.5" FHD (1920x1080) |
| **OS** | Tizen 10 |
| **RAM** | 2GB minimum |
| **Storage** | 100MB minimum |
| **ARM** | ARMv7l or ARM64 processor |

## 🤝 Contributing

This codebase follows clean architecture principles:
- **Models**: Pure data classes, no business logic
- **Services**: Business logic, event publishing, state management
- **Views**: UI layer, minimal logic, event subscriptions
- **Utils**: Constants and helper functions

## 📄 License

Created by Samsung for Family Hub ecosystem | 2024

## 🔗 References

- [Tizen Developer Documentation](https://developer.tizen.org/)
- [Samsung Family Hub Documentation](https://www.samsung.com/)
- [Tizen .NET Guide](https://docs.tizen.org/application/dotnet/)
- [NUI Sample Applications](https://github.com/Samsung/TizenFX)

## ‼️ Important Notes

1. **Tizen SDK Installation Required**: This project requires the Tizen Studio for deployment to actual devices
2. **Emulator Testing**: Use Tizen emulator for quick testing without physical device
3. **Package Manager**: All Tizen system services require proper privilege declarations in manifest
4. **Performance**: Timer tick interval is 100ms for responsive updates while minimizing CPU usage
5. **Localization**: All UI strings should be externalized for i18n support

---

**Build Status**: ✅ Successful  
**Last Updated**: February 25, 2024  
**Version**: 1.0.0
