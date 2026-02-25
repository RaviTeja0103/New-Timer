using System;
using Tizen.Applications;
using Tizen.NUI;
using FamilyHubTimer.Services;
using FamilyHubTimer.Views;
using FamilyHubTimer.Utils;

namespace FamilyHubTimer
{
    /// <summary>
    /// Main Tizen Native Application for Family Hub Timer
    /// Inherits from NUIApplication for proper Tizen lifecycle management
    /// </summary>
    public class FamilyHubTimerApplication : NUIApplication
    {
        private TimerService _timerService;
        private NotificationService _notificationService;

        /// <summary>
        /// Application entry point - called when NUI application starts
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();
            
            try
            {
                Tizen.Log.Info("FamilyHubTimer", "[TIZEN] OnCreate - Initializing Tizen NUI Application");
                
                // Initialize services
                _timerService = new TimerService();
                _notificationService = new NotificationService();
                
                // Initialize timer service
                _timerService.Initialize();
                _notificationService.Initialize();
                
                Tizen.Log.Info("FamilyHubTimer", "[TIZEN] Services initialized successfully");
                
                // Subscribe to events
                SubscribeToEvents();
                
                Tizen.Log.Info("FamilyHubTimer", "[TIZEN] Application created successfully");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[TIZEN] OnCreate failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Called when application resumes (brought to foreground)
        /// </summary>
        protected override void OnResume()
        {
            base.OnResume();
            Tizen.Log.Info("FamilyHubTimer", "[TIZEN] OnResume - Application resumed");
            
            try
            {
                // Resume timer service if needed
                if (_timerService != null)
                {
                    _timerService.Resume();
                }
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[TIZEN] OnResume failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when application pauses (sent to background)
        /// </summary>
        protected override void OnPause()
        {
            base.OnPause();
            Tizen.Log.Info("FamilyHubTimer", "[TIZEN] OnPause - Application paused");
            
            try
            {
                // Pause timer service and save state
                if (_timerService != null)
                {
                    _timerService.Pause();
                }
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[TIZEN] OnPause failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when application terminates
        /// </summary>
        protected override void OnTerminate()
        {
            base.OnTerminate();
            Tizen.Log.Info("FamilyHubTimer", "[TIZEN] OnTerminate - Application terminating");
            
            try
            {
                // Cleanup resources
                _timerService?.Cleanup();
                _notificationService?.Dispose();
                
                Tizen.Log.Info("FamilyHubTimer", "[TIZEN] Application terminated successfully");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[TIZEN] OnTerminate failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Subscribes to timer service events
        /// </summary>
        private void SubscribeToEvents()
        {
            try
            {
                _timerService.TimerStarted += (s, timer) =>
                {
                    Tizen.Log.Info("FamilyHubTimer", $"[EVENT] Timer started: {timer.Name}");
                };

                _timerService.TimerPaused += (s, timer) =>
                {
                    Tizen.Log.Info("FamilyHubTimer", $"[EVENT] Timer paused: {timer.Name}");
                };

                _timerService.TimerResumed += (s, timer) =>
                {
                    Tizen.Log.Info("FamilyHubTimer", $"[EVENT] Timer resumed: {timer.Name}");
                };

                _timerService.TimerFinished += (s, timer) =>
                {
                    Tizen.Log.Info("FamilyHubTimer", $"[EVENT] Timer finished: {timer.Name}");
                    
                    // Trigger Tizen system notifications
                    _notificationService?.PlayTimerAlert();
                };

                _timerService.TimerRemoved += (s, timer) =>
                {
                    Tizen.Log.Info("FamilyHubTimer", $"[EVENT] Timer removed: {timer.Name}");
                };

                _timerService.TimerUpdated += (s, timer) =>
                {
                    Tizen.Log.Debug("FamilyHubTimer", $"[EVENT] Timer updated: {timer.Name} - {timer.GetFormattedTime()}");
                };
                
                Tizen.Log.Info("FamilyHubTimer", "[TIZEN] Event subscriptions registered");
            }
            catch (Exception ex)
            {
                Tizen.Log.Error("FamilyHubTimer", $"[TIZEN] Failed to subscribe to events: {ex.Message}");
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
                Tizen.Log.Info("FamilyHubTimer", "[BOOTSTRAP] Starting Family Hub Timer (Tizen Native App)");
                
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
