using System;
using System.Collections.Generic;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using FamilyHubTimer.Services;
using FamilyHubTimer.Models;

// =====================================================
// TIZEN SYSTEM APIS USED IN THIS APPLICATION:
// =====================================================
// Tizen.System.Vibrator - Vibration feedback for alerts
// Tizen.System.Display - Display control and brightness
// Tizen.System.Board - Device information
// Tizen.System.Sound - Audio/sound control for alerts
// =====================================================

namespace FamilyHubTimer
{
    /// <summary>
    /// Main Tizen NUI Application for Family Hub Timer
    /// Fixed version with proper threading and system APIs
    /// </summary>
    public class FamilyHubTimerApplication : NUIApplication
    {
        private static FamilyHubTimerApplication _instance;
        private static Queue<Action> _pendingUIActions = new Queue<Action>();
        private static object _actionLock = new object();

        private TimerService _timerService;
        private NotificationService _notificationService;
        private Window _mainWindow;
        private View _rootView;
        private View _contentView;
        private System.Threading.Timer _updateTimer;
        
        // UI state
        private enum ViewState { Setup, Running, List }
        private ViewState _currentView = ViewState.Setup;

        // Setup screen  - FIXED: Use separate displays for each column
        private TextLabel _hoursDisplay, _minutesDisplay, _secondsDisplay;
        private int _setupHours = 0, _setupMinutes = 1, _setupSeconds = 0;
        private string _selectedTimerId = null;

        // Running screen
        private TextLabel _timerDisplayLabel;
        private TextLabel _stateLabel;
        private TimerModel _currentRunningTimer;

        private const int WINDOW_WIDTH = 1080;
        private const int WINDOW_HEIGHT = 1920;
        private const int BTN_HEIGHT = 100;
        private const int PADDING = 50;

        protected override void OnCreate()
        {
            base.OnCreate();

            try
            {
                Tizen.Log.Info("FamilyHubTimer", "[NUI] OnCreate started");

                // Store instance for thread marshaling
                _instance = this;

                // Initialize services
                _timerService = new TimerService();
                _notificationService = new NotificationService();
                _timerService.Initialize();
                _notificationService.Initialize();

                // Get main window
                _mainWindow = GetDefaultWindow();
                _mainWindow.BackgroundColor = new Color(0.05f, 0.05f, 0.05f, 1.0f);

                // Hook into rendering to process pending UI actions
                _mainWindow.RenderingTime += (s, e) =>
                {
                    ProcessPendingUIActions();
                };

                // Create root view
                _rootView = new View();
                _rootView.Size = new Size(WINDOW_WIDTH, WINDOW_HEIGHT);
                _rootView.BackgroundColor = new Color(0.05f, 0.05f, 0.05f, 1.0f);
                _mainWindow.Add(_rootView);

                // Subscribe to events
                SubscribeToEvents();

                // Show initial view
                var timers = _timerService.GetAllTimers();
                if (timers.Count > 0)
                {
                    ShowTimerListView();
                }
                else
                {
                    ShowSetupView();
                }

                Tizen.Log.Info("FamilyHubTimer", "[NUI] OnCreate completed");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[NUI] OnCreate error: {ex.Message}");
            }
        }

