# Tizen NUI Implementation Guide - Family Hub Timer

## ✓ MIGRATION COMPLETE

Your Family Hub Timer application has been successfully migrated from **ElmSharp** to **Tizen.NUI C#** framework with full support for 1080x1920 display optimization.

---

## Project Structure Overview

```
New-Timer/
├── MainWindow.cs                    ← MAIN IMPLEMENTATION (NUI-based)
├── FamilyHubTimer.csproj            ← Updated with Tizen.NUI packages
├── tizen-manifest.xml               ← App manifest (unchanged)
├── Models/
│   └── TimerModel.cs                ← Timer data model
├── Services/
│   ├── TimerService.cs              ← Timer lifecycle management
│   ├── NotificationService.cs       ← Alert sounds/vibration
│   └── PersistenceService.cs        ← Save/load timers
├── Utils/
│   └── AppConstants.cs              ← App configuration
└── Views/                           ← Legacy (can be removed)
    ├── MainSetupView.cs
    ├── TimerListView.cs
    └── TimerRunningView.cs
```

---

## Key Implementation Features

### 1. **Window Configuration**
```csharp
// Window size optimized for Family Hub
_mainWindow.WindowSize = new Size2D(1080, 1920);
_mainWindow.BackgroundColor = ConvertHexToColor("#0d0d0d");  // Dark theme
```

### 2. **Three-Screen UI Architecture**

#### Screen 1: Setup (Initial/Add Timer)
- Time input with +/- controls
- Quick presets (10s, 5m, 15m, 30m)
- Large START button
- Accessible via "Add Timer" or on startup

#### Screen 2: Running (Active Timer)
- Large time display (140px font)
- Progress bar visualization
- State indicator
- PAUSE/RESUME, RESET, DELETE buttons
- Back to list navigation

#### Screen 3: List (All Timers)
- Scrollable list of active timers
- Quick VIEW/DELETE actions
- Add new timer button
- Empty state message

### 3. **Touch-Optimized UI**
```
Button Heights:    140px (minimum for touch)
Button Widths:     Match parent or 140px+ minimum
Font Sizes:        32px-140px depending on importance
Touch Padding:     40px margins on sides
Vertical Spacing:  20px-60px between elements
```

### 4. **Color Scheme**
| Component | Color | Usage |
|-----------|-------|-------|
| Background | #0d0d0d | Main background |
| Secondary | #1a1a1a | List items, secondary areas |
| Accent | #00a9ff | Key info, highlights |
| Text | #ffffff | Primary text |

---

## UI Screens in Detail

### Setup Screen Flow
```
[SET TIMER Header]
    ↓
[Hours] [Minutes] [Seconds] Editors
    +/- buttons for each
    ↓
[Quick Set Buttons] 10s | 5m | 15m | 30m
    ↓
[START TIMER Button]
    ↓
→ Transitions to Running Screen
```

### Running Screen Flow
```
[Timer Name]
    ↓
[HH:MM:SS Large Display]
    ↓
[Progress Bar]
    ↓
[State Indicator] ● RUNNING / ⏸ PAUSED / ✓ FINISHED / ○ STOPPED
    ↓
[PAUSE] [RESET] [DELETE] [BACK TO LIST] Buttons
    ↓
→ Updates every 500ms
→ On completion: Alert + State change to FINISHED
```

### List Screen Flow
```
[ACTIVE TIMERS Header]
    ↓
[Empty State] OR [Timer Items List]
    Each item shows:
    - Name (40px)
    - Time (50px, blue accent)
    - Status (32px)
    - [VIEW] [DELETE] buttons
    ↓
[+ ADD NEW TIMER Button]
```

---

## Service Integration

