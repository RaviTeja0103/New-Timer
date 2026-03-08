using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FamilyHubTimer.Models;
using FamilyHubTimer.Services;

namespace FamilyHubTimer.Services
{
    /// <summary>
    /// Manages collection of timers and their lifecycle
    /// </summary>
    public class TimerService
    {
        private readonly List<TimerModel> _timers;
        private readonly PersistenceService _persistenceService;
        private readonly NotificationService _notificationService;
        private System.Threading.Timer _tickTimer;
        private bool _isRunning;

        // Events
        public event EventHandler<TimerModel> TimerStarted;
        public event EventHandler<TimerModel> TimerPaused;
        public event EventHandler<TimerModel> TimerResumed;
        public event EventHandler<TimerModel> TimerFinished;
        public event EventHandler<TimerModel> TimerRemoved;
        public event EventHandler<TimerModel> TimerUpdated;
        public event EventHandler<TimerModel> TimerAdded;

        public TimerService()
        {
            _timers = new List<TimerModel>();
            _persistenceService = new PersistenceService();
            _notificationService = new NotificationService();
            _isRunning = false;
        }

        /// <summary>
        /// Initializes the timer service and loads saved timers
        /// </summary>
        public void Initialize()
        {
            try
            {
                var savedTimers = _persistenceService.LoadTimers();
                _timers.AddRange(savedTimers);
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"Failed to initialize timer service: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all active timers
        /// </summary>
        public List<TimerModel> GetAllTimers()
        {
            return new List<TimerModel>(_timers);
        }

        /// <summary>
        /// Gets a timer by ID
        /// </summary>
        public TimerModel GetTimer(string id)
        {
            return _timers.FirstOrDefault(t => t.Id == id);
        }

        /// <summary>
        /// Creates and adds a new timer
        /// </summary>
        public TimerModel CreateTimer(int hours, int minutes, int seconds, string name = "Timer")
        {
            var timer = new TimerModel
            {
                Name = name,
                TotalSeconds = hours * 3600 + minutes * 60 + seconds,
                RemainingSeconds = hours * 3600 + minutes * 60 + seconds
            };

            _timers.Add(timer);
            TimerAdded?.Invoke(this, timer);
            SaveTimers();
            return timer;
        }

        /// <summary>
        /// Starts a timer
        /// </summary>
        public void StartTimer(string timerId)
        {
            var timer = GetTimer(timerId);
            if (timer != null && timer.State == TimerState.Idle)
            {
                timer.State = TimerState.Running;
                timer.StartedAt = DateTime.Now;
                
                if (!_isRunning)
                {
                    StartTickTimer();
                }

                TimerStarted?.Invoke(this, timer);
                SaveTimers();
            }
        }

        /// <summary>
        /// Pauses a timer
        /// </summary>
        public void PauseTimer(string timerId)
        {
            var timer = GetTimer(timerId);
            if (timer != null && timer.State == TimerState.Running)
            {
                timer.State = TimerState.Paused;
                timer.PausedAt = DateTime.Now;
                
                TimerPaused?.Invoke(this, timer);
                SaveTimers();
            }
        }

        /// <summary>
        /// Resumes a paused timer
        /// </summary>
        public void ResumeTimer(string timerId)
        {
            var timer = GetTimer(timerId);
            if (timer != null && timer.State == TimerState.Paused)
            {
                timer.State = TimerState.Running;
                timer.StartedAt = DateTime.Now;
                
                if (!_isRunning)
                {
                    StartTickTimer();
                }

                TimerResumed?.Invoke(this, timer);
                SaveTimers();
            }
        }

        /// <summary>
        /// Resets a timer to its original duration
        /// </summary>
        public void ResetTimer(string timerId)
        {
            var timer = GetTimer(timerId);
            if (timer != null)
            {
                timer.RemainingSeconds = timer.TotalSeconds;
                timer.State = TimerState.Idle;
                timer.StartedAt = null;
                timer.PausedAt = null;
                timer.CompletedAt = null;
                
                TimerUpdated?.Invoke(this, timer);
                SaveTimers();
            }
        }

        /// <summary>
        /// Removes a timer
        /// </summary>
        public void RemoveTimer(string timerId)
        {
            var timer = GetTimer(timerId);
            if (timer != null)
            {
                _timers.Remove(timer);
                TimerRemoved?.Invoke(this, timer);
                SaveTimers();
            }
        }

        /// <summary>
        /// Starts the internal tick timer that advances all running timers
        /// </summary>
        private void StartTickTimer()
        {
            if (_isRunning)
                return;

            _isRunning = true;
            _tickTimer = new System.Threading.Timer(
                callback: TickCallback,
                state: null,
                dueTime: TimeSpan.FromMilliseconds(1000),
                period: TimeSpan.FromMilliseconds(1000)
            );
        }

        /// <summary>
        /// Callback for the tick timer
        /// </summary>
        private void TickCallback(object state)
        {
            try
            {
                var runningTimers = _timers.Where(t => t.State == TimerState.Running).ToList();
                bool hasRunningTimers = false;

                foreach (var timer in runningTimers)
                {
                    timer.Tick();
                    TimerUpdated?.Invoke(this, timer);

                    if (timer.IsFinished())
                    {
                        _notificationService.PlayTimerAlert();
                        TimerFinished?.Invoke(this, timer);
                    }
                    else if (timer.State == TimerState.Running)
                    {
                        hasRunningTimers = true;
                    }
                }

                if (!hasRunningTimers)
                {
                    StopTickTimer();
                }

                SaveTimers();
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"Error in tick callback: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops the internal tick timer
        /// </summary>
        private void StopTickTimer()
        {
            if (_tickTimer != null)
            {
                _tickTimer.Dispose();
                _tickTimer = null;
            }
            _isRunning = false;
        }

        /// <summary>
        /// Saves all timers to persistent storage
        /// </summary>
        public void SaveTimers()
        {
            try
            {
                _persistenceService.SaveTimers(_timers);
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"Failed to save timers: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets count of running timers
        /// </summary>
        public int GetRunningTimerCount()
        {
            return _timers.Count(t => t.State == TimerState.Running);
        }

        /// <summary>
        /// Called when app is paused (backgrounded) - saves state
        /// </summary>
        public void Pause()
        {
            try
            {
                Tizen.Log.Info("FamilyHubTimer", "[TIZEN] TimerService paused - saving state");
                SaveTimers();
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[TIZEN] Pause failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when app resumes (brought to foreground) - restores state
        /// </summary>
        public void Resume()
        {
            try
            {
                Tizen.Log.Info("FamilyHubTimer", "[TIZEN] TimerService resumed");
                
                // If there are running timers and tick timer is not running, restart it
                if (GetRunningTimerCount() > 0 && !_isRunning)
                {
                    StartTickTimer();
                    Tizen.Log.Info("FamilyHubTimer", "[TIZEN] Tick timer restarted");
                }
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[TIZEN] Resume failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up resources
        /// </summary>
        public void Cleanup()
        {
            StopTickTimer();
            _notificationService?.Dispose();
            SaveTimers();
        }
    }
}