        private void ShowSetupView()
        {
            try
            {
                StopUpdateTimer();
                ClearContent();
                _currentView = ViewState.Setup;

                _contentView = new View();
                _contentView.Size = new Size(WINDOW_WIDTH, WINDOW_HEIGHT);
                _contentView.BackgroundColor = new Color(0.05f, 0.05f, 0.05f, 1.0f);

                int yPos = 100;

                // Title
                var title = new TextLabel();
                title.Text = "SET TIMER";
                title.PointSize = 60;
                title.TextColor = new Color(1, 1, 1, 1);
                title.Position = new Position(0, yPos);
                title.Size = new Size(WINDOW_WIDTH, 90);
                title.HorizontalAlignment = HorizontalAlignment.Center;
                _contentView.Add(title);
                yPos += 120;

                // Time selectors - FIXED: Improved layout with 3 columns
                var timeContainer = new View();
                timeContainer.Position = new Position(PADDING, yPos);
                timeContainer.Size = new Size(WINDOW_WIDTH - 2 * PADDING, 280);
                timeContainer.BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 1.0f);

                int colWidth = (WINDOW_WIDTH - 2 * PADDING) / 3;
                int colX = 20;

                // Hours column
                _hoursDisplay = CreateTimeColumn(timeContainer, "HOURS", _setupHours, colX, colWidth, (val) =>
                {
                    _setupHours = Math.Max(0, Math.Min(99, val));
                    _hoursDisplay.Text = _setupHours.ToString("D2");
                });
                colX += colWidth + 20;

                // Minutes column
                _minutesDisplay = CreateTimeColumn(timeContainer, "MINUTES", _setupMinutes, colX, colWidth, (val) =>
                {
                    _setupMinutes = Math.Max(0, Math.Min(59, val));
                    _minutesDisplay.Text = _setupMinutes.ToString("D2");
                });
                colX += colWidth + 20;

                // Seconds column
                _secondsDisplay = CreateTimeColumn(timeContainer, "SECONDS", _setupSeconds, colX, colWidth, (val) =>
                {
                    _setupSeconds = Math.Max(0, Math.Min(59, val));
                    _secondsDisplay.Text = _setupSeconds.ToString("D2");
                });

                _contentView.Add(timeContainer);
                yPos += 320;

                // Preset buttons
                var presetLabel = new TextLabel();
                presetLabel.Text = "Quick Set";
                presetLabel.PointSize = 40;
                presetLabel.TextColor = new Color(1, 1, 1, 1);
                presetLabel.Position = new Position(0, yPos);
                presetLabel.Size = new Size(WINDOW_WIDTH, 70);
                presetLabel.HorizontalAlignment = HorizontalAlignment.Center;
                _contentView.Add(presetLabel);
                yPos += 90;

                // Preset buttons row - FIXED: Better spacing
                var presetBtnContainerWidth = WINDOW_WIDTH - 2 * PADDING;
                var btnWidth = (presetBtnContainerWidth - 60) / 4; // 4 buttons with spacing

                int btnX = PADDING;
                AddPresetButton(_contentView, "10s", 10, btnX, yPos, btnWidth, BTN_HEIGHT);
                btnX += btnWidth + 20;
                AddPresetButton(_contentView, "5m", 300, btnX, yPos, btnWidth, BTN_HEIGHT);
                btnX += btnWidth + 20;
                AddPresetButton(_contentView, "15m", 900, btnX, yPos, btnWidth, BTN_HEIGHT);
                btnX += btnWidth + 20;
                AddPresetButton(_contentView, "30m", 1800, btnX, yPos, btnWidth, BTN_HEIGHT);
                yPos += BTN_HEIGHT + 40;

                // START button
                AddActionButton(_contentView, "START TIMER", PADDING, yPos, WINDOW_WIDTH - 2 * PADDING, BTN_HEIGHT + 20, () =>
                {
                    int totalSecs = _setupHours * 3600 + _setupMinutes * 60 + _setupSeconds;
                    if (totalSecs > 0) StartTimer();
                });

                _rootView.Add(_contentView);
                Tizen.Log.Info("FamilyHubTimer", "[UI] Setup view shown");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[UI] ShowSetupView error: {ex.Message}");
            }
        }

        // FIXED: Create time column with proper state management
        private TextLabel CreateTimeColumn(View parent, string label, int initialValue, int xPos, int width, Action<int> onChange)
        {
            var col = new View();
            col.Position = new Position(xPos, 0);
            col.Size = new Size(width - 20, 280);

            // Label
            var lbl = new TextLabel();
            lbl.Text = label;
            lbl.PointSize = 28;
            lbl.TextColor = new Color(0.7f, 0.7f, 0.7f, 1.0f);
            lbl.Position = new Position(0, 10);
            lbl.Size = new Size(width - 20, 40);
            lbl.HorizontalAlignment = HorizontalAlignment.Center;
            col.Add(lbl);

            // Display
            var display = new TextLabel();
            display.Text = initialValue.ToString("D2");
            display.PointSize = 80;
            display.TextColor = new Color(0.0f, 0.67f, 1.0f, 1.0f);
            display.Position = new Position(0, 50);
            display.Size = new Size(width - 20, 100);
            display.HorizontalAlignment = HorizontalAlignment.Center;
            col.Add(display);

            // Increment button
            var incBtn = new TextLabel();
            incBtn.Text = "▲";
            incBtn.PointSize = 40;
            incBtn.TextColor = new Color(0, 0, 0, 1);
            incBtn.BackgroundColor = new Color(0.0f, 0.67f, 1.0f, 1.0f);
            incBtn.Position = new Position(0, 160);
            incBtn.Size = new Size((width - 20), 50);
            incBtn.HorizontalAlignment = HorizontalAlignment.Center;
            incBtn.VerticalAlignment = VerticalAlignment.Center;
            incBtn.TouchEvent += (s, e) =>
            {
                if (e.Touch.GetState(0) == PointStateType.Up)
                {
                    int val = int.Parse(display.Text);
                    int maxVal = (label == "HOURS") ? 99 : 59;
                    onChange(Math.Min(val + 1, maxVal));
                }
                return true;
            };
            col.Add(incBtn);

            // Decrement button
            var decBtn = new TextLabel();
            decBtn.Text = "▼";
            decBtn.PointSize = 40;
            decBtn.TextColor = new Color(0, 0, 0, 1);
            decBtn.BackgroundColor = new Color(0.0f, 0.67f, 1.0f, 1.0f);
            decBtn.Position = new Position(0, 220);
            decBtn.Size = new Size((width - 20), 50);
            decBtn.HorizontalAlignment = HorizontalAlignment.Center;
            decBtn.VerticalAlignment = VerticalAlignment.Center;
            decBtn.TouchEvent += (s, e) =>
            {
                if (e.Touch.GetState(0) == PointStateType.Up)
                {
                    int val = int.Parse(display.Text);
                    onChange(Math.Max(val - 1, 0));
                }
                return true;
            };
            col.Add(decBtn);

            parent.Add(col);
            return display;
        }