### TimerService
```csharp
// Create a new timer
var timer = _timerService.CreateTimer(hours, minutes, seconds, "MyTimer");

// Control timer
_timerService.StartTimer(timerId);
_timerService.PauseTimer(timerId);
_timerService.ResumeTimer(timerId);
_timerService.ResetTimer(timerId);
_timerService.RemoveTimer(timerId);

// Retrieve timers
var allTimers = _timerService.GetAllTimers();
var specificTimer = _timerService.GetTimer(timerId);

// Events
_timerService.TimerFinished += (s, timer) => { /* Handle ... */ };
```

### NotificationService
```csharp
// Plays alert when timer finishes
_notificationService.PlayTimerAlert();

// Called automatically when TimerState == Finished
```

### PersistenceService
```csharp
// Automatically saves timers after changes
// Automatically loads timers on app startup
// Uses JSON format in application data directory
```

---

## Real-Time Updates

The "Running" screen updates the timer display every **500ms**:

```csharp
_displayUpdateTimer = new System.Threading.Timer(
    (s) => UpdateTimerDisplay(),
    null,
    TimeSpan.FromMilliseconds(500),
    TimeSpan.FromMilliseconds(500)
);
```

This ensures the display always shows current remaining time.

---

## Application Lifecycle

### OnCreate()
- Initialize services (TimerService, NotificationService)
- Load persisted timers
- Create main window (1080x1920)
- Setup event subscriptions
- Show appropriate screen (List if timers exist, else Setup)

### OnPause()
- Pause timer service
- Stop display updates
- Application goes to background

### OnResume()
- Resume timer service
- Restart display updates if on Running screen
- Application comes to foreground

### OnTerminate()
- Stop all updates
- Cleanup services
- Save state via PersistenceService

---

## Building the Application

### Prerequisites
1. **Tizen SDK 10.0** installed
2. **.NET 6.0+** SDK
3. **Visual Studio Code** or **Visual Studio**
4. **Tizen device certificate** for signing

### Build Steps

#### Option 1: Command Line
```bash
cd "New-Timer"
dotnet build FamilyHubTimer.csproj
```

#### Option 2: Package Creation
```powershell
.\build-and-package.ps1      # Windows PowerShell
```

Or:
```bash
./build-and-package.sh        # Linux/Mac
```

### Build Output
- `bin/Release/net6.0-tizen10.0/org.tizen.familyhubtimer-1.0.0.tpk` - Device package
- Ready to deploy to Family Hub display

---

## Testing Checklist

### Functionality Tests
- [ ] App launches on Family Hub
- [ ] Setup screen: Time input works (_setupHours/_setupMinutes/_setupSeconds)
- [ ] Quick preset buttons: Set correct times
- [ ] START button: Creates timer and transitions to Running screen
- [ ] Running screen: Time counts down properly
- [ ] PAUSE button: Pauses timer (button text changes to RESUME)
- [ ] RESUME button: Resumes from paused state
- [ ] RESET button: Resets timer to original time
- [ ] DELETE button: Removes timer and returns to list
- [ ] Back to List: Returns to list view
- [ ] List view: Shows all active timers
- [ ] Delete from list: Removes timer without opening it
- [ ] Add from list: Opens setup screen
- [ ] Alert: Plays sound when timer completes
- [ ] Persistence: Timers survive app restart

