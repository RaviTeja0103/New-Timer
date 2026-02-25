using Tizen.NUI;

namespace FamilyHubTimer.Utils
{
    /// <summary>
    /// Application-wide constants for colors, sizes, and UI configuration
    /// </summary>
    public static class AppConstants
    {
        // Color scheme
        public static readonly Color BackgroundColor = new Color(0.05f, 0.05f, 0.05f, 1.0f); // Dark background
        public static readonly Color PrimaryColor = new Color(0.31f, 0.46f, 0.92f, 1.0f); // Blue accent (RGB: 79, 117, 235)
        public static readonly Color SecondaryColor = new Color(0.2f, 0.2f, 0.2f, 1.0f); // Dark gray
        public static readonly Color TextColor = new Color(1.0f, 1.0f, 1.0f, 1.0f); // White text
        public static readonly Color TextLightColor = new Color(0.7f, 0.7f, 0.7f, 1.0f); // Light gray text
        public static readonly Color AlertColor = new Color(1.0f, 0.2f, 0.2f, 1.0f); // Red alert
        public static readonly Color SuccessColor = new Color(0.2f, 0.8f, 0.2f, 1.0f); // Green success

        // Screen dimensions - Family Hub 21.5" FHD (1920x1080)
        public const int ScreenWidth = 1920;
        public const int ScreenHeight = 1080;

        // UI Spacing and Sizing
        public const int LargeSpacing = 40;
        public const int MediumSpacing = 20;
        public const int SmallSpacing = 10;

        // Font sizes
        public const int TitleFontSize = 72;
        public const int LargeFontSize = 60;
        public const int MediumFontSize = 48;
        public const int RegularFontSize = 36;
        public const int SmallFontSize = 28;

        // Button sizing
        public const int ButtonHeight = 80;
        public const int ButtonWidth = 300;
        public const int SmallButtonWidth = 150;
        public const int SmallButtonHeight = 60;

        // Timer display sizing
        public const int CircularProgressSize = 500;
        public const int CircularProgressStrokeWidth = 12;

        // Animation duration (milliseconds)
        public const uint AnimationDuration = 300;
        public const uint TimerTickInterval = 100;

        // Timer limits
        public const int MaxHours = 99;
        public const int MaxMinutes = 59;
        public const int MaxSeconds = 59;

        // Persistent storage key
        public const string TimerDataKey = "family_hub_timer_data";
        public const string TimerHistoryKey = "family_hub_timer_history";
    }
}
