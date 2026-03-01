using System;
using System.Collections.Generic;
using System.Linq;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using FamilyHubTimer.Services;
using FamilyHubTimer.Models;

namespace FamilyHubTimer
{
    /// <summary>
    /// Main Tizen NUI Application for Family Hub Timer
    /// Simplified for Tizen.NUI 0.2.43 compatibility
    /// </summary>
    public class FamilyHubTimerApplication : NUIApplication
    {
        private TimerService _timerService;
        private NotificationService _notificationService;
        private Window _mainWindow;
        private View _rootView;
        private View _contentView;

        // UI state
        private enum ViewState { Setup, Running, List }
        private ViewState _currentView = ViewState.Setup;

        // Setup screen state
        private TextLabel _hoursDisplay, _minutesDisplay, _secondsDisplay;
        private int _setupHours = 0, _setupMinutes = 1, _setupSeconds = 0;

        // Running screen state
        private TextLabel _timerDisplayLabel;
        private TimerModel _currentRunningTimer;
        private System.Threading.Timer _displayUpdateTimer;

        private const int WINDOW_WIDTH = 1080;
        private const int WINDOW_HEIGHT = 1920;
        private const int BTN_HEIGHT = 120;
        private const int PADDING = 40;

        protected override void OnCreate()
        {
            base.OnCreate();

            try
            {
                Tizen.Log.Info("FamilyHubTimer", "[NUI] OnCreate started");

                // Initialize services
                _timerService = new TimerService();
                _notificationService = new NotificationService();
                _timerService.Initialize();
                _notificationService.Initialize();

                // Get main window
                _mainWindow = GetDefaultWindow();
                _mainWindow.BackgroundColor = new Color(0.05f, 0.05f, 0.05f, 1.0f);

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

                Tizen.Log.Info("FamilyHubTimer", "[NUI] OnCreate completed successfully");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[NUI] OnCreate error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void ShowSetupView()
        {
            try
            {
                ClearContent();
                _currentView = ViewState.Setup;

                _contentView = new View();
                _contentView.Size = new Size(WINDOW_WIDTH, WINDOW_HEIGHT);
                _contentView.BackgroundColor = new Color(0.05f, 0.05f, 0.05f, 1.0f);

                int yPos = 80;

                // Title
                var title = CreateLabel("SET TIMER", 60, yPos);
                title.HorizontalAlignment = HorizontalAlignment.Center;
                _contentView.Add(title);
                yPos += 120;

                // Time selectors
                yPos = CreateTimeEditor(_contentView, "HOURS", _setupHours, (v) => { _setupHours = v; _hoursDisplay.Text = v.ToString("D2"); }, yPos, _hoursDisplay);
                if (_hoursDisplay != null)
                {
                    var label = _hoursDisplay;
                    _hoursDisplay = label;
                }

                yPos = CreateTimeEditor(_contentView, "MINUTES", _setupMinutes, (v) => { _setupMinutes = v; _minutesDisplay.Text = v.ToString("D2"); }, yPos, _minutesDisplay);
                if (_minutesDisplay != null)
                {
                    var label = _minutesDisplay;
                    _minutesDisplay = label;
                }

                yPos = CreateTimeEditor(_contentView, "SECONDS", _setupSeconds, (v) => { _setupSeconds = v; _secondsDisplay.Text = v.ToString("D2"); }, yPos, _secondsDisplay);
                if (_secondsDisplay != null)
                {
                    var label = _secondsDisplay;
                    _secondsDisplay = label;
                }

                yPos += 40;

                // Preset buttons label
                var presetLabel = CreateLabel("Quick Set", 40, yPos);
                presetLabel.HorizontalAlignment = HorizontalAlignment.Center;
                _contentView.Add(presetLabel);
                yPos += 80;

                // Preset buttons
                var presetContainer = new View();
                presetContainer.Position = new Position(PADDING, yPos);
                presetContainer.Size = new Size(WINDOW_WIDTH - 2 * PADDING, BTN_HEIGHT);
                presetContainer.BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 1.0f);

                int btnWidth = (WINDOW_WIDTH - 3 * PADDING) / 4;
                int xPos = PADDING;

                CreatePresetQuickButton(presetContainer, "10s", 10, xPos, 0, btnWidth);
                xPos += btnWidth + 20;
                CreatePresetQuickButton(presetContainer, "5m", 300, xPos, 0, btnWidth);
                xPos += btnWidth + 20;
                CreatePresetQuickButton(presetContainer, "15m", 900, xPos, 0, btnWidth);
                xPos += btnWidth + 20;
                CreatePresetQuickButton(presetContainer, "30m", 1800, xPos, 0, btnWidth);

                _contentView.Add(presetContainer);
                yPos += BTN_HEIGHT + 60;

                // START button
                var startBtn = CreateButton("START TIMER", WINDOW_WIDTH - 2 * PADDING, BTN_HEIGHT + 20, yPos);
                startBtn.ClickEvent += (s, e) =>
                {
                    int total = _setupHours * 3600 + _setupMinutes * 60 + _setupSeconds;
                    if (total > 0) StartTimer();
                };
                _contentView.Add(startBtn);

                _rootView.Add(_contentView);
                Tizen.Log.Info("FamilyHubTimer", "[UI] Setup view shown");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[UI] ShowSetupView error: {ex.Message}");
            }
        }

        private int CreateTimeEditor(View parent, string label, int value, Action<int> onChanged, int yPos, TextLabel displayRef)
        {
            var container = new View();
            container.Position = new Position(PADDING, yPos);
            container.Size = new Size(250, 220);
            container.BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 1.0f);

            // Label
            var lbl = CreateLabel(label, 32, 20);
            lbl.HorizontalAlignment = HorizontalAlignment.Center;
            container.Add(lbl);

            // Value display
            var valueDisplay = CreateLabel(value.ToString("D2"), 90, 70);
            valueDisplay.HorizontalAlignment = HorizontalAlignment.Center;
            valueDisplay.TextColor = new Color(0.0f, 0.67f, 1.0f, 1.0f);
            container.Add(valueDisplay);
            
            if (label == "HOURS") _hoursDisplay = valueDisplay;
            else if (label == "MINUTES") _minutesDisplay = valueDisplay;
            else if (label == "SECONDS") _secondsDisplay = valueDisplay;

            // Buttons
            var upBtn = CreateButton("△", 100, 70, 170);
            upBtn.Position = new Position(20, 170);
            upBtn.ClickEvent += (s, e) =>
            {
                int max = (label == "HOURS") ? 23 : 59;
                int newVal = Math.Min(value + 1, max);
                onChanged(newVal);
                if (label == "HOURS") _setupHours = newVal;
                else if (label == "MINUTES") _setupMinutes = newVal;
                else if (label == "SECONDS") _setupSeconds = newVal;
            };
            container.Add(upBtn);

            var downBtn = CreateButton("▽", 100, 70, 170);
            downBtn.Position = new Position(130, 170);
            downBtn.ClickEvent += (s, e) =>
            {
                int newVal = Math.Max(value - 1, 0);
                onChanged(newVal);
                if (label == "HOURS") _setupHours = newVal;
                else if (label == "MINUTES") _setupMinutes = newVal;
                else if (label == "SECONDS") _setupSeconds = newVal;
            };
            container.Add(downBtn);

            parent.Add(container);
            return yPos + 270;
        }

