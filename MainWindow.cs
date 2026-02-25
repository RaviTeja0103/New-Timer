using System;
using System.Collections.Generic;
using ElmSharp;
using Tizen.Applications;
using FamilyHubTimer.Services;
using FamilyHubTimer.Utils;
using FamilyHubTimer.Models;

namespace FamilyHubTimer
{
    /// <summary>
    /// Main Tizen Native Application for Family Hub Timer
    /// Uses ElmSharp for UI components
    /// </summary>
    public class FamilyHubTimerApplication : CoreUIApplication
    {
        private TimerService _timerService;
        private NotificationService _notificationService;
        private Window _mainWindow;
        
        private Conformant _conformant;
        private Box _mainBox;
        
        // UI state
        private enum ViewState { Setup, Running, List }
        private ViewState _currentView;
        
        // Setup UI components
        private Label _hoursLabel, _minutesLabel, _secondsLabel;
        private int _setupHours, _setupMinutes, _setupSeconds;
        
        // Running UI components
        private Label _runningTimerLabel, _runningTimeDisplay;
        private TimerModel _currentRunningTimer;
        private System.Threading.Timer _displayUpdateTimer;

        /// <summary>
        /// Application entry point
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();
            
            try
            {
                Tizen.Log.Info("FamilyHubTimer", "[MAIN] Application OnCreate");
                
                // Initialize services
                _timerService = new TimerService();
                _notificationService = new NotificationService();
                _timerService.Initialize();
                _notificationService.Initialize();
                
                // Create main window
                _mainWindow = new Window("FamilyHubTimer");
                _mainWindow.BackgroundColor = ElmSharp.Color.FromHex("#0d0d0d");

                // Create conformant for proper sizing
                _conformant = new Conformant(_mainWindow);
                _conformant.Show();

                // Create main box
                _mainBox = new Box(_mainWindow);
                _mainBox.Show();
                _conformant.SetContent(_mainBox);

                // Subscribe to events  
                SubscribeToEvents();

                // Show setup view initially (or list if timers exist)
                var existingTimers = _timerService.GetAllTimers();
                if (existingTimers.Count > 0)
                {
                    ShowTimerListView();
                }
                else
                {
                    ShowSetupView();
                }

                _mainWindow.Show();
                
                Tizen.Log.Info("FamilyHubTimer", "[MAIN] Application created successfully");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[MAIN] OnCreate failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Show the timer setup view
        /// </summary>
        private void ShowSetupView()
        {
            try
            {
                _mainBox.Clear();
                _currentView = ViewState.Setup;
                _setupHours = 0;
                _setupMinutes = 1;
                _setupSeconds = 0;

                // Title
                var titleLabel = new Label(_mainWindow);
                titleLabel.Text = "<b>Set Timer</b>";
                titleLabel.TextStyle = "title";
                titleLabel.Show();
                _mainBox.PackEnd(titleLabel);

                // Time pickers (horizontal layout)
                var pickerBox = new Box(_mainWindow);
                pickerBox.IsHorizontal = true;
                pickerBox.Show();

                // Hours
                CreateTimeSelector(pickerBox, "Hours", _setupHours, (v) => _setupHours = v, ref _hoursLabel);

                // Minutes
                CreateTimeSelector(pickerBox, "Minutes", _setupMinutes, (v) => _setupMinutes = v, ref _minutesLabel);

                // Seconds
                CreateTimeSelector(pickerBox, "Seconds", _setupSeconds, (v) => _setupSeconds = v, ref _secondsLabel);

                _mainBox.PackEnd(pickerBox);

                // Preset buttons
                var presetBox = new Box(_mainWindow);
                presetBox.IsHorizontal = true;
                presetBox.Show();

                CreatePresetButton(presetBox, "00:10:00", 600);
                CreatePresetButton(presetBox, "00:15:00", 900);
                CreatePresetButton(presetBox, "00:30:00", 1800);

                _mainBox.PackEnd(presetBox);

                // Start button
                var startBtn = new Button(_mainWindow);
                startBtn.Text = "START";
                startBtn.Clicked += (s, e) => StartTimer();
                startBtn.Show();
                _mainBox.PackEnd(startBtn);

                Tizen.Log.Info("FamilyHubTimer", "[UI] Setup view shown");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[UI] ShowSetupView failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a time selector column
        /// </summary>
        private void CreateTimeSelector(Box parentBox, string label, int initialValue, Action<int> onChanged, ref Label displayLabel)
        {
            try
            {
                var col = new Box(_mainWindow);
                col.IsHorizontal = false;
                col.WeightX = 1.0;
                col.Show();

                var lbl = new Label(_mainWindow);
                lbl.Text = label;
                lbl.Show();
                col.PackEnd(lbl);

                displayLabel = new Label(_mainWindow);
                displayLabel.Text = initialValue.ToString("D2");
                displayLabel.TextStyle = "title";
                displayLabel.Show();
                col.PackEnd(displayLabel);

                var btnBox = new Box(_mainWindow);
                btnBox.IsHorizontal = true;
                btnBox.Show();

                var upBtn = new Button(_mainWindow);
                upBtn.Text = "+";
                var label_ref = displayLabel;  // Capture for lambda
                upBtn.Clicked += (s, e) =>
                {
                    int val = int.Parse(label_ref.Text);
                    val = Math.Min(val + 1, 99);
                    label_ref.Text = val.ToString("D2");
                    onChanged(val);
                };
                upBtn.Show();
                btnBox.PackEnd(upBtn);

                var downBtn = new Button(_mainWindow);
                downBtn.Text = "-";
                var label_ref2 = displayLabel;  // Capture for lambda
                downBtn.Clicked += (s, e) =>
                {
                    int val = int.Parse(label_ref2.Text);
                    val = Math.Max(val - 1, 0);
                    label_ref2.Text = val.ToString("D2");
                    onChanged(val);
                };
                downBtn.Show();
                btnBox.PackEnd(downBtn);

                col.PackEnd(btnBox);
                parentBox.PackEnd(col);
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[UI] CreateTimeSelector failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a preset button
        /// </summary>
        private void CreatePresetButton(Box parentBox, string label, int seconds)
        {
            try
            {
                var btn = new Button(_mainWindow);
                btn.Text = label;
                btn.Clicked += (s, e) => SetPresetTime(seconds);
                btn.Show();
                parentBox.PackEnd(btn);
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[UI] CreatePresetButton failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Set preset time
        /// </summary>
        private void SetPresetTime(int totalSeconds)
        {
            _setupSeconds = totalSeconds % 60;
            _setupMinutes = (totalSeconds / 60) % 60;
            _setupHours = totalSeconds / 3600;

            _hoursLabel.Text = _setupHours.ToString("D2");
            _minutesLabel.Text = _setupMinutes.ToString("D2");
            _secondsLabel.Text = _setupSeconds.ToString("D2");
        }

        /// <summary>
        /// Start a timer
        /// </summary>
        private void StartTimer()
        {
            int total = _setupHours * 3600 + _setupMinutes * 60 + _setupSeconds;
            if (total <= 0) {
                return;
            }

            try
            {
                var timer = _timerService.CreateTimer(_setupHours, _setupMinutes, _setupSeconds, "Timer");
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
                _mainBox.Clear();
                _currentView = ViewState.Running;
                _currentRunningTimer = _timerService.GetTimer(timerId);

                if (_currentRunningTimer == null) return;

                // Title with timer name
                var titleBox = new Box(_mainWindow);
                titleBox.IsHorizontal = true;
                titleBox.Show();

                var nameLabel = new Label(_mainWindow);
                nameLabel.Text = _currentRunningTimer.Name;
                nameLabel.Show();
                titleBox.PackEnd(nameLabel);

                _mainBox.PackEnd(titleBox);

                // Timer display
                _runningTimeDisplay = new Label(_mainWindow);
                _runningTimeDisplay.Text = _currentRunningTimer.GetFormattedTime();
                _runningTimeDisplay.TextStyle = "title";
                _runningTimeDisplay.Show();
                _mainBox.PackEnd(_runningTimeDisplay);

                // Control buttons
                var btnBox = new Box(_mainWindow);
                btnBox.IsHorizontal = true;
                btnBox.Show();

                var pauseBtn = new Button(_mainWindow);
                pauseBtn.Text = "PAUSE";
                pauseBtn.Clicked += (s, e) => PauseTimer();
                pauseBtn.Show();
                btnBox.PackEnd(pauseBtn);

                var deleteBtn = new Button(_mainWindow);
                deleteBtn.Text = "DELETE";
                deleteBtn.Clicked += (s, e) => DeleteTimer();
                deleteBtn.Show();
                btnBox.PackEnd(deleteBtn);

                var backBtn = new Button(_mainWindow);
                backBtn.Text = "BACK";
                backBtn.Clicked += (s, e) => ShowTimerListView();
                backBtn.Show();
                btnBox.PackEnd(backBtn);

                _mainBox.PackEnd(btnBox);

                // Start update timer
                StartDisplayUpdate();

                Tizen.Log.Info("FamilyHubTimer", "[UI] Running view shown");
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
                _mainBox.Clear();
                _currentView = ViewState.List;

                // Title
                var titleLabel = new Label(_mainWindow);
                titleLabel.Text = "<b>Active Timers</b>";
                titleLabel.TextStyle = "title";
                titleLabel.Show();
                _mainBox.PackEnd(titleLabel);

                var timers = _timerService.GetAllTimers();
                
                if (timers.Count == 0)
                {
                    var emptyLabel = new Label(_mainWindow);
                    emptyLabel.Text = "No timers active";
                    emptyLabel.Show();
                    _mainBox.PackEnd(emptyLabel);
                }
                else
                {
                    foreach (var timer in timers)
                    {
                        CreateTimerItem(timer);
                    }
                }

                // Add new button
                var addBtn = new Button(_mainWindow);
                addBtn.Text = "+ ADD TIMER";
                addBtn.Clicked += (s, e) => ShowSetupView();
                addBtn.Show();
                _mainBox.PackEnd(addBtn);

                Tizen.Log.Info("FamilyHubTimer", "[UI] Timer list view shown");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[UI] ShowTimerListView failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a timer item in the list
        /// </summary>
        private void CreateTimerItem(TimerModel timer)
        {
            try
            {
                var itemBox = new Box(_mainWindow);
                itemBox.IsHorizontal = true;
                itemBox.Show();

                var infoLabel = new Label(_mainWindow);
                infoLabel.Text = $"{timer.Name} - {timer.GetFormattedTime()}";
                infoLabel.Show();
                itemBox.PackEnd(infoLabel);

                var viewBtn = new Button(_mainWindow);
                viewBtn.Text = "VIEW";
                viewBtn.Clicked += (s, e) => ShowRunningView(timer.Id);
                viewBtn.Show();
                itemBox.PackEnd(viewBtn);

                var delBtn = new Button(_mainWindow);
                delBtn.Text = "DEL";
                delBtn.Clicked += (s, e) => { _timerService.RemoveTimer(timer.Id); ShowTimerListView(); };
                delBtn.Show();
                itemBox.PackEnd(delBtn);

                _mainBox.PackEnd(itemBox);
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[UI] CreateTimerItem failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Pause/resume the current timer
        /// </summary>
        private void PauseTimer()
        {
            if (_currentRunningTimer == null) return;

            try
            {
                if (_currentRunningTimer.State == TimerState.Running)
                {
                    _timerService.PauseTimer(_currentRunningTimer.Id);
                }
                else if (_currentRunningTimer.State == TimerState.Paused)
                {
                    _timerService.ResumeTimer(_currentRunningTimer.Id);
                }
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[UI] PauseTimer failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete the current timer
        /// </summary>
        private void DeleteTimer()
        {
            if (_currentRunningTimer == null) return;

            try
            {
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
                (s) => UpdateRunningDisplay(),
                null,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(100)
            );
        }

        /// <summary>
        /// Update the running timer display
        /// </summary>
        private void UpdateRunningDisplay()
        {
            try
            {
                if (_currentRunningTimer == null || _runningTimeDisplay == null) return;

                _currentRunningTimer = _timerService.GetTimer(_currentRunningTimer.Id);
                if (_currentRunningTimer != null)
                {
                    // Use Tizen UI thread if available, otherwise update directly
                    try
                    {
                        if (_runningTimeDisplay != null)
                        {
                            _runningTimeDisplay.Text = _currentRunningTimer.GetFormattedTime();
                        }
                    }
                    catch { }
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
        /// Subscribe to timer service events
        /// </summary>
        private void SubscribeToEvents()
        {
            try
            {
                _timerService.TimerFinished += (s, timer) =>
                {
                    _notificationService?.PlayTimerAlert();
                    
                    try
                    {
                        if (_currentView == ViewState.Running && _currentRunningTimer?.Id == timer.Id)
                        {
                            // Refresh display to show timer finished state
                            ShowRunningView(timer.Id);
                        }
                    }
                    catch { }
                };

                _timerService.TimerRemoved += (s, timer) =>
                {
                    try
                    {
                        if (_timerService.GetAllTimers().Count == 0)
                        {
                            ShowSetupView();
                        }
                        else
                        {
                            ShowTimerListView();
                        }
                    }
                    catch { }
                };

                Tizen.Log.Info("FamilyHubTimer", "[MAIN] Event subscriptions registered");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[MAIN] Failed to subscribe to events: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when application is paused (sent to background)
        /// </summary>
        protected override void OnPause()
        {
            base.OnPause();
            Tizen.Log.Info("FamilyHubTimer", "[MAIN] Application paused");
            
            try
            {
                _timerService?.Pause();
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[MAIN] OnPause failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when application resumes (brought to foreground)
        /// </summary>
        protected override void OnResume()
        {
            base.OnResume();
            Tizen.Log.Info("FamilyHubTimer", "[MAIN] Application resumed");
            
            try
            {
                _timerService?.Resume();
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[MAIN] OnResume failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when application terminates
        /// </summary>
        protected override void OnTerminate()
        {
            base.OnTerminate();
            Tizen.Log.Info("FamilyHubTimer", "[MAIN] Application terminating");
            
            try
            {
                StopDisplayUpdate();
                _timerService?.Cleanup();
                _notificationService?.Dispose();
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[MAIN] OnTerminate failed: {ex.Message}");
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
                Tizen.Log.Info("FamilyHubTimer", "[BOOTSTRAP] Starting Family Hub Timer");
                
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
