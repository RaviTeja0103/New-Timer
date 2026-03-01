using System;
using System.Collections.Generic;
using System.Linq;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.NUI.Components;
using FamilyHubTimer.Services;
using FamilyHubTimer.Models;

namespace FamilyHubTimer
{
    /// <summary>
    /// Main Tizen NUI Application for Family Hub Timer
    /// Optimized for Samsung Family Hub (1080x1920 display)
    /// </summary>
    public class FamilyHubTimerApplication : NUIApplication
    {
        private TimerService _timerService;
        private NotificationService _notificationService;
        private Window _mainWindow;
        private View _rootView;

        // UI state
        private enum ViewState { Setup, Running, List, Finished }
        private ViewState _currentView = ViewState.Setup;

        // Setup screen state
        private TextLabel _hoursDisplay, _minutesDisplay, _secondsDisplay;
        private int _setupHours = 0, _setupMinutes = 1, _setupSeconds = 0;

        // Running screen state
        private TextLabel _timerDisplayLabel;
        private TimerModel _currentRunningTimer;
        private System.Threading.Timer _displayUpdateTimer;

        // Constants
        private const int WINDOW_WIDTH = 1080;
        private const int WINDOW_HEIGHT = 1920;
        private const string DARK_BG = "#0d0d0d";
        private const string ACCENT_COLOR = "#00a9ff";
        private const string TEXT_COLOR = "#ffffff";

        /// <summary>
        /// Application initialization
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();