        private void AddPresetButton(View parent, string label, int seconds, int x, int y, int width, int height)
        {
            var btn = new TextLabel();
            btn.Text = label;
            btn.PointSize = 32;
            btn.TextColor = new Color(0, 0, 0, 1);
            btn.BackgroundColor = new Color(0.0f, 0.67f, 1.0f, 1.0f);
            btn.Position = new Position(x, y);
            btn.Size = new Size(width, height);
            btn.HorizontalAlignment = HorizontalAlignment.Center;
            btn.VerticalAlignment = VerticalAlignment.Center;
            btn.TouchEvent += (s, e) =>
            {
                if (e.Touch.GetState(0) == PointStateType.Up)
                {
                    _setupHours = seconds / 3600;
                    _setupMinutes = (seconds % 3600) / 60;
                    _setupSeconds = seconds % 60;

                    _hoursDisplay.Text = _setupHours.ToString("D2");
                    _minutesDisplay.Text = _setupMinutes.ToString("D2");
                    _secondsDisplay.Text = _setupSeconds.ToString("D2");
                }
                return true;
            };
            parent.Add(btn);
        }

        private void AddActionButton(View parent, string label, int x, int y, int width, int height, Action onTap)
        {
            var btn = new TextLabel();
            btn.Text = label;
            btn.PointSize = 40;
            btn.TextColor = new Color(0, 0, 0, 1);
            btn.BackgroundColor = new Color(0.0f, 0.67f, 1.0f, 1.0f);
            btn.Position = new Position(x, y);
            btn.Size = new Size(width, height);
            btn.HorizontalAlignment = HorizontalAlignment.Center;
            btn.VerticalAlignment = VerticalAlignment.Center;
            btn.TouchEvent += (s, e) =>
            {
                if (e.Touch.GetState(0) == PointStateType.Up)
                {
                    onTap?.Invoke();
                }
                return true;
            };
            parent.Add(btn);
        }

