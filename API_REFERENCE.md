# Family Hub Timer - API Reference

## Table of Contents
1. [TimerService API](#timerservice-api)
2. [TimerModel API](#timermodel-api)
3. [PersistenceService API](#persistenceservice-api)
4. [NotificationService API](#notificationservice-api)
5. [Views API](#views-api)
6. [Constants Reference](#constants-reference)

---

## TimerService API

Central service for all timer operations. Manages collections of timers and their lifecycle.

### Construction

```csharp
var timerService = new TimerService();
timerService.Initialize();  // Load persisted timers
```

### Methods

#### `List<TimerModel> GetAllTimers()`
Returns a copy of all active timers.

**Returns**: `List<TimerModel>` - Collection of all timer instances

**Example**:
```csharp
var timers = timerService.GetAllTimers();
foreach (var timer in timers)
{
    Console.WriteLine($"{timer.Name}: {timer.GetFormattedTime()}");
}
```

---

#### `TimerModel GetTimer(string id)`
Retrieves a specific timer by ID.

**Parameters**:
- `id` (string): Timer unique identifier (UUID)

**Returns**: `TimerModel` - Timer instance or null if not found

**Example**:
```csharp
var timer = timerService.GetTimer("timer-uuid");
if (timer != null)
{
    Console.WriteLine($"Timer: {timer.Name}");
}
```

---

#### `TimerModel CreateTimer(int hours, int minutes, int seconds, string name = "Timer")`
Creates a new timer instance and adds it to the collection.

**Parameters**:
- `hours` (int): 0-99
- `minutes` (int): 0-59
- `seconds` (int): 0-59
- `name` (string, optional): Display name for the timer

**Returns**: `TimerModel` - Newly created timer instance

**Exceptions**: None (validates input internally)

**Example**:
```csharp
// Create a 5-minute pasta timer
var timer = timerService.CreateTimer(0, 5, 0, "Pasta");
Console.WriteLine($"Created: {timer.Id}");
```

---

#### `void StartTimer(string timerId)`
Starts a timer if it's in Idle state.

**Parameters**:
- `timerId` (string): ID of timer to start

**Behavior**:
- Changes state from Idle → Running
- Starts internal tick timer if first running timer
- Saves timer state
- Publishes TimerStarted event

**Example**:
```csharp
timerService.StartTimer(timer.Id);
```

---

#### `void PauseTimer(string timerId)`
Pauses a running timer.

**Parameters**:
- `timerId` (string): ID of timer to pause

**Behavior**:
- Changes state from Running → Paused
- Preserves remaining time
- Saves timer state
- Publishes TimerPaused event

**Example**:
```csharp
timerService.PauseTimer(timer.Id);
```

---

#### `void ResumeTimer(string timerId)`
Resumes a paused timer.

**Parameters**:
- `timerId` (string): ID of timer to resume

**Behavior**:
- Changes state from Paused → Running
- Restarts internal tick timer if needed
- Saves timer state
- Publishes TimerResumed event

**Example**:
```csharp
timerService.ResumeTimer(timer.Id);
Tizen.Log.Info("App", "Timer resumed");
```

---

#### `void ResetTimer(string timerId)`
Resets a timer to its original duration.

**Parameters**:
- `timerId` (string): ID of timer to reset

**Behavior**:
- Restores RemainingSeconds to TotalSeconds
- Changes state to Idle
- Clears timestamps (StartedAt, PausedAt, CompletedAt)
- Saves timer state
- Publishes TimerUpdated event

**Example**:
```csharp
timerService.ResetTimer(timer.Id);
```

---

#### `void RemoveTimer(string timerId)`
Deletes a timer from the collection.

**Parameters**:
- `timerId` (string): ID of timer to remove

**Behavior**:
- Removes timer from internal list
- Saves updated state
- Publishes TimerRemoved event

**Example**:
```csharp
timerService.RemoveTimer(timer.Id);
```

---

#### `int GetRunningTimerCount()`
Gets the number of currently running timers.

**Returns**: `int` - Count of timers in Running state

**Example**:
```csharp
if (timerService.GetRunningTimerCount() > 0)
{
    Console.WriteLine("Timers are running");
}
```

---

#### `void SaveTimers()`
Manually saves all timers to persistent storage.

**Behavior**:
- Serializes timer collection to JSON
- Writes to file system
- Called automatically on state changes

**Example**:
```csharp
// Manual save (usually automatic)
timerService.SaveTimers();
```

---

#### `void Cleanup()`
Releases all resources and prepares for shutdown.

**Behavior**:
- Stops internal tick timer
- Saves final state
- Cleans up notification service

**Example**:
```csharp
// Called in application OnTerminate
timerService.Cleanup();
```

---

### Events

#### `TimerStarted`
Published when a timer transitions to Running state.

**Event Args**: `TimerModel timer` - The started timer

**Example**:
```csharp
timerService.TimerStarted += (sender, timer) =>
{
    Tizen.Log.Info("App", $"Timer '{timer.Name}' started");
};
```

---

#### `TimerPaused`
Published when a timer transitions to Paused state.

**Event Args**: `TimerModel timer` - The paused timer

---

#### `TimerResumed`
Published when a timer transitions from Paused to Running state.

**Event Args**: `TimerModel timer` - The resumed timer

---

#### `TimerFinished`
Published when a timer completes (RemainingSeconds reaches 0).

**Event Args**: `TimerModel timer` - The finished timer

**Example**:
```csharp
timerService.TimerFinished += (sender, timer) =>
{
    _notificationService.PlayTimerAlert();
    ShowAlert($"{timer.Name} is done!");
};
```

---

#### `TimerRemoved`
Published when a timer is deleted.

**Event Args**: `TimerModel timer` - The removed timer

---

#### `TimerUpdated`
Published when timer properties change (every tick while running).

**Event Args**: `TimerModel timer` - The updated timer

**Note**: This event fires frequently. Consider debouncing if subscribing for UI updates.

**Example**:
```csharp
timerService.TimerUpdated += (sender, timer) =>
{
    // Update UI display
    UpdateProgressBar(timer.GetProgressPercentage());
};
```

---

## TimerModel API

Represents a single timer instance.

### Properties

#### `string Id` (readonly)
Unique identifier (UUID) for the timer.

```csharp
var id = timer.Id;  // "550e8400-e29b-41d4-a716-446655440000"
```

---

#### `string Name`
Display name for the timer (user-defined).

```csharp
timer.Name = "Pasta";
```

---

#### `int TotalSeconds`
Original duration in seconds.

```csharp
var total = timer.TotalSeconds;  // 300 (5 minutes)
```

---

#### `int RemainingSeconds`
Time remaining in seconds.

```csharp
var remaining = timer.RemainingSeconds;
// Updates every tick while running
```

---

#### `TimerState State`
Current state of the timer (Idle, Running, Paused, Finished).

```csharp
if (timer.State == TimerState.Running)
{
    // Timer is active
}
```

---

#### `DateTime CreatedAt`
Timestamp when timer was created.

```csharp
var created = timer.CreatedAt;
```

---

#### `DateTime? StartedAt`
Timestamp when timer was started (null if not started).

```csharp
if (timer.StartedAt.HasValue)
{
    var elapsed = DateTime.Now - timer.StartedAt.Value;
}
```

---

#### `DateTime? PausedAt`
Timestamp when timer was paused (null if not paused).

---

#### `DateTime? CompletedAt`
Timestamp when timer finished (null if not finished).

---

### Methods

#### `float GetProgressPercentage()`
Calculates progress as a percentage (0-100).

**Returns**: `float` - Percentage complete

**Example**:
```csharp
var progress = timer.GetProgressPercentage();
progressBar.SetWidth((int)(progress * 2));  // 1920px total
```

---

#### `string GetFormattedTime()`
Returns remaining time in HH:MM:SS format.

**Returns**: `string` - Formatted time string

**Example**:
```csharp
var display = timer.GetFormattedTime();
// Returns: "00:05:30" for 5m 30s remaining
```

---

#### `string GetFormattedTotalTime()`
Returns total duration in HH:MM:SS format.

**Returns**: `string` - Formatted total time

**Example**:
```csharp
var total = timer.GetFormattedTotalTime();
// Returns: "00:10:00" for 10m duration
```

---

#### `bool IsFinished()`
Checks if timer has completed.

**Returns**: `bool` - True if completed

**Example**:
```csharp
if (timer.IsFinished())
{
    Console.WriteLine("Timer complete!");
}
```

---

#### `void Tick()`
**(Internal)** Decrements remaining time by one second.

Called automatically by TimerService every 100ms.

```csharp
// Called internally - don't call directly
timer.Tick();
```

---

## PersistenceService API

Handles saving and loading timer data.

### Construction

```csharp
var persistence = new PersistenceService();
// Storage location: ~/.local/share/FamilyHubTimer/
```

### Methods

#### `void SaveTimers(List<TimerModel> timers)`
Saves timer collection to JSON file.

**Parameters**:
- `timers`: List of timers to persist

**File**: `timers.json`

**Example**:
```csharp
persistence.SaveTimers(timerService.GetAllTimers());
```

---

#### `List<TimerModel> LoadTimers()`
Loads timers from persistent storage.

**Returns**: `List<TimerModel>` - Loaded timers (empty list if file not found)

**Error Handling**: Returns empty list on error (graceful degradation)

**Example**:
```csharp
var saved = persistence.LoadTimers();
// Timers loaded and ready to use
```

---

#### `void SaveTimerHistory(List<TimerHistoryEntry> history)`
Saves timer completion history.

**Parameters**:
- `history`: List of history entries

**File**: `timer_history.json`

---

#### `List<TimerHistoryEntry> LoadTimerHistory()`
Loads timer completion history.

**Returns**: `List<TimerHistoryEntry>` - History entries

---

#### `void ClearAllData()`
Deletes all stored data (factory reset).

**Behavior**:
- Removes `timers.json`
- Removes `timer_history.json`
- Non-destructive (graceful if files don't exist)

**Example**:
```csharp
// Factory reset
persistence.ClearAllData();
```

---

## NotificationService API

Handles alerts when timers complete.

### Construction

```csharp
var notification = new NotificationService();
```

### Methods

#### `void PlayTimerAlert()`
Plays full alert sequence (vibration + sound).

**Behavior**:
- Triggers vibration pattern
- Plays system notification sound
- Non-blocking execution

**Example**:
```csharp
timerService.TimerFinished += (s, timer) =>
{
    notification.PlayTimerAlert();
};
```

---

#### `void PlayBeepSound()`
Plays audio notification only.

**System Sound**: Uses system notification sound type

**Example**:
```csharp
notification.PlayBeepSound();
```

---

#### `void Stop()`
Stops any active vibration.

**Example**:
```csharp
notification.Stop();
```

---

#### `void Dispose()`
Releases resources.

**Example**:
```csharp
notification.Dispose();
```

---

## Views API

UI layer components.

### MainSetupView

Timer creation interface.

#### Methods

- `void AdjustTime(int component, int delta, int maxValue)` - Adjust hours/minutes/seconds
- `void SetPresetTime(int seconds)` - Apply preset duration
- `void StartTimer()` - Create and start timer
- `object GetView()` - Get root UI view
- `int GetSelectedHours()` - Get hours value
- `int GetSelectedMinutes()` - Get minutes value
- `int GetSelectedSeconds()` - Get seconds value

---

### TimerListView

Multi-timer management interface.

#### Methods

- `void RefreshTimerList()` - Update displayed timers
- `void TogglePauseTimer(string timerId)` - Pause/resume
- `void ResetTimer(string timerId)` - Reset timer
- `void RemoveTimer(string timerId)` - Delete timer
- `void AddNewTimer()` - Start new timer creation
- `object GetView()` - Get root UI view
- `List<TimerModel> GetTimers()` - Get active timers

---

### TimerRunningView

Single timer active display.

#### Methods

- `void DisplayTimer(string timerId)` - Show specific timer
- `void PauseTimer()` - Pause current timer
- `void ResumeTimer()` - Resume current timer
- `void ResetTimer()` - Reset current timer
- `object GetView()` - Get root UI view
- `TimerModel GetCurrentTimer()` - Get displayed timer

---

## Constants Reference

### Colors

```csharp
AppConstants.BackgroundColor      // #0D0D0D (Dark gray)
AppConstants.PrimaryColor         // #4F75EB (Blue)
AppConstants.SecondaryColor       // #323232 (Darker gray)
AppConstants.TextColor            // #FFFFFF (White)
AppConstants.TextLightColor       // #B3B3B3 (Light gray)
AppConstants.AlertColor           // #FF3333 (Red)
AppConstants.SuccessColor         // #33CC33 (Green)
```

---

### Screen Dimensions

```csharp
AppConstants.ScreenWidth          // 1920 pixels
AppConstants.ScreenHeight         // 1080 pixels
```

---

### Spacing

```csharp
AppConstants.LargeSpacing         // 40 pixels
AppConstants.MediumSpacing        // 20 pixels
AppConstants.SmallSpacing         // 10 pixels
```

---

### Typography

```csharp
AppConstants.TitleFontSize        // 72 points
AppConstants.LargeFontSize        // 60 points
AppConstants.MediumFontSize       // 48 points
AppConstants.RegularFontSize      // 36 points
AppConstants.SmallFontSize        // 28 points
```

---

### Component Sizes

```csharp
AppConstants.ButtonHeight         // 80 pixels
AppConstants.ButtonWidth          // 300 pixels
AppConstants.SmallButtonWidth     // 150 pixels
AppConstants.SmallButtonHeight    // 60 pixels
AppConstants.CircularProgressSize // 500 pixels
AppConstants.CircularProgressStrokeWidth // 12 pixels
```

---

### Animation

```csharp
AppConstants.AnimationDuration    // 300 milliseconds
AppConstants.TimerTickInterval    // 100 milliseconds
```

---

### Validation Limits

```csharp
AppConstants.MaxHours             // 99
AppConstants.MaxMinutes           // 59
AppConstants.MaxSeconds           // 59
```

---

### Storage Keys

```csharp
AppConstants.TimerDataKey         // "family_hub_timer_data"
AppConstants.TimerHistoryKey      // "family_hub_timer_history"
```

---

## Example: Complete Usage

```csharp
// Initialize
var timerService = new TimerService();
timerService.Initialize();

// Subscribe to events
timerService.TimerFinished += (s, timer) =>
{
    Console.WriteLine($"Timer '{timer.Name}' completed!");
};

timerService.TimerUpdated += (s, timer) =>
{
    Console.WriteLine($"{timer.GetFormattedTime()} remaining");
};

// Create a timer
var timer = timerService.CreateTimer(0, 5, 0, "Cooking");

// Start it
timerService.StartTimer(timer.Id);

// Check status
Console.WriteLine($"Progress: {timer.GetProgressPercentage()}%");

// Pause
timerService.PauseTimer(timer.Id);

// Resume
timerService.ResumeTimer(timer.Id);

// Reset
timerService.ResetTimer(timer.Id);

// Remove
timerService.RemoveTimer(timer.Id);

// Cleanup
timerService.Cleanup();
```

---

**API Reference Version**: 1.0  
**Last Updated**: February 25, 2024  
**Status**: Production Ready
