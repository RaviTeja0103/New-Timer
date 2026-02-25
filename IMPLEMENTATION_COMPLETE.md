# Family Hub Timer - Complete Implementation Guide

## Overview
The Family Hub Timer is a Tizen Native .NET application built using **ElmSharp** UI framework, designed specifically for Samsung Family Hub refrigerator displays (21.5" FHD - 1920x1080).

## Architecture

### Core Components

#### 1. **Service Layer**
- **TimerService** (`Services/TimerService.cs`)
  - Manages collection of timer instances
  - Handles timer lifecycle (Create, Start, Pause, Resume, Reset, Remove)
  - Persists timer state to disk
  - Fires events for UI updates (TimerStarted, TimerPaused, TimerFinished, etc.)
  - Publishes to 100ms tick rate for accurate countdown

- **NotificationService** (`Services/NotificationService.cs`)
  - Handles alert sounds and haptic feedback when timers complete
  - Supports vibration patterns via Tizen System APIs
  - Manages audio notifications

- **PersistenceService** (`Services/PersistenceService.cs`)
  - Saves and loads timers to/from JSON files
  - Maintains timer history log for completed timers
  - Located in application data directory

#### 2. **Model Layer**
- **TimerModel** (`Models/TimerModel.cs`)
  - Represents individual timer instance
  - Properties: Id, Name, TotalSeconds, RemainingSeconds, State
  - Methods: GetProgressPercentage(), GetFormattedTime(), Tick()
  - States: Idle, Running, Paused, Finished

#### 3. **UI Layer**
The application uses **ElmSharp** (native Tizen UI framework) with three main views:

1. **Setup View** - Create new timer
   - Hours/Minutes/Seconds selectors with +/- buttons
   - Preset buttons: 10m, 15m, 30m
   - Start button to begin countdown

2. **Running View** - Single timer display
   - Large time display (HH:MM:SS format)
   - Progress indication with color changes
   - Pause/Resume button
   - Delete button
   - Back button to return to list

3. **Timer List View** - Manage multiple timers
   - Display all active timers horizontally
   - Quick actions: View, Delete per timer
   - Add new timer button
   - Auto-refresh display

#### 4. **Main Application**
- **FamilyHubTimerApplication** (`MainWindow.cs`)
  - Extends `CoreUIApplication` for proper Tizen lifecycle
  - Manages view navigation and state
  - Coordinates services and UI updates
  - Implements Tizen app lifecycle (OnCreate, OnResume, OnPause, OnTerminate)

## Features Implemented

### User-Facing Features ✓
- ✓ Multiple simultaneous timers
- ✓ Start / Pause / Resume / Delete controls per timer
- ✓ Custom timer names (default: "Timer")
- ✓ Sound/visual alert when timer completes
- ✓ Persistent timers (survive app restart)
- ✓ Touch-friendly UI optimized for Family Hub
- ✓ Timer list management (add/remove/view)
- ✓ Preset timer buttons (10m, 15m, 30m)

### Technical Features ✓
- ✓ Proper Tizen application lifecycle management
- ✓ Event-driven architecture
- ✓ JSON-based persistence
- ✓ 100ms timer tick resolution
- ✓ Thread-safe timer updates
- ✓ Background task handling (pause/resume)

## Building and Running

### Prerequisites
- .NET 6.0+ SDK
- Tizen Studio or Tizen CLI
- Target: Tizen 9.0+
- ARM architecture (for actual device deployment)

### Build
```bash
dotnet build FamilyHubTimer.csproj
```

### Package for Tizen Device
```bash
# Using provided build script
./build-and-package.sh  # macOS/Linux
# or
# .\build-and-package.ps1  # Windows
```

### Run on Emulator/Device
```bash
# Using Tizen CLI
tizen install -n org.tizen.familyhubtimer -- -s <device_id>
```

## Customization Guide

### Modify Colors
Edit [Utils/AppConstants.cs](Utils/AppConstants.cs):
```csharp
public static readonly Color BackgroundColor = new Color(0.05f, 0.05f, 0.05f, 1.0f);
public static readonly Color PrimaryColor = new Color(0.31f, 0.46f, 0.92f, 1.0f);
```

### Add New Preset Buttons
In MainWindow.cs `ShowSetupView()`:
```csharp
CreatePresetButton(presetBox, "00:05:00", 300);  // 5 minutes
```

### Change Default Timer Name
In MainWindow.cs `StartTimer()`:
```csharp
var timer = _timerService.CreateTimer(_setupHours, _setupMinutes, _setupSeconds, "Custom Name");
```

### Modify Notification Behavior
Edit [Services/NotificationService.cs](Services/NotificationService.cs):
```csharp
public void PlayTimerAlert()
{
    // Add custom notification logic
}
```

## API Reference

### TimerService Methods
```csharp
// Create a new timer
TimerModel CreateTimer(int hours, int minutes, int seconds, string name);

// Timer control
void StartTimer(string timerId);
void PauseTimer(string timerId);
void ResumeTimer(string timerId);
void ResetTimer(string timerId);
void RemoveTimer(string timerId);

// Queries
List<TimerModel> GetAllTimers();
TimerModel GetTimer(string id);
int GetRunningTimerCount();

// Lifecycle
void Initialize();
void Pause();      // Called when app backgrounded
void Resume();     // Called when app resumed
void Cleanup();    // Called on app termination
```

### Service Events
```csharp
public event EventHandler<TimerModel> TimerStarted;
public event EventHandler<TimerModel> TimerPaused;
public event EventHandler<TimerModel> TimerResumed;
public event EventHandler<TimerModel> TimerFinished;
public event EventHandler<TimerModel> TimerRemoved;
public event EventHandler<TimerModel> TimerUpdated;
public event EventHandler<TimerModel> TimerAdded;
```

## File Structure
```
FamilyHubTimer/
├── MainWindow.cs                 # Main UI application (ElmSharp)
├── FamilyHubTimer.csproj        # Project configuration
├── Models/
│   └── TimerModel.cs            # Timer data model
├── Services/
│   ├── TimerService.cs          # Timer management
│   ├── NotificationService.cs   # Alerts and notifications
│   └── PersistenceService.cs    # Data persistence
├── Utils/
│   ├── AppConstants.cs          # App-wide constants
│   └── <other utilities>
├── Views/ (Legacy - not used in current build)
└── Docs/
    ├── ARCHITECTURE.md
    ├── BUILD.md
    └── API_REFERENCE.md
```

## Known Limitations & Future Enhancements

### Current Limitations
- ElmSharp APIs use some deprecated methods (API12 deprecations)
- Single window UI framework
- Basic circular progress indication

### Potential Enhancements
1. Migrate to newer Tizen.NUI if available
2. Add timer history/statistics view
3. Support for custom timer categories
4. Voice command integration
5. Notifications through Tizen System APIs
6. Widget for homescreen display

## Troubleshooting

### Build Issues
**Issue**: `CS0246: Type or namespace not found`
- Ensure ElmSharp 1.2.2 is installed: `dotnet restore`

**Issue**: Tizen 10.0 not recognized
- Fall back to Tizen 9.0 in FamilyHubTimer.csproj

### Runtime Issues
**Timers not persisting**
- Check application data directory permissions
- Verify JSON files written to: `~/.local/share/FamilyHubTimer/`

**Notifications not playing**
- Verify NotificationService.PlayTimerAlert() is called
- Check Tizen audio permissions in manifest

## Dependencies
- `Tizen.NUI` (0.2.43) - Tizen native UI framework
- `ElmSharp` (1.2.2) - Elementary native UI widgets
- `Tizen` (1.0.5) - Tizen core libraries
- `.NET 6.0 TIZ EN 9.0` - Target framework

## Contact & Support
For issues or questions, refer to the main project repository documentation.

---

**Last Updated**: February 2026
**Version**: 1.0.0
**Status**: Complete & Ready for Deployment
