# Family Hub Timer - Architecture & Design Document

## 1. System Architecture Overview

The Family Hub Timer application follows a **layered architecture** with clear separation of concerns:

```
┌─────────────────────────────────────────┐
│          UI Layer (Views)                │
│  - MainSetupView                         │
│  - TimerListView                         │
│  - TimerRunningView                      │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│    Application Layer (Services)          │
│  - TimerService (Main Orchestrator)     │
│  - NotificationService (Alerts)         │
│  - PersistenceService (Data I/O)        │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│      Data Layer (Models)                 │
│  - TimerModel (Data & State)            │
│  - TimerHistoryEntry (Audit Log)        │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│   Infrastructure (Utils & Config)        │
│  - AppConstants (Configuration)          │
│  - Tizen Framework APIs                 │
└─────────────────────────────────────────┘
```

## 2. Core Components

### 2.1 Application Entry Point (MainWindow.cs)

**Responsibility**: Application lifecycle management

```csharp
public class MainApplication
{
    private TimerService _timerService;
    
    // Lifecycle methods
    - Initialize()      // Called on app startup
    - Stop()           // Cleanup on shutdown
    
    // Event subscriptions
    - TimerStarted     // Navigate to timer view
    - TimerFinished    // Show alert dialog
}
```

**Key Responsibilities**:
- Initialize all services on startup
- Subscribe to timer events
- Coordinate view transitions
- Handle application pause/resume
- Cleanup resources on termination

---

### 2.2 Timer Service (TimerService.cs)

**Responsibility**: Central timer management and orchestration

```
TimerService (Singleton)
├── _timers: List<TimerModel>
├── _persistenceService: PersistenceService
├── _notificationService: NotificationService
├── _tickTimer: System.Threading.Timer
│
├── Public Methods:
│   ├── CreateTimer()        → TimerModel
│   ├── StartTimer()         → void
│   ├── PauseTimer()         → void
│   ├── ResumeTimer()        → void
│   ├── ResetTimer()         → void
│   ├── RemoveTimer()        → void
│   ├── GetAllTimers()       → List<TimerModel>
│   ├── GetTimer(id)         → TimerModel
│   └── SaveTimers()         → void
│
├── Events:
│   ├── TimerStarted
│   ├── TimerPaused
│   ├── TimerResumed
│   ├── TimerFinished
│   ├── TimerRemoved
│   └── TimerUpdated
│
└── Private Methods:
    ├── StartTickTimer()     → void
    ├── StopTickTimer()      → void
    └── TickCallback()       → void (100ms interval)
```

**Timer Lifecycle State Machine**:

```
            ┌─────────────────────────────┐
            │          IDLE               │
            └──────┬──────────────────────┘
                   │ CreateTimer()
                   │
            ┌──────▼──────────────────────┐
            │        NEW TIMER            │
            └──────┬──────────────────────┘
                   │ StartTimer()
                   │
            ┌──────▼──────────────────────┐
            │      RUNNING                │
            │  (Tick every 100ms)         │
            └──┬──────────────────┬───────┘
               │ PauseTimer()     │ Time expires
               │                  │ (RemainingSeconds = 0)
            ┌──▼─────────────────▼─┐
            │    PAUSED  /  FINISHED │
            └──┬─────────────────┬──┘
               │ ResumeTimer()   │ Reset or Remove
               │                 │
               └─────────────────┘
                   → IDLE / REMOVED
```

---

### 2.3 Timer Model (TimerModel.cs)

**Responsibility**: Data representation and state management

```csharp
public class TimerModel
{
    // Identity
    public string Id { get; set; }
    public string Name { get; set; }
    
    // Duration & Progress
    public int TotalSeconds { get; set; }
    public int RemainingSeconds { get; set; }
    
    // State
    public TimerState State { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? PausedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Methods
    public void Tick()                      // Decrement time
    public bool IsFinished()                // Check completion
    public float GetProgressPercentage()    // 0-100%
    public string GetFormattedTime()        // "HH:MM:SS"
}

public enum TimerState
{
    Idle,      // Created, not started
    Running,   // Currently counting down
    Paused,    // User paused
    Finished   // Countdown complete
}
```

---

### 2.4 Persistence Service (PersistenceService.cs)

**Responsibility**: Data persistence and recovery

