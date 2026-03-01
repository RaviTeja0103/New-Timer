# Migration Reference: ElmSharp → Tizen.NUI

## Quick Reference for Developers

This document shows the key differences between the old ElmSharp implementation and the new Tizen.NUI implementation.

---

## Visual Comparison

### BEFORE (ElmSharp)
```
Native Tizen UI
├─ CoreUIApplication
├─ ElmSharp components (Label, Button, Box)
├─ Native rendering engine
└─ Older maintenance model
```

### AFTER (Tizen.NUI)
```
Modern .NET UI Framework
├─ NUIApplication
├─ NUI components (TextLabel, Button, View, ScrollView)
├─ Modern layout system (LinearLayout)
└─ Active development & community support
```

---

## Code Structure Changes

### Application Base Class
```csharp
// BEFORE (ElmSharp)
public class FamilyHubTimerApplication : CoreUIApplication

// AFTER (Tizen.NUI)
public class FamilyHubTimerApplication : NUIApplication
```

### Window Initialization
```csharp
// BEFORE (ElmSharp)
_mainWindow = new Window("FamilyHubTimer");
_mainWindow.BackgroundColor = ElmSharp.Color.FromHex("#0d0d0d");
_conformant = new Conformant(_mainWindow);
_mainBox = new Box(_mainWindow);

// AFTER (Tizen.NUI)
_mainWindow = GetDefaultWindow();
_mainWindow.BackgroundColor = ConvertHexToColor(DARK_BG);
_mainWindow.WindowSize = new Size2D(WINDOW_WIDTH, WINDOW_HEIGHT);
_rootView = new View { /* Layout config */ };
_mainWindow.Add(_rootView);
```

---

## UI Component Changes

### Text Labels
```csharp
// BEFORE (ElmSharp)
var label = new Label(_mainWindow);
label.Text = "Timer";
label.TextStyle = "title";

// AFTER (Tizen.NUI)
var label = new TextLabel
{
    Text = "Timer",
    PixelSize = 60,
    TextColor = ConvertHexToColor(TEXT_COLOR),
    FontAttributes = FontAttributes.Bold
};
```

### Buttons
```csharp
// BEFORE (ElmSharp)
var btn = new Button(_mainWindow);
btn.Text = "START";
btn.Clicked += (s, e) => StartTimer();
btn.Show();

// AFTER (Tizen.NUI)
var btn = new Button
{
    Text = "START",
    HeightSpecification = 140,
    BackgroundColor = ConvertHexToColor(ACCENT_COLOR),
    ButtonStyle = new ButtonStyle { /* Style config */ }
};
btn.Clicked += (s, e) => StartTimer();
// Add to parent view automatically
```

### Containers
```csharp
// BEFORE (ElmSharp)
var box = new Box(_mainWindow);
box.IsHorizontal = true;
box.PackEnd(child1);
box.PackEnd(child2);

// AFTER (Tizen.NUI)
var container = new View
{
    Layout = new LinearLayout 
    { 
        Orientation = LinearLayout.Orientation.Horizontal,
        CellPadding = new Size2D(20, 0)
    }
};
container.Add(child1);
container.Add(child2);
```

---

## Layout System Differences

### BEFORE: ElmSharp PackingModel
```csharp
// Packing order determines layout
box.PackEnd(element1);  // Added to end
box.PackEnd(element2);  // Added after element1
box.PackEnd(element3);  // Added after element2

// Manual sizing
label.MinimumHeight = 100;
label.MinimumWidth = 200;
```

### AFTER: NUI Layout System
```csharp
// Explicit layout definition
var layout = new LinearLayout
{
    Orientation = LinearLayout.Orientation.Vertical,
    CellPadding = new Size2D(0, 20),
    Justification = LinearLayout.Justification.End
};

// Automatic sizing specifications
HeightSpecification = LayoutParamPolicies.MatchParent;  // Fill parent
WidthSpecification = LayoutParamPolicies.WrapContent;  // Fit content
WidthSpecification = 300;  // Explicit pixel size
```

---

## Sizing & Layout Properties

### Size Specifications
```csharp
// NUI uses specification policies (much more flexible)
LayoutParamPolicies.MatchParent     // Fill available space
LayoutParamPolicies.WrapContent     // Fit content size
LayoutParamPolicies.Fixed           // Use explicit size
300                                 // Explicit pixel value (implies Fixed)

// With weight for flex layout
Weight = 1.0f  // Take up available space proportionally
```