### UI/UX Tests
- [ ] All text is readable (dark background, white text)
- [ ] All buttons are tap-able (140px+ height)
- [ ] Layout doesn't overflow (1080x1920 bounds)
- [ ] Colors match spec (dark #0d0d0d, accent #00a9ff)
- [ ] Font sizes are appropriate
- [ ] Touch response is immediate
- [ ] No overlapping elements
- [ ] List scrolls properly if many timers
- [ ] Progress bar updates smoothly

### Performance Tests
- [ ] App doesn't lag during countdown
- [ ] Display updates every ~500ms (not more frequent)
- [ ] Battery usage is acceptable
- [ ] No memory leaks after extended use
- [ ] App responsive to user input

---

## API Reference Links

- **Tizen.NUI:** https://docs.tizen.org/application/dotnet/guides/user-interface/nui/overview/
- **Tizen System APIs:** https://docs.tizen.org/application/tizen-studio/native-tools/tizen-apis/
- **NUI Components:** https://docs.tizen.org/application/dotnet/guides/user-interface/nui/components

---

## Customization Guide

### Changing Colors
Edit these constants in MainWindow.cs:
```csharp
private const string DARK_BG = "#0d0d0d";       // Main background
private const string ACCENT_COLOR = "#00a9ff";  // Highlights
private const string TEXT_COLOR = "#ffffff";    // Text
```

### Adjusting Font Sizes
Font sizes are defined as pixel sizes in each CreateTextLabel/CreateButton call:
- Title: 60px
- Headers: 50px
- Large display: 140px
- Buttons: 70px
- Labels: 32-40px

### Changing Button Heights
Modify HeightSpecification in CreateButton():
```csharp
HeightSpecification = 140,  // Change this value
```

### Adjusting Spacing
Modify margins and paddings:
```csharp
Margin = new Extents(left, right, top, bottom);
Layout = new LinearLayout { CellPadding = new Size2D(horizontal, vertical) };
```

### Modifying Preset Times
In ShowSetupView():
```csharp
CreatePresetButton(presetButtonContainer, "10s", 10);    // seconds
CreatePresetButton(presetButtonContainer, "5m", 300);    // 5*60 seconds
CreatePresetButton(presetButtonContainer, "15m", 900);   // 15*60 seconds
CreatePresetButton(presetButtonContainer, "30m", 1800);  // 30*60 seconds
```

---

## Troubleshooting

### Build Issues

**Error:** "The target platform identifier tizen was not recognized"
- **Cause:** Tizen SDK not properly installed
- **Solution:** Install Tizen SDK 10.0 via Visual Studio or command line

**Error:** "NUIApplication not found"
- **Cause:** Tizen.NUI NuGet package not restored
- **Solution:** Run `dotnet restore` before building

**Error:** "Package already exists"
- **Cause:** Previous build artifacts
- **Solution:** Run `dotnet clean` before rebuilding

### Runtime Issues

**Timer not counting:** Check TimerService is initialized before use
**No sound on alert:** Verify NotificationService permissions in manifest
**Timers not persisting:** Check data directory creation in PersistenceService
**UI not updating:** Verify MainThread is used for UI updates from background tasks

### Display Issues

**Text too small:** Image text is 1080x1920 - adjust PixelSize values
**Buttons not responsive:** Increase HeightSpecification to 140px minimum
**Layout overflowing:** Check total width/height of elements
**Colors appear wrong:** Verify hex values and ConvertHexToColor() conversion

---

## Performance Optimization Tips

1. **Display Updates:** Currently 500ms - increase if battery usage high
2. **Scrolling:** ScrollView automatically optimizes list rendering
3. **Memory:** Services clean up properly in OnTerminate()
4. **Battery:** Timers pause when app backgrounded (OnPause)

---

## Future Enhancements

Potential improvements for future versions:

1. **Timer Sounds:** Multiple alert tones to choose from
2. **Timer Categories:** Group timers by type (Cooking, Laundry, etc.)
3. **Scheduling:** Set timers to start at specific times
4. **Display Customization:** Let users choose theme/colors
5. **Statistics:** Track timer usage patterns
6. **Accessibility:** Screen reader support
7. **Multi-language:** Localization support
8. **Advanced Settings:** Min/max timer limits, auto-repeat

---

## Support & Documentation

### Official Resources
- Tizen NUI Documentation: https://docs.tizen.org/application/dotnet/guides/user-interface/nui/
- Tizen .NET API Reference: https://docs.tizen.org/application/dotnet/api/
- Samsung SmartThings API: https://smartthings.developer.samsung.com/

### Code Comments
All methods in MainWindow.cs include XML documentation comments:
```csharp
/// <summary>
/// Show the timer setup view
/// </summary>
```

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2026-03-01 | Initial NUI migration from ElmSharp |

---

**Status:** ✓ Implementation Complete | Ready for Tizen SDK Testing

