using System;
using System.Collections.Generic;

namespace FamilyHubTimer.Models
{
    /// <summary>
    /// Represents a single timer instance with state management
    /// </summary>
    public class TimerModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int TotalSeconds { get; set; }
        public int RemainingSeconds { get; set; }
        public TimerState State { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? PausedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Initializes a new timer instance
        /// </summary>
        public TimerModel()
        {
            Id = Guid.NewGuid().ToString();
            Name = "Timer";
            State = TimerState.Idle;
            CreatedAt = DateTime.Now;
            RemainingSeconds = 0;
            TotalSeconds = 0;
        }

        /// <summary>
        /// Gets the elapsed time percentage (0-100)
        /// </summary>
        public float GetProgressPercentage()
        {
            if (TotalSeconds <= 0)
                return 0;
            
            float elapsed = TotalSeconds - RemainingSeconds;
            return (elapsed / TotalSeconds) * 100f;
        }

        /// <summary>
        /// Formats the remaining time as HH:MM:SS
        /// </summary>
        public string GetFormattedTime()
        {
            int hours = RemainingSeconds / 3600;
            int minutes = (RemainingSeconds % 3600) / 60;
            int seconds = RemainingSeconds % 60;
            
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }

        /// <summary>
        /// Formats the total time as HH:MM:SS
        /// </summary>
        public string GetFormattedTotalTime()
        {
            int hours = TotalSeconds / 3600;
            int minutes = (TotalSeconds % 3600) / 60;
            int seconds = TotalSeconds % 60;
            
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }

        /// <summary>
        /// Checks if the timer has finished counting down
        /// </summary>
        public bool IsFinished()
        {
            return RemainingSeconds <= 0 && State != TimerState.Idle;
        }

        /// <summary>
        /// Decrements the remaining time by one second
        /// </summary>
        public void Tick()
        {
            if (RemainingSeconds > 0)
            {
                RemainingSeconds--;
            }
            else if (RemainingSeconds == 0 && State == TimerState.Running)
            {
                State = TimerState.Finished;
                CompletedAt = DateTime.Now;
            }
        }
    }

    /// <summary>
    /// Enum representing possible timer states
    /// </summary>
    public enum TimerState
    {
        Idle,      // Not started
        Running,   // Currently counting down
        Paused,    // Paused by user
        Finished   // Countdown complete
    }
}