### Margins & Padding
```csharp
// BEFORE (ElmSharp) - Limited
label.Margin = "10,10";

// AFTER (Tizen.NUI) - Precise control
Margin = new Extents(left: 40, right: 40, top: 80, bottom: 60);

// Layout cell padding (spacing between children)
Layout = new LinearLayout { CellPadding = new Size2D(horizontal: 20, vertical: 10) };
```

---

## Color Handling

### BEFORE (ElmSharp)
```csharp
_mainWindow.BackgroundColor = ElmSharp.Color.FromHex("#ff0000");
label.TextColor = ElmSharp.Color.White;
button.BackgroundColor = ElmSharp.Color.FromHex("#0d0d0d");
```

### AFTER (Tizen.NUI)
```csharp
private Color ConvertHexToColor(string hex)
{
    hex = hex.TrimStart('#');
    int r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
    int g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
    int b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
    return new Color((float)r / 255, (float)g / 255, (float)b / 255, 1.0f);
}

// Usage
BackgroundColor = ConvertHexToColor("#0d0d0d");
TextColor = ConvertHexToColor("#ffffff");
```

---

## View Hierarchy Changes

### BEFORE (ElmSharp - Imperative)
```csharp
// Manually add, show, pack
var window = new Window("App");
var box = new Box(window);
var label = new Label(window);

label.Show();
box.PackEnd(label);
box.Show();
window.Show();
```

### AFTER (Tizen.NUI - Declarative with Hierarchy)
```csharp
// Create complete hierarchy, add to parent
var container = new View();
var label = new TextLabel { Text = "Hello" };
container.Add(label);  // Automatically managed

_rootView.Add(container);  // Automatic rendering
```

---

## Event Handling Similarities

This part didn't change much:
```csharp
// BEFORE & AFTER - Both support same pattern
button.Clicked += (s, e) => HandleClick();

// Service events - Same pattern
_timerService.TimerFinished += (s, timer) => HandleFinished(timer);
```

---

## Application Lifecycle

### Similar Methods
Both frameworks provide these lifecycle methods:
```csharp
OnCreate()    // App initialization - SAME PURPOSE
OnPause()     // App backgrounding - SAME PURPOSE
OnResume()    // App foregrounding - SAME PURPOSE
OnTerminate() // App cleanup - SAME PURPOSE
```

Implementation details may differ, but overall pattern is identical.

---

## Performance Considerations

### ElmSharp
- Native bindings to EFL (Elementary)
- Direct rendering to display
- Lower memory footprint
- More mature on older Tizen versions

### Tizen.NUI
- Managed C# framework
- Abstraction layer (performance overhead minimal)
- Better memory management
- Future-proof implementation
- More features out of box

---

## Import Changes

### BEFORE (ElmSharp)
```csharp
using ElmSharp;
using Tizen.Applications;  // CoreUIApplication
using Tizen.System;         // System APIs
```

### AFTER (Tizen.NUI)
```csharp
using Tizen.NUI;                    // NUIApplication
using Tizen.NUI.BaseComponents;     // TextLabel, View
using Tizen.NUI.Components;         // Button, ScrollView
using Tizen.NUI.Layout;            // LinearLayout, etc.
```

---

## Project File Changes

### BEFORE (FamilyHubTimer.csproj)
```xml
<ItemGroup>
    <PackageReference Include="Tizen.NUI" Version="0.2.43" />
    <PackageReference Include="ElmSharp" Version="1.2.2" />
    <PackageReference Include="Tizen" Version="1.0.5" />
</ItemGroup>
```

### AFTER (FamilyHubTimer.csproj)
```xml
<ItemGroup>
    <PackageReference Include="Tizen.NUI" Version="0.2.43" />
    <PackageReference Include="Tizen.NUI.XamlLoader" Version="0.2.43" />
    <PackageReference Include="Tizen" Version="1.0.5" />
</ItemGroup>
```

---

## View Transition Implementation

### BEFORE (ElmSharp)
```csharp
private void ShowNewScreen()
{
    _mainBox.Clear();  // Clear all children
    // Create new UI elements
    var label = new Label(_mainWindow);
    label.Show();
    _mainBox.PackEnd(label);
}
```

