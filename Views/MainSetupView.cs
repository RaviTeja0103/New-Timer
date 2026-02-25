using System;
using FamilyHubTimer.Utils;
using FamilyHubTimer.Services;

namespace FamilyHubTimer.Views
{
    /// <summary>
    /// View for setting up a new timer with hours, minutes, and seconds selection
    /// </summary>
    public class MainSetupView
    {
        private TimerService _timerService;
        private int _selectedHours;
        private int _selectedMinutes;
        private int _selectedSeconds;

        public MainSetupView(TimerService timerService)
        {
            _timerService = timerService;
            _selectedHours = 0;
            _selectedMinutes = 0;
            _selectedSeconds = 0;
        }

        /// <summary>
        /// Adjusts the time value for a specific component
        /// </summary>
        public void AdjustTime(int component, int delta, int maxValue)
        {
            if (component == 0) // Hours
            {
                _selectedHours = Math.Max(0, Math.Min(AppConstants.MaxHours, _selectedHours + delta));
            }
            else if (component == 1) // Minutes
            {
                _selectedMinutes = Math.Max(0, Math.Min(AppConstants.MaxMinutes, _selectedMinutes + delta));
            }
            else if (component == 2) // Seconds
            {
                _selectedSeconds = Math.Max(0, Math.Min(AppConstants.MaxSeconds, _selectedSeconds + delta));
            }
        }

        /// <summary>
        /// Sets preset time values
        /// </summary>
        public void SetPresetTime(int seconds)
        {
            _selectedHours = 0;
            _selectedMinutes = 0;
            _selectedSeconds = seconds;
        }

        /// <summary>
        /// Starts a timer with current selections
        /// </summary>
        public void StartTimer()
        {
            int totalSeconds = _selectedHours * 3600 + _selectedMinutes * 60 + _selectedSeconds;

            if (totalSeconds <= 0)
            {
                Tizen.Log.Info("FamilyHubTimer", "Timer duration is 0 or negative");
                return;
            }

            var timer = _timerService.CreateTimer(_selectedHours, _selectedMinutes, _selectedSeconds);
            _timerService.StartTimer(timer.Id);

            Tizen.Log.Info("FamilyHubTimer", $"Timer created and started: {timer.Id}");
        }

        /// <summary>
        /// Gets the root view (placeholder for Tizen NUI integration)
        /// </summary>
        public object GetView()
        {
            return new object(); // Placeholder
        }

        /// <summary>
        /// Gets selected hours
        /// </summary>
        public int GetSelectedHours() => _selectedHours;

        /// <summary>
        /// Gets selected minutes
        /// </summary>
        public int GetSelectedMinutes() => _selectedMinutes;

        /// <summary>
        /// Gets selected seconds
        /// </summary>
        public int GetSelectedSeconds() => _selectedSeconds;
    }
}