        private void CreatePresetQuickButton(View parent, string label, int seconds, int xPos, int yPos, int width)
        {
            var btn = CreateButton(label, width - 10, BTN_HEIGHT - 20, yPos + 10);
            btn.Position = new Position(xPos, yPos + 10);
            btn.ClickEvent += (s, e) =>
            {
                _setupHours = seconds / 3600;
                _setupMinutes = (seconds % 3600) / 60;
                _setupSeconds = seconds % 60;

                if (_hoursDisplay != null) _hoursDisplay.Text = _setupHours.ToString("D2");
                if (_minutesDisplay != null) _minutesDisplay.Text = _setupMinutes.ToString("D2");
                if (_secondsDisplay != null) _secondsDisplay.Text = _setupSeconds.ToString("D2");
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
                ClearContent();
                _currentView = ViewState.Running;
                _currentRunningTimer = _timerService.GetTimer(timerId);

                if (_currentRunningTimer == null) return;

                _contentView = new View();
                _contentView.Size = new Size(WINDOW_WIDTH, WINDOW_HEIGHT);
                _contentView.BackgroundColor = new Color(0.05f, 0.05f, 0.05f, 1.0f);

                int yPos = 80;

                // Timer name
                var nameLabel = CreateLabel(_currentRunningTimer.Name, 50, yPos);
                nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
                _contentView.Add(nameLabel);
                yPos += 100;

                // Timer display
                _timerDisplayLabel = CreateLabel(_currentRunningTimer.GetFormattedTime(), 140, yPos);
                _timerDisplayLabel.HorizontalAlignment = HorizontalAlignment.Center;
                _timerDisplayLabel.TextColor = new Color(0.0f, 0.67f, 1.0f, 1.0f);
                _contentView.Add(_timerDisplayLabel);
                yPos += 180;

                // State label
                var stateLabel = CreateLabel(GetStateText(_currentRunningTimer.State), 36, yPos);
                stateLabel.HorizontalAlignment = HorizontalAlignment.Center;
                _contentView.Add(stateLabel);
                yPos += 100;

                // Buttons
                var pauseBtn = CreateButton(
                    _currentRunningTimer.State == TimerState.Running ? "PAUSE" : "RESUME",
                    WINDOW_WIDTH - 2 * PADDING, BTN_HEIGHT, yPos
                );
                pauseBtn.ClickEvent += (s, e) => TogglePause();
                _contentView.Add(pauseBtn);
                yPos += BTN_HEIGHT + 20;

                var resetBtn = CreateButton("RESET", WINDOW_WIDTH - 2 * PADDING, BTN_HEIGHT, yPos);
                resetBtn.ClickEvent += (s, e) => ResetTimer();
                _contentView.Add(resetBtn);
                yPos += BTN_HEIGHT + 20;

                var deleteBtn = CreateButton("DELETE", WINDOW_WIDTH - 2 * PADDING, BTN_HEIGHT, yPos);
                deleteBtn.ClickEvent += (s, e) => DeleteTimer();
                _contentView.Add(deleteBtn);
                yPos += BTN_HEIGHT + 20;

                var backBtn = CreateButton("BACK TO LIST", WINDOW_WIDTH - 2 * PADDING, BTN_HEIGHT, yPos);
                backBtn.ClickEvent += (s, e) => ShowTimerListView();
                _contentView.Add(backBtn);

                _rootView.Add(_contentView);
                StartDisplayUpdate();
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
                StopDisplayUpdate();
                ClearContent();
                _currentView = ViewState.List;

                _contentView = new View();
                _contentView.Size = new Size(WINDOW_WIDTH, WINDOW_HEIGHT);
                _contentView.BackgroundColor = new Color(0.05f, 0.05f, 0.05f, 1.0f);

                int yPos = 80;

                // Title
                var title = CreateLabel("ACTIVE TIMERS", 60, yPos);
                title.HorizontalAlignment = HorizontalAlignment.Center;
                _contentView.Add(title);
                yPos += 120;

                var timers = _timerService.GetAllTimers();

                if (timers.Count == 0)
                {
                    var emptyLabel = CreateLabel("No active timers", 48, 400);
                    emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
                    _contentView.Add(emptyLabel);
                }
                else
                {
                    foreach (var timer in timers)
                    {
                        CreateTimerListItem(_contentView, timer, ref yPos);
                        yPos += 200;
                    }
                }

                yPos = WINDOW_HEIGHT - 180;

                // Add timer button
                var addBtn = CreateButton("+ ADD NEW TIMER", WINDOW_WIDTH - 2 * PADDING, BTN_HEIGHT + 20, yPos);
                addBtn.ClickEvent += (s, e) => ShowSetupView();
                _contentView.Add(addBtn);

                _rootView.Add(_contentView);
                Tizen.Log.Info("FamilyHubTimer", "[UI] Timer list view shown");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"ShowTimerListView error: {ex.Message}");
            }
        }

