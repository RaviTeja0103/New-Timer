using System;
using System.Collections.Generic;
using FamilyHubTimer.Utils;
using FamilyHubTimer.Services;
using FamilyHubTimer.Models;

namespace FamilyHubTimer.Views
{
    /// <summary>
    /// View displaying a list of all active timers with controls
    /// </summary>
    public class TimerListView
    {
        private TimerService _timerService;
        private Dictionary<string, object> _timerViews;

        public TimerListView(TimerService timerService)
        {
            _timerService = timerService;
            _timerViews = new Dictionary<string, object>();
            SubscribeToEvents();
        }

        /// <summary>
        /// Refreshes the list of displayed timers
        /// </summary>
        public void RefreshTimerList()
        {
            _timerViews.Clear();

            var timers = _timerService.GetAllTimers();
            foreach (var timer in timers)
            {
                _timerViews[timer.Id] = new object(); // Placeholder for Tizen NUI component
            }

            Tizen.Log.Info("FamilyHubTimer", $"Timer list refreshed with {timers.Count} timers");
        }

        /// <summary>
        /// Pauses or resumes a timer
        /// </summary>
        public void TogglePauseTimer(string timerId)
        {
            var timer = _timerService.GetTimer(timerId);
            if (timer != null)
            {
                if (timer.State == TimerState.Running)
                {
                    _timerService.PauseTimer(timerId);
                }
                else if (timer.State == TimerState.Paused)
                {
                    _timerService.ResumeTimer(timerId);
                }
            }
        }

        /// <summary>
        /// Resets a timer
        /// </summary>
        public void ResetTimer(string timerId)
        {
            _timerService.ResetTimer(timerId);
        }

        /// <summary>
        /// Removes a timer
        /// </summary>
        public void RemoveTimer(string timerId)
        {
            _timerService.RemoveTimer(timerId);
        }

        /// <summary>
        /// Subscribes to timer service events
        /// </summary>
        private void SubscribeToEvents()
        {
            _timerService.TimerUpdated += (s, timer) =>
            {
                Tizen.Log.Debug("FamilyHubTimer", $"Timer updated: {timer.Id}");
            };

            _timerService.TimerRemoved += (s, timer) =>
            {
                RefreshTimerList();
            };

            _timerService.TimerStarted += (s, timer) =>
            {
                RefreshTimerList();
            };
        }

        /// <summary>
        /// Handles adding a new timer
        /// </summary>
        public void AddNewTimer()
        {
            Tizen.Log.Info("FamilyHubTimer", "Adding new timer");
        }

        /// <summary>
        /// Gets the root view (placeholder for Tizen NUI integration)
        /// </summary>
        public object GetView()
        {
            RefreshTimerList();
            return new object(); // Placeholder
        }

        /// <summary>
        /// Gets all active timers
        /// </summary>
        public List<TimerModel> GetTimers()
        {
            return _timerService.GetAllTimers();
        }
    }
}
