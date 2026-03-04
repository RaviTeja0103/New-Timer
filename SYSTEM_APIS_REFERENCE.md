# Tizen System APIs Reference for Family Hub Timer

## Complete List of Available System APIs

### 1. **Tizen.System.Vibrator** ⚡
**Purpose:** Provides haptic feedback (vibration)
**Status:** ✅ Currently used in `NotificationService`

**Key Methods:**
```csharp
// Single vibration pattern
Vibrator.Vibrate(duration)           // duration in milliseconds
// Example: 
Vibrator.Vibrate(500);              // 500ms vibration for timer alert
Vibrator.Vibrate(100);              // Short pulse for button tap

// Pattern vibration
Vibrator.Vibrate(patterns)          // array of duration values
// Example:
Vibrator.Vibrate(new int[] { 100, 100, 100, 100, 500 });
// Short-short-short-short-long pattern
```

**Use Cases:**
- Alert notification when timer finishes
- Feedback for button presses
- Pattern alerts (long/short vibrations)
- Warning patterns for critical timers

**Implementation Location:** `Services/NotificationService.cs`

---

### 2. **Tizen.System.Display** 🖥️
**Purpose:** Control display brightness, power state, timeout
**Status:** Not yet used - Recommended for Family Hub

**Key Methods:**
```csharp
// Turn display on/off
Display.TurnOn();                    // Display power on
Display.TurnOff();                   // Display power off

// Brightness control
Display.SetBrightness(brightness);   // 0-100 brightness level
// Example:
Display.SetBrightness(75);          // 75% brightness for running timer

// Get display state
Display.GetState();                 // Current display state
Display.IsOn();                     // Check if display is on

// Auto-brightness
Display.SetAutoBrightnessEnabled(enabled);
```

**Use Cases:**
- Keep display on while timer is running
- Prevent screen timeout during countdown
- Reduce brightness for nighttime
- Increase brightness for alarm/finished state

**Family Hub Optimization:**
```csharp
// Keep display on during running timer
if (timer.State == TimerState.Running)
{
    Display.TurnOn();
    Display.SetBrightness(80);
}

// Return to normal on app exit
Display.SetAutoBrightnessEnabled(true);
```

---

### 3. **Tizen.System.Power** 🔋
**Purpose:** Manage device power state and CPU/display locks
**Status:** Recommended - Prevent app sleep during timers

**Key Methods:**
```csharp
// CPU lock
Power.RequestCpuLock(timeout);      // Prevent CPU sleep
Power.ReleaseCpuLock();             // Release CPU lock

// Display lock
Power.RequestDisplayLock(timeout);  // Prevent display sleep
Power.ReleaseDisplayLock();         // Release display lock

// Example usage:
Power.RequestCpuLock(TimeSpan.FromHours(1));
// Ensures timer continues counting even with dim display
```

**Use Cases:**
- Keep timer ticking even when display is dimmed
- Prevent device from entering deep sleep
- Critical for accurate interval timing

---

### 4. **Tizen.System.Sound** 🔊
**Purpose:** Play sounds and manage audio settings
**Status:** ✅ Currently used in `NotificationService`

**Key Methods:**
```csharp
// Play sound file
Sound.PlaySound(filePath, volume, type);

// Audio control
Sound.SetVolume(SoundType.Alarm, volume);  // 0-15
Sound.GetVolume(SoundType.Alarm);

// Get sound state
Sound.IsSilent();
Sound.IsVibrateActive();

// Sound types
SoundType.Alarm      // Alarm sounds
SoundType.Notification  // Notification sounds  
SoundType.Media      // Music/media
SoundType.Call       // Call ringtone
```

**Use Cases:**
- Play beep when timer completes
- Different alert tones for different timer types
- Respect silent mode settings

**Implementation:**
```csharp
Sound.SetVolume(SoundType.Alarm, 15); // Max volume for alert
Sound.PlaySound("/usr/share/sounds/timer-alert.wav", 1.0f, SoundType.Alarm);
```

---

### 5. **Tizen.System.SystemInfo** ℹ️
**Purpose:** Get system information about device
**Status:** Useful for device optimization

**Key Methods:**
```csharp
// Get system property
SystemInfo.GetValue(key);           // Get property value
SystemInfo.GetValue<T>(key);        // Get typed value

// Common properties
"http://tizen.org/feature/network.bluetooth"    // Bluetooth support
"http://tizen.org/feature/audio.recording"      // Mic support
"http://tizen.org/feature/sensor.accelerometer" // Accelerometer
"http://tizen.org/system/tizenversion"          // OS version
"http://tizen.org/system/model.name"            // Device model
```

**Use Cases:**
- Detect if device is Family Hub refrigerator
- Check sensor availability
- Verify audio capabilities
- Optimize UI for device capabilities

**Example:**
```csharp
var model = SystemInfo.GetValue<string>("http://tizen.org/system/model.name");
if (model.Contains("Family Hub"))
{
    // Optimize for Family Hub display
    WINDOW_WIDTH = 1080;
    WINDOW_HEIGHT = 1920;
}
```