        private void CreateTimerListItem(View parent, TimerModel timer, ref int yPos)
        {
            try
            {
                var itemBg = new View();
                itemBg.Position = new Position(PADDING, yPos);
                itemBg.Size = new Size(WINDOW_WIDTH - 2 * PADDING, 180);
                itemBg.BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 1.0f);

                // Timer name
                var nameLabel = CreateLabel(timer.Name, 40, 20);
                nameLabel.Position = new Position(20, 20);
                itemBg.Add(nameLabel);

                // Timer time
                var timeLabel = CreateLabel(timer.GetFormattedTime(), 50, 70);
                timeLabel.Position = new Position(20, 70);
                timeLabel.TextColor = new Color(0.0f, 0.67f, 1.0f, 1.0f);
                itemBg.Add(timeLabel);

                // Timer state
                var stateLabel = CreateLabel(GetStateText(timer.State), 32, 140);
                stateLabel.Position = new Position(20, 140);
                itemBg.Add(stateLabel);

                // View button
                var viewBtn = CreateButton("VIEW", 120, 80, 0);
                viewBtn.Position = new Position(WINDOW_WIDTH - 2 * PADDING - 260, 50);
                viewBtn.ClickEvent += (s, e) => ShowRunningView(timer.Id);
                itemBg.Add(viewBtn);

                // Delete button
                var delBtn = CreateButton("DELETE", 120, 80, 0);
                delBtn.Position = new Position(WINDOW_WIDTH - 2 * PADDING - 130, 50);
                delBtn.ClickEvent += (s, e) =>
                {
                    _timerService.RemoveTimer(timer.Id);
                    ShowTimerListView();
                };
                itemBg.Add(delBtn);

                parent.Add(itemBg);
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"CreateTimerListItem error: {ex.Message}");
            }
        }

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

                ShowRunningView(_currentRunningTimer.Id);
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"TogglePause error: {ex.Message}");
            }
        }

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
                Tizen.Log.Error("FamilyHubTimer", $"ResetTimer error: {ex.Message}");
            }
        }

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
                Tizen.Log.Error("FamilyHubTimer", $"DeleteTimer error: {ex.Message}");
            }
        }

        private void StartDisplayUpdate()
        {
            StopDisplayUpdate();
            _displayUpdateTimer = new System.Threading.Timer(
                (s) => UpdateDisplay(),
                null,
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500)
            );
        }

        private void UpdateDisplay()
        {
            try
            {
                if (_currentRunningTimer == null) return;

                _currentRunningTimer = _timerService.GetTimer(_currentRunningTimer.Id);
                if (_currentRunningTimer != null && _timerDisplayLabel != null)
                {
                    _timerDisplayLabel.Text = _currentRunningTimer.GetFormattedTime();
                }

                if (_currentRunningTimer?.State == TimerState.Finished)
                {
                    StopDisplayUpdate();
                    _notificationService?.PlayTimerAlert();
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ShowRunningView(_currentRunningTimer.Id);
                    });
                }
            }
            catch { }
        }

        private void StopDisplayUpdate()
        {
            if (_displayUpdateTimer != null)
            {
                _displayUpdateTimer.Dispose();
                _displayUpdateTimer = null;
            }
        }

        private TextLabel CreateLabel(string text, int fontSize, int yPos)
        {
            var label = new TextLabel();
            label.Text = text;
            label.PointSize = (uint)fontSize;
            label.TextColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            label.Position = new Position(PADDING, yPos);
            label.Size = new Size(WINDOW_WIDTH - 2 * PADDING, 100);
            return label;
        }

        private TextLabel CreateButton(string text, int width, int height, int yPos)
        {
            var btn = new TextLabel();
            btn.Text = text;
            btn.PointSize = 40;
            btn.TextColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            btn.BackgroundColor = new Color(0.0f, 0.67f, 1.0f, 1.0f);
            btn.HorizontalAlignment = HorizontalAlignment.Center;
            btn.VerticalAlignment = VerticalAlignment.Center;
            btn.Position = new Position(PADDING, yPos);
            btn.Size = new Size(width, height);
            btn.TouchEvent += (s, e) => { return true; };
            return btn;
        }

        private void ClearContent()
        {
            if (_contentView != null)
            {
                _rootView.Remove(_contentView);
                _contentView.Dispose();
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
                StopDisplayUpdate();
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"OnPause error: {ex.Message}");
            }
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
                    StartDisplayUpdate();
                }
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"OnResume error: {ex.Message}");
            }
        }

        protected override void OnTerminate()
        {
            base.OnTerminate();
            Tizen.Log.Info("FamilyHubTimer", "[NUI] OnTerminate");

            try
            {
                StopDisplayUpdate();
                _timerService?.Cleanup();
                _notificationService?.Dispose();
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"OnTerminate error: {ex.Message}");
            }
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
