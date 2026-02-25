using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using Tizen.Applications;
using FamilyHubTimer.Models;
using FamilyHubTimer.Utils;

namespace FamilyHubTimer.Services
{
    /// <summary>
    /// Handles persistence of timer data to Tizen preference storage
    /// </summary>
    public class PersistenceService
    {
        private readonly string _dataDirectory;
        private const string TimerDataFileName = "timers.json";
        private const string TimerHistoryFileName = "timer_history.json";

        public PersistenceService()
        {
            _dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FamilyHubTimer"
            );

            try
            {
                if (!Directory.Exists(_dataDirectory))
                {
                    Directory.CreateDirectory(_dataDirectory);
                }
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"Failed to create data directory: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves active timers to persistent storage
        /// </summary>
        public void SaveTimers(List<TimerModel> timers)
        {
            try
            {
                string filePath = Path.Combine(_dataDirectory, TimerDataFileName);
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(timers, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"Failed to save timers: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads timers from persistent storage
        /// </summary>
        public List<TimerModel> LoadTimers()
        {
            try
            {
                string filePath = Path.Combine(_dataDirectory, TimerDataFileName);
                if (!File.Exists(filePath))
                    return new List<TimerModel>();

                string json = File.ReadAllText(filePath);
                var timers = JsonSerializer.Deserialize<List<TimerModel>>(json);
                return timers ?? new List<TimerModel>();
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"Failed to load timers: {ex.Message}");
                return new List<TimerModel>();
            }
        }

        /// <summary>
        /// Saves timer history for audit/analytics
        /// </summary>
        public void SaveTimerHistory(List<TimerHistoryEntry> history)
        {
            try
            {
                string filePath = Path.Combine(_dataDirectory, TimerHistoryFileName);
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(history, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"Failed to save timer history: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads timer history from persistent storage
        /// </summary>
        public List<TimerHistoryEntry> LoadTimerHistory()
        {
            try
            {
                string filePath = Path.Combine(_dataDirectory, TimerHistoryFileName);
                if (!File.Exists(filePath))
                    return new List<TimerHistoryEntry>();

                string json = File.ReadAllText(filePath);
                var history = JsonSerializer.Deserialize<List<TimerHistoryEntry>>(json);
                return history ?? new List<TimerHistoryEntry>();
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"Failed to load timer history: {ex.Message}");
                return new List<TimerHistoryEntry>();
            }
        }

        /// <summary>
        /// Clears all stored data
        /// </summary>
        public void ClearAllData()
        {
            try
            {
                string timerFile = Path.Combine(_dataDirectory, TimerDataFileName);
                string historyFile = Path.Combine(_dataDirectory, TimerHistoryFileName);

                if (File.Exists(timerFile))
                    File.Delete(timerFile);
                if (File.Exists(historyFile))
                    File.Delete(historyFile);
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"Failed to clear data: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Represents a historical timer completion entry
    /// </summary>
    public class TimerHistoryEntry
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int DurationSeconds { get; set; }
        public DateTime CompletedAt { get; set; }
        public TimeSpan ElapsedTime { get; set; }
    }
}