```
Storage Architecture
├── Data Directory: ~/.local/share/FamilyHubTimer/
│
├── timers.json
│   ├── Uses: JsonSerializer (.NET 8.0)
│   ├── Format: JSON array of TimerModel
│   ├── Updated: On every timer state change
│   └── Loaded: On application startup
│
└── timer_history.json
    ├── Uses: JsonSerializer
    ├── Format: JSON array of TimerHistoryEntry
    ├── Updated: When timer completes
    └── Loaded: For analytics/history view
```

**Methods**:
- `SaveTimers(List<TimerModel>)` - Serialize timers to JSON file
- `LoadTimers()` - Deserialize timers from JSON file
- `SaveTimerHistory(List<TimerHistoryEntry>)` - Save history
- `LoadTimerHistory()` - Load history
- `ClearAllData()` - Factory reset

**Error Handling**:
```csharp
Try
    ├── Open/Create file
    ├── Serialize data
    ├── Write to disk
Catch IOException
    ├── Log error
    └── Return empty collection
```

---

### 2.5 Notification Service (NotificationService.cs)

**Responsibility**: Alert delivery (sound & vibration)

```csharp
public class NotificationService
{
    Public Methods:
    ├── PlayTimerAlert()      // Full alert sequence
    ├── PlayBeepSound()       // Audio only
    └── PlayVibrationPattern() // Haptic only
    
    Vibration Pattern:
    ├── 100ms on
    ├── 100ms off
    ├── 100ms on
    ├── 900ms off (silence)
    └── 200ms on (final pulse)
    
    Audio:
    └── System notification sound
        └── SystemSoundType.Notification
}
```

**Vibrator Compatibility**:
- Checks: `Vibrator.NumberOfVibrators > 0`
- Graceful degradation if unavailable
- Non-blocking execution

---

### 2.6 View Components

#### 2.6.1 MainSetupView (Timer Creation)

**UI Elements**:
```
┌───────────────────────────────────────┐
│         Set Timer (Title)              │
├───────────────────────────────────────┤
│                                        │
│  ┌─────────┬─────────┬─────────┐     │
│  │ Hours   │Minutes  │Seconds  │     │
│  │         │         │         │     │
│  │    ▲    │    ▲    │    ▲    │     │
│  │   00    │   00    │   00    │     │
│  │    ▼    │    ▼    │    ▼    │     │
│  └─────────┴─────────┴─────────┘     │
│                                        │
│  ┌───────┬───────┬───────┐           │
│  │ 10s   │ 15s   │ 30s   │           │
│  └───────┴───────┴───────┘           │
│                                        │
│        ┌──────────────┐               │
│        │  ▶ Start    │               │
│        └──────────────┘               │
└───────────────────────────────────────┘
```

**Features**:
- Separate hour, minute, second selectors
- Up/down buttons for adjustment
- Preset quick-select buttons
- Start button to create and begin timer

---

#### 2.6.2 TimerListView (Multi-Timer Management)

**UI Layout**:
```
┌──────────────────────────────────────┐
│    Active Timers  (Title)             │
├──────────────────────────────────────┤
│                                       │
│  ┌──────────────────────────────────┐│
│  │ Pasta                             ││
│  │ 00:05:30                          ││
│  │ ████████░░ 50%  ⏸  ↻  ✕         ││
│  └──────────────────────────────────┘│
│                                       │
│  ┌──────────────────────────────────┐│
│  │ Coffee                            ││
│  │ 00:00:45                          ││
│  │ ██░░░░░░░░ 20%  ▶  ↻  ✕         ││
│  └──────────────────────────────────┘│
│                                       │
│         ┌─────────────────┐          │
│         │ + New Timer     │          │
│         └─────────────────┘          │
└──────────────────────────────────────┘
```

**Controls**:
- **Pause/Resume** (⏸/▶): Toggle timer
- **Reset** (↻): Restore to original duration
- **Remove** (✕): Delete timer
- **Progress Bar**: Visual time representation

---

#### 2.6.3 TimerRunningView (Active Timer Display)

**UI Layout**:
```
┌──────────────────────────────────────┐
│         Pasta                         │
├──────────────────────────────────────┤
│                                       │
│                                       │
│            ╭─────────╮               │
│            │         │               │
│            │ 00:05:30│               │
│            │         │               │
│            ╰─────────╯               │
│          (Circular Progress)         │
│                                       │
│       ┌──────┐          ┌──────┐    │
│       │ ⏸   │          │  ↻   │    │
│       │Pause │          │Reset │    │
│       └──────┘          └──────┘    │
└──────────────────────────────────────┘
```

**Features**:
- Large time display (72pt font)
- Circular progress indicator (500px)
- One-timer focused view
- Minimal distractions