---

### 6. **Tizen.System.Board** 🔧
**Purpose:** Get hardware information
**Status:** Optional - For device detection

**Key Methods:**
```csharp
// Get board type/info
Board.GetBoardType();               // Hardware board info
Board.GetProcessor();               // CPU information
Board.GetRam();                     // RAM amount
```

**Use Cases:**
- Verify hardware capabilities
- Adjust performance settings
- Debug on different devices

---

### 7. **Tizen.System.WiFi** 📡 (Optional)
**Purpose:** WiFi connectivity status
**Status:** Not needed for timer app

**Methods:**
```csharp
WiFi.IsActivated();                 // Check WiFi status
WiFi.GetConnectedAp();              // Get connected network
```

---

## Implementation Recommendations

### For Family Hub Timer - Recommended APIs to Add:

**Priority 1 (Critical):**
```csharp
using Tizen.System;

// In NotificationService:
Vibrator.Vibrate(300);              // Alert vibration
Sound.PlaySound(alertFile, 1.0f);   // Alert sound

// In MainWindow:
Power.RequestCpuLock(timeout);      // Keep CPU awake during timer
Display.TurnOn();                    // Ensure display is visible
```

**Priority 2 (Enhanced UX):**
```csharp
// In ShowRunningView:
Display.SetBrightness(85);          // Increase brightness for timer

// In OnTerminate:
Display.SetAutoBrightnessEnabled(true);  // Restore auto-brightness
Power.ReleaseCpuLock();              // Release power lock
```

**Priority 3 (Optimization):**
```csharp
// Device detection
var model = SystemInfo.GetValue<string>("http://tizen.org/system/model.name");
if (model.Contains("Family Hub"))
{
    // Apply Family Hub optimizations
}
```

---

## Code Example: Enhanced NotificationService with System APIs

```csharp
public class NotificationService
{
    public void PlayTimerAlert()
    {
        try
        {
            // 1. Vibration pattern (3 short pulses)
            Vibrator.Vibrate(new int[] { 100, 50, 100, 50, 100 });

            // 2. Increase volume temporarily
            Sound.SetVolume(SoundType.Alarm, 15);

            // 3. Play alert sound
            string alertPath = "/usr/share/sounds/timer-complete.wav";
            Sound.PlaySound(alertPath, 1.0f, SoundType.Alarm);

            // 4. Turn on display
            Display.TurnOn();
            Display.SetBrightness(100);  // Full brightness for alert

            Tizen.Log.Info("FamilyHubTimer", "Alert played with all effects");
        }
        catch (Exception ex)
        {
            Tizen.Log.Error("FamilyHubTimer", $"Alert error: {ex.Message}");
        }
    }
}
```

---

## Code Example: Enhanced MainWindow with System APIs

```csharp
private void ShowRunningView(string timerId)
{
    // ... existing code ...
    
    try
    {
        // Keep CPU awake for 2 hours
        Power.RequestCpuLock(TimeSpan.FromHours(2));
        
        // Ensure display stays on
        Display.TurnOn();
        Display.SetBrightness(80);
        
        // ... create UI ...
        
        StartUpdateTimer();
    }
    finally
    {
        Tizen.Log.Info("FamilyHubTimer", "Power locks set for running timer");
    }
}

protected override void OnTerminate()
{
    try
    {
        // Release locks on exit
        Power.ReleaseCpuLock();
        Display.SetAutoBrightnessEnabled(true);
        
        _timerService?.Cleanup();
    }
    catch { }
}
```

---

## Using System APIs - Complete Checklist

- [ ] Add `using Tizen.System;` to MainWindow.cs
- [ ] Add `using Tizen.System;` to NotificationService.cs
- [ ] Implement `Power.RequestCpuLock()` in ShowRunningView
- [ ] Implement `Power.ReleaseCpuLock()` in OnTerminate
- [ ] Implement `Display.TurnOn()` when timer alert fires
- [ ] Implement `Vibrator.Vibrate()` in NotificationService
- [ ] Test power management on actual device
- [ ] Monitor battery drain with system APIs enabled

---

## System APIs - Permissions Required

Add to `tizen-manifest.xml`:
```xml
<privileges>
    <privilege>http://tizen.org/privilege/vibrator</privilege>
    <privilege>http://tizen.org/privilege/sound.manager</privilege>
    <privilege>http://tizen.org/privilege/system</privilege>
    <privilege>http://tizen.org/privilege/power</privilege>
</privileges>
```

---

## Performance Impact

| API | CPU Impact | RAM | Notes |
|-----|-----------|-----|-------|
| Vibrator | Very Low | None | Only during alerts |
| Display | Low | None | Display control only |
| Power | Low | None | Lock management only |
| Sound | Medium | ~2MB | Audio playback |
| SystemInfo | Very Low | Minimal | One-time queries |

---

**All System APIs documented and ready for implementation ✓**
