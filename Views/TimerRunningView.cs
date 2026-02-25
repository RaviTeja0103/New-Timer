using System;
using FamilyHubTimer.Utils;
using FamilyHubTimer.Services;
using FamilyHubTimer.Models;

namespace FamilyHubTimer.Views
{
    /// <summary>
    /// View displaying a single running timer with large circular progress indicator
    /// </summary>
    public class TimerRunningView
    {
        private TimerService _timerService;
        private TimerModel _currentTimer;

        public TimerRunningView(TimerService timerService)
        {
            _timerService = timerService;
            SubscribeToEvents();
        }

        /// <summary>
        /// Sets the current timer to display
        /// </summary>
        public void DisplayTimer(string timerId)
        {
            _currentTimer = _timerService.GetTimer(timerId);
            if (_currentTimer != null)
            {
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Updates the display with current timer state
        /// </summary>
        private void UpdateDisplay()
        {
            if (_currentTimer == null)
                return;

            Tizen.Log.Info("FamilyHubTimer", $"Displaying timer: {_currentTimer.Name} - {_currentTimer.GetFormattedTime()}");

            // Update progress
            float progress = _currentTimer.GetProgressPercentage();
            Tizen.Log.Debug("FamilyHubTimer", $"Timer progress: {progress}%");

            // Update button states based on state
            if (_currentTimer.State == TimerState.Running)
            {
                Tizen.Log.Debug("FamilyHubTimer", "Timer is running");
            }
            else if (_currentTimer.State == TimerState.Paused)
            {
                Tizen.Log.Debug("FamilyHubTimer", "Timer is paused");
            }
            else if (_currentTimer.State == TimerState.Finished)
            {
                ShowFinishedAlert();
            }
        }

        /// <summary>
        /// Subscribes to timer service events for updates
        /// </summary>
        private void SubscribeToEvents()
        {
            _timerService.TimerUpdated += (s, timer) =>
            {
                if (_currentTimer != null && timer.Id == _currentTimer.Id)
                {
                    _currentTimer = timer;
                    UpdateDisplay();
                }
            };

            _timerService.TimerFinished += (s, timer) =>
            {
                if (_currentTimer != null && timer.Id == _currentTimer.Id)
                {
                    _currentTimer = timer;
                    UpdateDisplay();
                }
            };
        }

        /// <summary>
        /// Shows an alert when timer finishes
        /// </summary>
        private void ShowFinishedAlert()
        {
            Tizen.Log.Info("FamilyHubTimer", "Timer finished!");
        }

        /// <summary>
        /// Pauses the current timer
        /// </summary>
        public void PauseTimer()
        {
            if (_currentTimer != null && _currentTimer.State == TimerState.Running)
            {
                _timerService.PauseTimer(_currentTimer.Id);
            }
        }

        /// <summary>
        /// Resumes the current timer
        /// </summary>
        public void ResumeTimer()
        {
            if (_currentTimer != null && _currentTimer.State == TimerState.Paused)
            {
                _timerService.ResumeTimer(_currentTimer.Id);
            }
        }

        /// <summary>
        /// Resets the current timer
        /// </summary>
        public void ResetTimer()
        {
            if (_currentTimer != null)
            {
                _timerService.ResetTimer(_currentTimer.Id);
            }
        }

        /// <summary>
        /// Gets the root view (placeholder for Tizen NUI integration)
        /// </summary>
        public object GetView()
        {
            return new object(); // Placeholder
        }

        /// <summary>
        /// Gets the current timer
        /// </summary>
        public TimerModel GetCurrentTimer()
        {
            return _currentTimer;
        }
    }
}