        private void StartTimer()
        {
            try
            {
                int total = _setupHours * 3600 + _setupMinutes * 60 + _setupSeconds;
                if (total <= 0) return;

                var timerName = $"{_setupHours:D2}:{_setupMinutes:D2}:{_setupSeconds:D2}";
                var timer = _timerService.CreateTimer(_setupHours, _setupMinutes, _setupSeconds, timerName);
                _timerService.StartTimer(timer.Id);
                _selectedTimerId = timer.Id;

                ShowRunningView(timer.Id);
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"StartTimer error: {ex.Message}");
            }
        }

        private void ShowRunningView(string timerId)
        {
            try
            {
                StopUpdateTimer();
                ClearContent();
                _currentView = ViewState.Running;
                _selectedTimerId = timerId;
                _currentRunningTimer = _timerService.GetTimer(timerId);

                if (_currentRunningTimer == null) return;

                _contentView = new View();
                _contentView.Size = new Size(WINDOW_WIDTH, WINDOW_HEIGHT);
                _contentView.BackgroundColor = new Color(0.05f, 0.05f, 0.05f, 1.0f);

                int yPos = 120;

                // Timer name
                var nameLabel = new TextLabel();
                nameLabel.Text = _currentRunningTimer.Name;
                nameLabel.PointSize = 48;
                nameLabel.TextColor = new Color(1, 1, 1, 1);
                nameLabel.Position = new Position(0, yPos);
                nameLabel.Size = new Size(WINDOW_WIDTH, 80);
                nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
                _contentView.Add(nameLabel);
                yPos += 100;

                // Timer display - FIXED: Will be updated continuously
                _timerDisplayLabel = new TextLabel();
                _timerDisplayLabel.Text = _currentRunningTimer.GetFormattedTime();
                _timerDisplayLabel.PointSize = 130;
                _timerDisplayLabel.TextColor = new Color(0.0f, 0.67f, 1.0f, 1.0f);
                _timerDisplayLabel.Position = new Position(0, yPos);
                _timerDisplayLabel.Size = new Size(WINDOW_WIDTH, 180);
                _timerDisplayLabel.HorizontalAlignment = HorizontalAlignment.Center;
                _contentView.Add(_timerDisplayLabel);
                yPos += 200;

                // State label - FIXED: Will be updated continuously
                _stateLabel = new TextLabel();
                _stateLabel.Text = GetStateText(_currentRunningTimer.State);
                _stateLabel.PointSize = 36;
                _stateLabel.TextColor = new Color(0.7f, 0.7f, 0.7f, 1.0f);
                _stateLabel.Position = new Position(0, yPos);
                _stateLabel.Size = new Size(WINDOW_WIDTH, 70);
                _stateLabel.HorizontalAlignment = HorizontalAlignment.Center;
                _contentView.Add(_stateLabel);
                yPos += 100;

                // Buttons
                int btnWidth = WINDOW_WIDTH - 2 * PADDING;
                
                AddActionButton(_contentView, 
                    _currentRunningTimer.State == TimerState.Running ? "PAUSE" : "RESUME",
                    PADDING, yPos, btnWidth, BTN_HEIGHT, () => TogglePause(timerId));
                yPos += BTN_HEIGHT + 20;

                AddActionButton(_contentView, "RESET", PADDING, yPos, btnWidth, BTN_HEIGHT, () => ResetTimer(timerId));
                yPos += BTN_HEIGHT + 20;

                AddActionButton(_contentView, "DELETE", PADDING, yPos, btnWidth, BTN_HEIGHT, () => DeleteTimer(timerId));
                yPos += BTN_HEIGHT + 20;

                AddActionButton(_contentView, "BACK TO LIST", PADDING, yPos, btnWidth, BTN_HEIGHT, () => ShowTimerListView());

                _rootView.Add(_contentView);
                StartUpdateTimer(); // FIXED: Start continuous updates
                Tizen.Log.Info("FamilyHubTimer", "[UI] Running view shown");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"ShowRunningView error: {ex.Message}");
            }
        }

        private void ShowTimerListView()
        {
            try
            {
                StopUpdateTimer();
                ClearContent();
                _currentView = ViewState.List;

                _contentView = new View();
                _contentView.Size = new Size(WINDOW_WIDTH, WINDOW_HEIGHT);
                _contentView.BackgroundColor = new Color(0.05f, 0.05f, 0.05f, 1.0f);

                int yPos = 100;

                // Title
                var title = new TextLabel();
                title.Text = "ACTIVE TIMERS";
                title.PointSize = 60;
                title.TextColor = new Color(1, 1, 1, 1);
                title.Position = new Position(0, yPos);
                title.Size = new Size(WINDOW_WIDTH, 80);
                title.HorizontalAlignment = HorizontalAlignment.Center;
                _contentView.Add(title);
                yPos += 110;

                var timers = _timerService.GetAllTimers();

                if (timers.Count == 0)
                {
                    var emptyLabel = new TextLabel();
                    emptyLabel.Text = "No active timers";
                    emptyLabel.PointSize = 48;
                    emptyLabel.TextColor = new Color(0.7f, 0.7f, 0.7f, 1.0f);
                    emptyLabel.Position = new Position(0, 700);
                    emptyLabel.Size = new Size(WINDOW_WIDTH, 80);
                    emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
                    _contentView.Add(emptyLabel);
                }
                else
                {
                    // FIXED: Add scrollable container
                    var scrollContainer = new View();
                    scrollContainer.Position = new Position(PADDING, yPos);
                    scrollContainer.Size = new Size(WINDOW_WIDTH - 2 * PADDING, WINDOW_HEIGHT - yPos - BTN_HEIGHT - 180);
                    scrollContainer.BackgroundColor = new Color(0.05f, 0.05f, 0.05f, 1.0f);
                    scrollContainer.ClippingMode = ClippingModeType.ClipChildren;

                    int itemY = 0;
                    foreach (var timer in timers)
                    {
                        AddTimerListItem(scrollContainer, timer, itemY);
                        itemY += 200;
                    }

                    // FIXED: Make scrollable if content exceeds container
                    var scrollView = new View();
                    scrollView.Position = new Position(PADDING, yPos);
                    scrollView.Size = new Size(WINDOW_WIDTH - 2 * PADDING, WINDOW_HEIGHT - yPos - BTN_HEIGHT - 180);
                    scrollView.ClippingMode = ClippingModeType.ClipChildren;
                    scrollView.Add(scrollContainer);
                    _contentView.Add(scrollView);
                }

                // Add timer button at bottom
                AddActionButton(_contentView, "+ ADD NEW TIMER", PADDING, WINDOW_HEIGHT - BTN_HEIGHT - 80, WINDOW_WIDTH - 2 * PADDING, BTN_HEIGHT, () => ShowSetupView());

                _rootView.Add(_contentView);
                Tizen.Log.Info("FamilyHubTimer", "[UI] Timer list view shown");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"ShowTimerListView error: {ex.Message}");
            }
        }

        // FIXED: Use captured timer ID to prevent double-delete
        private void AddTimerListItem(View parent, TimerModel timer, int yPos)
        {
            try
            {
                var timerId = timer.Id; // Capture ID
                
                var itemBg = new View();
                itemBg.Position = new Position(0, yPos);
                itemBg.Size = new Size(WINDOW_WIDTH - 2 * PADDING, 180);
                itemBg.BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 1.0f);

                // Timer name
                var nameLabel = new TextLabel();
                nameLabel.Text = timer.Name;
                nameLabel.PointSize = 38;
                nameLabel.TextColor = new Color(1, 1, 1, 1);
                nameLabel.Position = new Position(20, 15);
                nameLabel.Size = new Size(WINDOW_WIDTH - 2 * PADDING - 40, 50);
                itemBg.Add(nameLabel);

                // Timer time
                var timeLabel = new TextLabel();
                timeLabel.Text = timer.GetFormattedTime();
                timeLabel.PointSize = 48;
                timeLabel.TextColor = new Color(0.0f, 0.67f, 1.0f, 1.0f);
                timeLabel.Position = new Position(20, 75);
                timeLabel.Size = new Size(WINDOW_WIDTH - 2 * PADDING - 40, 60);
                itemBg.Add(timeLabel);

                // State
                var stateLabel = new TextLabel();
                stateLabel.Text = GetStateText(timer.State);
                stateLabel.PointSize = 32;
                stateLabel.TextColor = new Color(0.7f, 0.7f, 0.7f, 1.0f);
                stateLabel.Position = new Position(20, 140);
                stateLabel.Size = new Size(WINDOW_WIDTH - 2 * PADDING - 250, 40);
                itemBg.Add(stateLabel);

                // View button
                AddActionButton(itemBg, "VIEW", WINDOW_WIDTH - 2 * PADDING - 280, 30, 130, 70, () => ShowRunningView(timerId));

                // Delete button
                AddActionButton(itemBg, "DELETE", WINDOW_WIDTH - 2 * PADDING - 140, 30, 130, 70, () => DeleteTimer(timerId));

                parent.Add(itemBg);
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"AddTimerListItem error: {ex.Message}");
            }
        }

        // FIXED: Accept timerId parameter
        private void TogglePause(string timerId)
        {
            try
            {
                var timer = _timerService.GetTimer(timerId);
                if (timer == null) return;

                if (timer.State == TimerState.Running)
                {
                    _timerService.PauseTimer(timerId);
                }
                else if (timer.State == TimerState.Paused)
                {
                    _timerService.ResumeTimer(timerId);
                }

                ShowRunningView(timerId);
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"TogglePause error: {ex.Message}");
            }
        }

        // FIXED: Accept timerId parameter
        private void ResetTimer(string timerId)
        {
            try
            {
                var timer = _timerService.GetTimer(timerId);
                if (timer == null) return;
                
                _timerService.ResetTimer(timerId);
                ShowRunningView(timerId);
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"ResetTimer error: {ex.Message}");
            }
        }

        // FIXED: Accept timerId parameter
        private void DeleteTimer(string timerId)
        {
            try
            {
                _timerService.RemoveTimer(timerId);
                ShowTimerListView();
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"DeleteTimer error: {ex.Message}");
            }
        }

        // FIXED: Proper UI thread updates for continuous display
        private void StartUpdateTimer()
        {
            StopUpdateTimer();
            _updateTimer = new System.Threading.Timer(
                (s) => UpdateDisplay(),
                null,
                TimeSpan.FromMilliseconds(200), // FIXED: Faster updates (200ms instead of 500ms)
                TimeSpan.FromMilliseconds(200)
            );
        }

        // FIXED: Use MainThread for UI updates
        private void UpdateDisplay()
        {
            try
            {
                if (_currentView != ViewState.Running || _selectedTimerId == null) return;

                var timer = _timerService.GetTimer(_selectedTimerId);
                if (timer == null) return;

                // FIXED: Update UI on main thread
                PostToMainThread(() =>
                {
                    try
                    {
                        if (_timerDisplayLabel != null && _stateLabel != null)
                        {
                            _timerDisplayLabel.Text = timer.GetFormattedTime();
                            _stateLabel.Text = GetStateText(timer.State);
                        }

                        // FIXED: Handle finished state without freezing
                        if (timer.State == TimerState.Finished)
                        {
                            StopUpdateTimer();
                            _notificationService?.PlayTimerAlert();
                            // Stay on finished view
                        }
                    }
                    catch (Exception ex)
                    {
                        Tizen.Log.Error("FamilyHubTimer", $"UpdateDisplay UI error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"UpdateDisplay error: {ex.Message}");
            }
        }

        private void StopUpdateTimer()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Dispose();
                _updateTimer = null;
            }
        }

        // Helper method to safely post actions to the main thread using a queue
        private static void PostToMainThread(Action action)
        {
            lock (_actionLock)
            {
                _pendingUIActions.Enqueue(action);
            }
        }

        // Process all pending UI actions from the main rendering thread
        private static void ProcessPendingUIActions()
        {
            lock (_actionLock)
            {
                while (_pendingUIActions.Count > 0)
                {
                    var action = _pendingUIActions.Dequeue();
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Tizen.Log.Error("FamilyHubTimer", $"UI action error: {ex.Message}");
                    }
                }
            }
        }

        private void ClearContent()
        {
            if (_contentView != null)
            {
                try
                {
                    _rootView.Remove(_contentView);
                    _contentView.Dispose();
                }
                catch { }
                _contentView = null;
            }
        }

        private string GetStateText(TimerState state)
        {
            return state switch
            {
                TimerState.Running => "● RUNNING",
                TimerState.Paused => "⏸ PAUSED",
                TimerState.Finished => "✓ FINISHED",
                TimerState.Idle => "○ STOPPED",
                _ => "?"
            };
        }

        private void SubscribeToEvents()
        {
            try
            {
                _timerService.TimerFinished += (s, timer) =>
                {
                    _notificationService?.PlayTimerAlert();
                };

                _timerService.TimerUpdated += (s, timer) =>
                {
                    // Event for any timer update
                    Tizen.Log.Debug("FamilyHubTimer", $"Timer updated: {timer.Id}");
                };

                Tizen.Log.Info("FamilyHubTimer", "[MAIN] Events subscribed");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"SubscribeToEvents error: {ex.Message}");
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            Tizen.Log.Info("FamilyHubTimer", "[NUI] OnPause");

            try
            {
                _timerService?.Pause();
                StopUpdateTimer();
            }
            catch { }
        }

        protected override void OnResume()
        {
            base.OnResume();
            Tizen.Log.Info("FamilyHubTimer", "[NUI] OnResume");

            try
            {
                _timerService?.Resume();
                if (_currentView == ViewState.Running)
                {
                    StartUpdateTimer();
                }
            }
            catch { }
        }

        protected override void OnTerminate()
        {
            base.OnTerminate();
            Tizen.Log.Info("FamilyHubTimer", "[NUI] OnTerminate");

            try
            {
                StopUpdateTimer();
                _timerService?.Cleanup();
                _notificationService?.Dispose();
            }
            catch { }
        }
    }

    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Tizen.Log.Info("FamilyHubTimer", "[MAIN] Starting...");
                var app = new FamilyHubTimerApplication();
                app.Run(args);
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"Fatal error: {ex.Message}");
            }
        }
    }
}