            try
            {
                Tizen.Log.Info("FamilyHubTimer", "[NUI] Application OnCreate");

                // Initialize services
                _timerService = new TimerService();
                _notificationService = new NotificationService();
                _timerService.Initialize();
                _notificationService.Initialize();

                // Configure main window
                _mainWindow = GetDefaultWindow();
                _mainWindow.BackgroundColor = ConvertHexToColor(DARK_BG);
                _mainWindow.WindowSize = new Size2D(WINDOW_WIDTH, WINDOW_HEIGHT);

                // Create root view
                _rootView = new View
                {
                    WidthSpecification = WINDOW_WIDTH,
                    HeightSpecification = WINDOW_HEIGHT,
                    BackgroundColor = ConvertHexToColor(DARK_BG),
                    Layout = new LinearLayout { Orientation = LinearLayout.Orientation.Vertical }
                };

                _mainWindow.Add(_rootView);

                // Subscribe to timer events
                SubscribeToEvents();

                // Show initial view
                var existingTimers = _timerService.GetAllTimers();
                if (existingTimers.Count > 0)
                {
                    ShowTimerListView();
                }
                else
                {
                    ShowSetupView();
                }

                Tizen.Log.Info("FamilyHubTimer", "[NUI] Application created successfully");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[NUI] OnCreate failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Show the timer setup view
        /// </summary>
        private void ShowSetupView()
        {
            try
            {
                ClearRootView();
                _currentView = ViewState.Setup;

                // Scroll view for content
                var scrollView = new ScrollView
                {
                    WidthSpecification = LayoutParamPolicies.MatchParent,
                    HeightSpecification = LayoutParamPolicies.MatchParent,
                    ScrollingDirection = ScrollView.Direction.Vertical,
                    Layout = new LinearLayout { Orientation = LinearLayout.Orientation.Vertical }
                };

                // Content container
                var contentContainer = new View
                {
                    WidthSpecification = LayoutParamPolicies.MatchParent,
                    HeightSpecification = LayoutParamPolicies.WrapContent,
                    Layout = new LinearLayout { Orientation = LinearLayout.Orientation.Vertical, CellPadding = new Size2D(0, 30) }
                };

                // Title
                var titleLabel = CreateTextLabel("SET TIMER", 60, true);
                titleLabel.Margin = new Extents(40, 40, 80, 60);
                contentContainer.Add(titleLabel);

                // Time input section
                var timeContainer = new View
                {
                    WidthSpecification = LayoutParamPolicies.MatchParent,
                    HeightSpecification = LayoutParamPolicies.WrapContent,
                    Layout = new LinearLayout { Orientation = LinearLayout.Orientation.Horizontal, CellPadding = new Size2D(30, 0) }
                };

                // Hours editor
                _hoursDisplay = CreateTimeEditor("HOURS", _setupHours);
                _hoursDisplay.Parent = timeContainer;

                // Minutes editor
                _minutesDisplay = CreateTimeEditor("MINUTES", _setupMinutes);
                _minutesDisplay.Parent = timeContainer;

                // Seconds editor  
                _secondsDisplay = CreateTimeEditor("SECONDS", _setupSeconds);
                _secondsDisplay.Parent = timeContainer;

                contentContainer.Add(timeContainer);

                // Preset buttons
                var presetContainer = new View
                {
                    WidthSpecification = LayoutParamPolicies.MatchParent,
                    HeightSpecification = LayoutParamPolicies.WrapContent,
                    Layout = new LinearLayout { Orientation = LinearLayout.Orientation.Vertical, CellPadding = new Size2D(0, 20) }
                };

                var presetLabel = CreateTextLabel("Quick Set", 40, true);
                presetLabel.Margin = new Extents(40, 40, 40, 20);
                presetContainer.Add(presetLabel);

                var presetButtonContainer = new View
                {
                    WidthSpecification = LayoutParamPolicies.MatchParent,
                    HeightSpecification = LayoutParamPolicies.WrapContent,
                    Layout = new LinearLayout { Orientation = LinearLayout.Orientation.Horizontal, CellPadding = new Size2D(20, 0) }
                };

                CreatePresetButton(presetButtonContainer, "10s", 10);
                CreatePresetButton(presetButtonContainer, "5m", 300);
                CreatePresetButton(presetButtonContainer, "15m", 900);
                CreatePresetButton(presetButtonContainer, "30m", 1800);

                presetContainer.Add(presetButtonContainer);
                contentContainer.Add(presetContainer);

                // START button
                var startBtn = CreateButton("START TIMER", 80, () =>
                {
                    int total = _setupHours * 3600 + _setupMinutes * 60 + _setupSeconds;
                    if (total > 0)
                    {
                        StartTimer();
                    }
                });
                startBtn.Margin = new Extents(40, 40, 400, 100);
                contentContainer.Add(startBtn);

                scrollView.Add(contentContainer);
                _rootView.Add(scrollView);

                Tizen.Log.Info("FamilyHubTimer", "[UI] Setup view displayed");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[UI] ShowSetupView failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a time editor control
        /// </summary>
        private TextLabel CreateTimeEditor(string label, int initialValue)
        {
            var container = new View
            {
                WidthSpecification = 200,
                HeightSpecification = LayoutParamPolicies.WrapContent,
                Layout = new LinearLayout { Orientation = LinearLayout.Orientation.Vertical, CellPadding = new Size2D(0, 20) }
            };

            // Label
            var labelText = CreateTextLabel(label, 32, false);
            container.Add(labelText);

            // Value display
            var valueDisplay = CreateTextLabel(initialValue.ToString("D2"), 70, true);
            valueDisplay.TextColor = ConvertHexToColor(ACCENT_COLOR);
            container.Add(valueDisplay);

            // Button container
            var btnContainer = new View
            {
                WidthSpecification = LayoutParamPolicies.MatchParent,
                HeightSpecification = LayoutParamPolicies.WrapContent,
                Layout = new LinearLayout { Orientation = LinearLayout.Orientation.Horizontal, CellPadding = new Size2D(10, 0) }
            };

            var upBtn = CreateButton("△", 60, () =>
            {
                UpdateDisplayValue(container, label, 1);
            });
            upBtn.WidthSpecification = 100;
            btnContainer.Add(upBtn);

            var downBtn = CreateButton("▽", 60, () =>
            {
                UpdateDisplayValue(container, label, -1);
            });
            downBtn.WidthSpecification = 100;
            btnContainer.Add(downBtn);

            container.Add(btnContainer);
            return valueDisplay;
        }

        /// <summary>
        /// Update a time display value
        /// </summary>
        private void UpdateDisplayValue(View container, string label, int delta)
        {
            if (label == "HOURS")
            {
                _setupHours = Math.Max(0, Math.Min(23, _setupHours + delta));
                _hoursDisplay.Text = _setupHours.ToString("D2");
            }
            else if (label == "MINUTES")
            {
                _setupMinutes = Math.Max(0, Math.Min(59, _setupMinutes + delta));
                _minutesDisplay.Text = _setupMinutes.ToString("D2");
            }
            else if (label == "SECONDS")
            {
                _setupSeconds = Math.Max(0, Math.Min(59, _setupSeconds + delta));
                _secondsDisplay.Text = _setupSeconds.ToString("D2");
            }
        }

        /// <summary>
        /// Create a preset button
        /// </summary>
        private void CreatePresetButton(View container, string label, int seconds)
        {
            var btn = CreateButton(label, 40, () =>
            {
                _setupHours = seconds / 3600;
                _setupMinutes = (seconds % 3600) / 60;
                _setupSeconds = seconds % 60;

                _hoursDisplay.Text = _setupHours.ToString("D2");
                _minutesDisplay.Text = _setupMinutes.ToString("D2");
                _secondsDisplay.Text = _setupSeconds.ToString("D2");
            });
            btn.WidthSpecification = LayoutParamPolicies.MatchParent;
            btn.HeightSpecification = 120;
            container.Add(btn);
        }

        /// <summary>
        /// Start a new timer
        /// </summary>
        private void StartTimer()
        {
            try
            {
                int total = _setupHours * 3600 + _setupMinutes * 60 + _setupSeconds;
                if (total <= 0) return;

                var timerName = $"{_setupHours:D2}:{_setupMinutes:D2}:{_setupSeconds:D2}";
                var timer = _timerService.CreateTimer(_setupHours, _setupMinutes, _setupSeconds, timerName);
                _timerService.StartTimer(timer.Id);

                ShowRunningView(timer.Id);
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[UI] StartTimer failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Show the running timer view
        /// </summary>
        private void ShowRunningView(string timerId)
        {
            try
            {
                ClearRootView();
                _currentView = ViewState.Running;
                _currentRunningTimer = _timerService.GetTimer(timerId);

                if (_currentRunningTimer == null) return;

                var container = new View
                {
                    WidthSpecification = LayoutParamPolicies.MatchParent,
                    HeightSpecification = LayoutParamPolicies.MatchParent,
                    Layout = new LinearLayout { Orientation = LinearLayout.Orientation.Vertical, CellPadding = new Size2D(0, 60) }
                };

                // Header section
                var headerContainer = new View
                {
                    WidthSpecification = LayoutParamPolicies.MatchParent,
                    HeightSpecification = LayoutParamPolicies.WrapContent,
                    Layout = new LinearLayout { Orientation = LinearLayout.Orientation.Vertical }
                };

                // Timer name
                var nameLabel = CreateTextLabel(_currentRunningTimer.Name, 50, true);
                nameLabel.Margin = new Extents(40, 40, 80, 40);
                headerContainer.Add(nameLabel);

                // Timer display
                _timerDisplayLabel = CreateTextLabel(_currentRunningTimer.GetFormattedTime(), 140, true);
                _timerDisplayLabel.TextColor = ConvertHexToColor(ACCENT_COLOR);
                _timerDisplayLabel.Margin = new Extents(40, 40, 40, 80);
                headerContainer.Add(_timerDisplayLabel);

                // Progress indicator (visual feedback)
                var progressBar = new View
                {
                    WidthSpecification = LayoutParamPolicies.MatchParent,
                    HeightSpecification = 15,
                    BackgroundColor = ConvertHexToColor("#1a1a1a"),
                    Margin = new Extents(40, 40, 0, 40)
                };

                var progressFill = new View
                {
                    WidthSpecification = (int)(WINDOW_WIDTH * (_currentRunningTimer.GetProgressPercentage() / 100f)),
                    HeightSpecification = 15,
                    BackgroundColor = ConvertHexToColor(ACCENT_COLOR)
                };
                progressBar.Add(progressFill);

                headerContainer.Add(progressBar);
                container.Add(headerContainer);

                // State label
                var stateLabel = CreateTextLabel(GetStateText(_currentRunningTimer.State), 36, false);
                stateLabel.Margin = new Extents(40, 40, 0, 60);
                container.Add(stateLabel);

                // Control buttons
                var buttonContainer = new View
                {
                    WidthSpecification = LayoutParamPolicies.MatchParent,
                    HeightSpecification = LayoutParamPolicies.WrapContent,
                    Layout = new LinearLayout { Orientation = LinearLayout.Orientation.Vertical, CellPadding = new Size2D(0, 20) }
                };

                var pauseBtn = CreateButton(
                    _currentRunningTimer.State == TimerState.Running ? "PAUSE" : "RESUME",
                    70,
                    () => TogglePause()
                );
                pauseBtn.Margin = new Extents(40, 40, 20, 20);
                buttonContainer.Add(pauseBtn);

                var resetBtn = CreateButton("RESET", 70, () => ResetTimer());
                resetBtn.Margin = new Extents(40, 40, 20, 20);
                buttonContainer.Add(resetBtn);

                var deleteBtn = CreateButton("DELETE", 70, () => DeleteTimer());
                deleteBtn.Margin = new Extents(40, 40, 20, 20);
                buttonContainer.Add(deleteBtn);

                var backBtn = CreateButton("BACK TO LIST", 70, () => ShowTimerListView());
                backBtn.Margin = new Extents(40, 40, 20, 100);
                buttonContainer.Add(backBtn);

                container.Add(buttonContainer);
                _rootView.Add(container);

                StartDisplayUpdate();
                Tizen.Log.Info("FamilyHubTimer", "[UI] Running view displayed");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[UI] ShowRunningView failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Show the timer list view
        /// </summary>
        private void ShowTimerListView()
        {
            try
            {
                StopDisplayUpdate();
                ClearRootView();
                _currentView = ViewState.List;

                var scrollView = new ScrollView
                {
                    WidthSpecification = LayoutParamPolicies.MatchParent,
                    HeightSpecification = LayoutParamPolicies.MatchParent,
                    ScrollingDirection = ScrollView.Direction.Vertical,
                    Layout = new LinearLayout { Orientation = LinearLayout.Orientation.Vertical }
                };

                var contentContainer = new View
                {
                    WidthSpecification = LayoutParamPolicies.MatchParent,
                    HeightSpecification = LayoutParamPolicies.WrapContent,
                    Layout = new LinearLayout { Orientation = LinearLayout.Orientation.Vertical, CellPadding = new Size2D(0, 20) }
                };

                // Title
                var titleLabel = CreateTextLabel("ACTIVE TIMERS", 60, true);
                titleLabel.Margin = new Extents(40, 40, 80, 60);
                contentContainer.Add(titleLabel);

                var timers = _timerService.GetAllTimers();

                if (timers.Count == 0)
                {
                    var emptyLabel = CreateTextLabel("No active timers", 48, false);
                    emptyLabel.Margin = new Extents(40, 40, 200, 200);
                    contentContainer.Add(emptyLabel);
                }
                else
                {
                    foreach (var timer in timers)
                    {
                        CreateTimerListItem(contentContainer, timer);
                    }
                }

                // Add new button
                var addBtn = CreateButton("+ ADD NEW TIMER", 70, () => ShowSetupView());
                addBtn.Margin = new Extents(40, 40, 200, 100);
                contentContainer.Add(addBtn);

                scrollView.Add(contentContainer);
                _rootView.Add(scrollView);

                Tizen.Log.Info("FamilyHubTimer", "[UI] Timer list view displayed");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[UI] ShowTimerListView failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a timer list item
        /// </summary>
        private void CreateTimerListItem(View container, TimerModel timer)
        {
            try
            {
                var itemContainer = new View
                {
                    WidthSpecification = LayoutParamPolicies.MatchParent,
                    HeightSpecification = 180,
                    BackgroundColor = ConvertHexToColor("#1a1a1a"),
                    Layout = new LinearLayout { Orientation = LinearLayout.Orientation.Horizontal, CellPadding = new Size2D(20, 0) },
                    Margin = new Extents(40, 40, 0, 0)
                };

                // Info section
                var infoContainer = new View
                {
                    WidthSpecification = 0,
                    HeightSpecification = LayoutParamPolicies.MatchParent,
                    Layout = new LinearLayout { Orientation = LinearLayout.Orientation.Vertical, CellPadding = new Size2D(0, 10) },
                    Weight = 1.0f
                };

                var nameLabel = CreateTextLabel(timer.Name, 40, true);
                nameLabel.Margin = new Extents(20, 0, 20, 0);
                infoContainer.Add(nameLabel);

                var timeLabel = CreateTextLabel(timer.GetFormattedTime(), 50, true);
                timeLabel.TextColor = ConvertHexToColor(ACCENT_COLOR);
                timeLabel.Margin = new Extents(20, 0, 10, 0);
                infoContainer.Add(timeLabel);

                var stateLabel = CreateTextLabel(GetStateText(timer.State), 32, false);
                stateLabel.Margin = new Extents(20, 0, 10, 0);
                infoContainer.Add(stateLabel);

                itemContainer.Add(infoContainer);

                // Action buttons
                var btnContainer = new View
                {
                    WidthSpecification = LayoutParamPolicies.WrapContent,
                    HeightSpecification = LayoutParamPolicies.MatchParent,
                    Layout = new LinearLayout { Orientation = LinearLayout.Orientation.Horizontal, CellPadding = new Size2D(10, 0) }
                };

                var viewBtn = CreateButton("VIEW", 50, () => ShowRunningView(timer.Id));
                viewBtn.WidthSpecification = 140;
                btnContainer.Add(viewBtn);

                var delBtn = CreateButton("DELETE", 50, () =>
                {
                    _timerService.RemoveTimer(timer.Id);
                    ShowTimerListView();
                });
                delBtn.WidthSpecification = 140;
                btnContainer.Add(delBtn);

                itemContainer.Add(btnContainer);
                container.Add(itemContainer);
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[UI] CreateTimerListItem failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Toggle pause/resume on current timer
        /// </summary>
        private void TogglePause()
        {
            try
            {
                if (_currentRunningTimer == null) return;

                if (_currentRunningTimer.State == TimerState.Running)
                {
                    _timerService.PauseTimer(_currentRunningTimer.Id);
                }
                else if (_currentRunningTimer.State == TimerState.Paused)
                {
                    _timerService.ResumeTimer(_currentRunningTimer.Id);
                }

                // Refresh view
                ShowRunningView(_currentRunningTimer.Id);
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[UI] TogglePause failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Reset the current timer
        /// </summary>
        private void ResetTimer()
        {
            try
            {
                if (_currentRunningTimer == null) return;

                _timerService.ResetTimer(_currentRunningTimer.Id);
                ShowRunningView(_currentRunningTimer.Id);
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[UI] ResetTimer failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete the current timer
        /// </summary>
        private void DeleteTimer()
        {
            try
            {
                if (_currentRunningTimer == null) return;

                _timerService.RemoveTimer(_currentRunningTimer.Id);
                ShowTimerListView();
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[UI] DeleteTimer failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Start periodic display updates
        /// </summary>
        private void StartDisplayUpdate()
        {
            StopDisplayUpdate();
            _displayUpdateTimer = new System.Threading.Timer(
                (s) => UpdateTimerDisplay(),
                null,
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500)
            );
        }

        /// <summary>
        /// Update running timer display
        /// </summary>
        private void UpdateTimerDisplay()
        {
            try
            {
                if (_currentRunningTimer == null) return;

                _currentRunningTimer = _timerService.GetTimer(_currentRunningTimer.Id);
                if (_currentRunningTimer != null && _timerDisplayLabel != null)
                {
                    _timerDisplayLabel.Text = _currentRunningTimer.GetFormattedTime();
                }

                // Check if timer finished
                if (_currentRunningTimer?.State == TimerState.Finished)
                {
                    StopDisplayUpdate();
                    _notificationService?.PlayTimerAlert();
                    // Refresh view to show finished state
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ShowRunningView(_currentRunningTimer.Id);
                    });
                }
            }
            catch { }
        }

        /// <summary>
        /// Stop display updates
        /// </summary>
        private void StopDisplayUpdate()
        {
            if (_displayUpdateTimer != null)
            {
                _displayUpdateTimer.Dispose();
                _displayUpdateTimer = null;
            }
        }

        /// <summary>
        /// Clear root view
        /// </summary>
        private void ClearRootView()
        {
            while (_rootView.ChildCount > 0)
            {
                _rootView.RemoveChild(_rootView.GetChildAt(0));
            }
        }

        /// <summary>
        /// Create a text label
        /// </summary>
        private TextLabel CreateTextLabel(string text, int fontSize, bool isBold)
        {
            return new TextLabel
            {
                Text = text,
                PixelSize = (uint)fontSize,
                TextColor = ConvertHexToColor(TEXT_COLOR),
                WidthSpecification = LayoutParamPolicies.MatchParent,
                HeightSpecification = LayoutParamPolicies.WrapContent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontAttributes = isBold ? FontAttributes.Bold : FontAttributes.Default
            };
        }

        /// <summary>
        /// Create a button
        /// </summary>
        private Button CreateButton(string text, int fontSize, Action onClicked)
        {
            var btn = new Button
            {
                Text = text,
                WidthSpecification = LayoutParamPolicies.MatchParent,
                HeightSpecification = 140,
                BackgroundColor = ConvertHexToColor(ACCENT_COLOR),
                ButtonStyle = new ButtonStyle
                {
                    Text = new TextLabelStyle
                    {
                        PixelSize = (uint)fontSize,
                        TextColor = ConvertHexToColor("#000000"),
                        FontAttributes = FontAttributes.Bold
                    }
                }
            };

            btn.Clicked += (s, e) => onClicked?.Invoke();
            return btn;
        }

        /// <summary>
        /// Convert hex color to NUI Color
        /// </summary>
        private Color ConvertHexToColor(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                int r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                int g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                int b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                return new Color((float)r / 255, (float)g / 255, (float)b / 255, 1.0f);
            }
            return new Color(1, 1, 1, 1);
        }

        /// <summary>
        /// Get state text for display
        /// </summary>
        private string GetStateText(TimerState state)
        {
            return state switch
            {
                TimerState.Running => "● RUNNING",
                TimerState.Paused => "⏸ PAUSED",
                TimerState.Finished => "✓ FINISHED",
                TimerState.Idle => "○ STOPPED",
                _ => "UNKNOWN"
            };
        }

        /// <summary>
        /// Subscribe to timer service events
        /// </summary>
        private void SubscribeToEvents()
        {
            try
            {
                _timerService.TimerFinished += (s, timer) =>
                {
                    Tizen.Log.Info("FamilyHubTimer", $"[EVENT] Timer finished: {timer.Id}");
                    _notificationService?.PlayTimerAlert();
                };

                _timerService.TimerRemoved += (s, timer) =>
                {
                    Tizen.Log.Info("FamilyHubTimer", $"[EVENT] Timer removed: {timer.Id}");
                };

                Tizen.Log.Info("FamilyHubTimer", "[MAIN] Event handlers registered");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[MAIN] Failed to subscribe to events: {ex.Message}");
            }
        }

        /// <summary>
        /// Application pause
        /// </summary>
        protected override void OnPause()
        {
            base.OnPause();
            Tizen.Log.Info("FamilyHubTimer", "[NUI] Application paused");

            try
            {
                _timerService?.Pause();
                StopDisplayUpdate();
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[NUI] OnPause failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Application resume
        /// </summary>
        protected override void OnResume()
        {
            base.OnResume();
            Tizen.Log.Info("FamilyHubTimer", "[NUI] Application resumed");

            try
            {
                _timerService?.Resume();
                if (_currentView == ViewState.Running)
                {
                    StartDisplayUpdate();
                }
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[NUI] OnResume failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Application terminate
        /// </summary>
        protected override void OnTerminate()
        {
            base.OnTerminate();
            Tizen.Log.Info("FamilyHubTimer", "[NUI] Application terminating");

            try
            {
                StopDisplayUpdate();
                _timerService?.Cleanup();
                _notificationService?.Dispose();
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[NUI] OnTerminate failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Application entry point
    /// </summary>
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Tizen.Log.Info("FamilyHubTimer", "[BOOTSTRAP] Starting Family Hub Timer with NUI");
                var app = new FamilyHubTimerApplication();
                app.Run(args);
                Tizen.Log.Info("FamilyHubTimer", "[BOOTSTRAP] Application exited");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[BOOTSTRAP] Fatal error: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