---

## 3. Event Flow Architecture

### 3.1 Timer Lifecycle Events

```
User Action → Service Method → State Change → Events → UI Update
    │              │                │          │         │
    ↓              ↓                ↓          ↓         ↓
CreateTimer() → Add to list → Model.State = Idle
                                                        (UI: Timer added)

StartTimer() → Update state → Model.State = Running
                 Start tick timer                       (UI: List refreshed)
                                                        (Navigation to running view)

[100ms interval]
Tick() → Decrement remaining → TimerUpdated event
         Check if finished    (UI: Progress updated)

RemainingSeconds = 0
    → Tick() detects finish
    → Model.State = Finished
    → TimerFinished event → NotificationService.PlayAlert()
                                                        (UI: Alert shown)
```

### 3.2 Event Subscription Chain

```
MainApplication
    │
    └─→ SubscribeToEvents()
        │
        ├─→ TimerService.TimerStarted += ShowTimerList()
        ├─→ TimerService.TimerFinished += ShowAlert()
        │
        └─→ Views also subscribe
            ├─→ TimerListView.TimerUpdated += RefreshList()
            └─→ TimerRunningView.TimerUpdated += UpdateDisplay()
```

---

## 4. State Management

### 4.1 Application State

```
MainApplication
├── Single TimerService instance (Singleton)
│   ├── List of TimerModels
│   ├── Tick timer (running/stopped)
│   └── Event publishers
│
└── Views (stateless UI)
    ├── Read from TimerService
    └── React to events
```

### 4.2 Timer State Persistence

```
Timer Creation
    ↓
In-Memory: _timers list
    ↓
Save on every change
    ↓
PersistenceService.SaveTimers() 
    ↓
JSON file on disk
    ↓
On app restart
    ↓
PersistenceService.LoadTimers()
    ↓
_timers list reconstructed
```

---

## 5. Threading & Concurrency

### 5.1 Tick Timer Management

```
Timer Service
├── Main thread: UI updates, user input
└── Background thread: TTimer (System.Threading.Timer)
    └── Executes TickCallback() every 100ms
        ├── Non-blocking execution
        ├── Calls TimerModel.Tick()
        ├── Publishes TimerUpdated events
        └── If no running timers → dispose timer
```

**Synchronization**:
- Thread-safe List<T> collection (no concurrent list modifications during tick)
- Events marshaled to main thread (if needed via SynchronizationContext)
- File I/O not on tick thread

### 5.2 Timer Interval Optimization

```
100ms Tick Interval Rationale:
├── Responsive UI (10 updates/second)
├── Reasonable CPU usage
├── Smooth progress bar animation
└── Sufficient precision for user experience
```

---

## 6. Error Handling Strategy

### 6.1 Error Handling Layers

```
UI Layer (Views)
    ↓ (try-catch around user interactions)
    ↓
Service Layer (TimerService)
    ↓ (try-catch in public methods)
    ↓
Data Layer (Models)
    ├─ (validation in constructors)
    └─ (bounds checking in properties)

Persistence Layer
    └─ (try-catch for file I/O)
        └─ (return empty collections on error)

Notification Layer
    └─ (try-catch for system APIs)
        └─ (graceful degradation if unavailable)
```

### 6.2 Exception Handling Examples

```csharp
// Service method error handling
public void StartTimer(string timerId)
{
    try
    {
        var timer = GetTimer(timerId);
        if (timer != null && timer.State == TimerState.Idle)
        {
            // ... logic
        }
    }
    catch (Exception ex)
    {
        Tizen.Log.Error("FamilyHubTimer", $"Error starting timer: {ex.Message}");
        // Don't throw - allow app to continue
    }
}

// Persistence error handling
public List<TimerModel> LoadTimers()
{
    try
    {
        // ... try to load
        return timers ?? new List<TimerModel>();
    }
    catch (Exception ex)
    {
        Tizen.Log.Error("FamilyHubTimer", $"Failed to load timers: {ex.Message}");
        return new List<TimerModel>(); // Graceful fallback
    }
}
```

---

## 7. Configuration & Constants

### 7.1 AppConstants Organization