### AFTER (Tizen.NUI)
```csharp
private void ShowNewScreen()
{
    ClearRootView();  // Remove all children
    // Create new UI hierarchy
    var container = new View { /* Config */ };
    var label = new TextLabel { Text = "..." };
    container.Add(label);
    _rootView.Add(container);
}

private void ClearRootView()
{
    while (_rootView.ChildCount > 0)
    {
        _rootView.RemoveChild(_rootView.GetChildAt(0));
    }
}
```

---

## Time Display Updates

### BEFORE (ElmSharp)
```csharp
_displayUpdateTimer = new System.Threading.Timer(
    (s) => UpdateRunningDisplay(),
    null,
    TimeSpan.FromMilliseconds(100),
    TimeSpan.FromMilliseconds(100)
);

private void UpdateRunningDisplay()
{
    if (_runningTimeDisplay != null)
    {
        _runningTimeDisplay.Text = _currentRunningTimer.GetFormattedTime();
    }
}
```

### AFTER (Tizen.NUI - Same Core Logic, Better Thread Handling)
```csharp
_displayUpdateTimer = new System.Threading.Timer(
    (s) => UpdateTimerDisplay(),
    null,
    TimeSpan.FromMilliseconds(500),
    TimeSpan.FromMilliseconds(500)
);

private void UpdateTimerDisplay()
{
    if (_currentRunningTimer == null) return;
    
    _currentRunningTimer = _timerService.GetTimer(_currentRunningTimer.Id);
    if (_currentRunningTimer != null && _timerDisplayLabel != null)
    {
        _timerDisplayLabel.Text = _currentRunningTimer.GetFormattedTime();
    }
    
    if (_currentRunningTimer?.State == TimerState.Finished)
    {
        MainThread.BeginInvokeOnMainThread(() => {
            ShowRunningView(_currentRunningTimer.Id);
        });
    }
}
```

---

## Window Sizing

### BEFORE (ElmSharp)
```csharp
// No explicit window sizing - relies on device defaults
_mainWindow = new Window("FamilyHubTimer");
// Display size determined by Tizen window manager
```

### AFTER (Tizen.NUI)
```csharp
// Explicit window sizing for Family Hub
_mainWindow = GetDefaultWindow();
_mainWindow.WindowSize = new Size2D(1080, 1920);
```

---

## Service Integration - No Changes

Services work the same in both implementations:
```csharp
// Initialization - SAME
_timerService = new TimerService();
_timerService.Initialize();

// Usage - SAME
var timer = _timerService.CreateTimer(hours, minutes, seconds, "MyTimer");
_timerService.StartTimer(timer.Id);

// Events - SAME
_timerService.TimerFinished += (s, timer) => { /* ... */ };
```

---

## Migration Summary Table

| Aspect | ElmSharp | Tizen.NUI |
|--------|----------|-----------|
| **Base Class** | CoreUIApplication | NUIApplication |
| **Main Container** | Box | View + LinearLayout |
| **Text Component** | Label | TextLabel |
| **Buttons** | Button | Button (Components) |
| **Color Format** | ElmSharp.Color | Color (normalized 0-1) |
| **Layout System** | PackEnd/PackStart | LinearLayout with specs |
| **Sizing** | Manual MinimumHeight/Width | LayoutParamPolicies |
| **Window Control** | Limited | Full control via Get/Set |
| **Maintenance** | Older | Active development |

---

## Benefits of the Migration

✓ **Modern Framework** - NUI is actively maintained
✓ **Better Layout System** - LinearLayout more flexible than packing
✓ **Type Safety** - Managed C# with better IDE support
✓ **Performance** - Optimized for modern devices
✓ **Features** - More components available (ScrollView, etc.)
✓ **Documentation** - Better official resources
✓ **Future-Proof** - Tizen alignment with Samsung's direction

---

## Backward Compatibility

⚠️ **Breaking Changes**
- No direct compatibility between ElmSharp and NUI codebases
- Service layer remains identical, UI layer completely replaced
- Data models and business logic unchanged

✓ **What Stayed the Same**
- Timer service functionality
- Notification service
- Persistence service
- Data models
- Application manifest
- Build process

---

**Migration Completed:** ✓ 2026-03-01
**Time Investment:** Justified for long-term maintainability
**Recommendation:** Use NUI for all new Tizen .NET UI projects going forward