```
AppConstants
├── Colors
│   ├── BackgroundColor (Dark gray)
│   ├── PrimaryColor (Blue)
│   ├── SecondaryColor (Darker gray)
│   └── TextColor (White)
│
├── Dimensions
│   ├── ScreenWidth/Height (1920x1080)
│   ├── Spacing (Large/Medium/Small)
│   ├── FontSizes (Title/Large/Medium/Regular/Small)
│   └── ComponentSizes (Button/Circle/etc)
│
├── Animation
│   ├── AnimationDuration (300ms)
│   └── TimerTickInterval (100ms)
│
└── Validation
    ├── MaxHours (99)
    ├── MaxMinutes (59)
    └── MaxSeconds (59)
```

---

## 8. UI Layout Constraints

### 8.1 Screen Layout Grid (1920x1080)

```
Safe Area (with 40px margins):
┌────────────────────────────────────────────┐
│ (40px) Content Area 1840px × 1000px        │
│                                             │
│  [160px] [20px gap] [160px] [20px gap] ... │
│                                             │
└────────────────────────────────────────────┘
```

### 8.2 Button Sizing

```
Primary Button: 300px × 80px
├── Minimum finger touch: ~10mm (48px)
├── Font: 36pt Regular
└── Padding: 10px internal

Secondary Button: 150px × 60px
├── Font: 28pt Small
└── For compact multi-button layouts

Preset Button: 150px × 80px
├── Quick preset timers
└── Grouped horizontally
```

---

## 9. Testing Strategy

### 9.1 Unit Test Coverage

```
TimerModel
├── GetProgressPercentage()
├── GetFormattedTime()
├── Tick() transitions
└── IsFinished() logic

TimerService
├── CreateTimer() uniqueness
├── State transitions
├── Event firing
└── Persistence integration

PersistenceService
├── JSON serialization
├── File I/O error handling
└── Data integrity
```

### 9.2 Integration Tests

```
1. Create → Start → Tick → Check events
2. Create → Start → Pause → Resume → Finish
3. Multiple timers simultaneously
4. Persistence round-trip (save/load)
5. View navigation and event subscriptions
```

### 9.3 UI Tests

```
1. Touch button responsiveness
2. Display updates on timer tick
3. Progress bar animation smoothness
4. Font sizing and readability
5. Color contrast for accessibility
```

---

## 10. Performance Considerations

### 10.1 Memory Management

```
Timer List Size: ~10 timers (typical)
├── Per timer: ~200 bytes (estimate)
├── Total: ~2KB active data
└── Negligible footprint

String allocations:
├── GetFormattedTime() called every tick
├── Consider string pooling if needed
└── Currently acceptable performance
```

### 10.2 CPU Optimization

```
Tick interval: 100ms
├── 10 invocations/second per running timer
├── Each tick: O(1) arithmetic + event publication
├── System.Threading.Timer is efficient
└── Stopped when no running timers

Events:
├── Published but not always subscribed
├── Only UI thread processes
└── Minimal lock contention
```

### 10.3 Disk I/O

```
Save Frequency: Every state change
├── CreateTimer
├── StartTimer
├── PauseTimer
├── ResetTimer
└── RemoveTimer

Optimization:
├── Consider batch saves if too frequent
├── File size: ~1KB per application state
└── JSON format is human-readable for debugging
```

---

## 11. Security Considerations

### 11.1 Data Security

```
Stored Data:
├── Timer names (user input)
├── Duration values (numeric, validated)
└── Timestamps (system generated)

Risks:
├── Low: Local file storage only
├── Low: No network transmission
├── Low: No sensitive data
└── Consider: File system permissions

Mitigations:
├── Input validation on timer names
├── Range validation on durations
└── Atomic file operations (write temp → rename)
```

### 11.2 API Security

No external API calls, so network security not applicable.

---

## 12. Scalability & Future Enhancements

### 12.1 Potential Improvements

```
Short-term:
├── Custom alert sounds
├── Timer name categories
├── Favorite presets
└── Dark mode toggle (theme system)

Medium-term:
├── Cloud sync with Samsung account
├── Multi-device timer sync
├── Voice control integration
└── Smart recipe integration

Long-term:
├── Machine learning for smart presets
├── Integration with Family Hub recipes
├── Energy monitoring
└── Accessibility features (audio menus)
```

### 12.2 Architecture Extensibility

```
Observer Pattern:
└── Events allow easy new subscribers
    ├── Analytics service
    ├── Smart home integration
    └── Voice assistant interface

Dependency Injection Ready:
└── Services can be abstracted to interfaces
    ├── ITimerService
    ├── IPersistenceService
    └── INotificationService
```

---

**Document Version**: 1.0  
**Last Updated**: February 25, 2024  
**Architecture Status**: Production Ready
