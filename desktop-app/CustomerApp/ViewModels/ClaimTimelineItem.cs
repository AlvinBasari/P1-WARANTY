using CommunityToolkit.Mvvm.ComponentModel;

namespace CustomerApp.ViewModels
{
    public partial class ClaimTimelineItem : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCompleted))]
        [NotifyPropertyChangedFor(nameof(IsActive))]
        [NotifyPropertyChangedFor(nameof(IsPending))]
        [NotifyPropertyChangedFor(nameof(BorderColorHex))]
        [NotifyPropertyChangedFor(nameof(BackgroundColorHex))]
        [NotifyPropertyChangedFor(nameof(TextColorHex))]
        [NotifyPropertyChangedFor(nameof(BadgeBgHex))]
        private string _statusState = "pending"; // "completed", "active", "pending"

        [ObservableProperty]
        private int _stepIndex;

        [ObservableProperty]
        private string _stepNumber = "1";

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _subtitle = string.Empty;

        [ObservableProperty]
        private string _timestamp = string.Empty;

        [ObservableProperty]
        private string _badgeText = string.Empty;

        public bool IsCompleted => StatusState == "completed";
        public bool IsActive => StatusState == "active";
        public bool IsPending => StatusState == "pending";

        private static bool IsLightMode =>
            Avalonia.Application.Current?.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Light
            || Avalonia.Application.Current?.RequestedThemeVariant == Avalonia.Styling.ThemeVariant.Light;

        public string BorderColorHex => IsCompleted
            ? "#22C55E"
            : (IsActive ? "#F59E0B" : (IsLightMode ? "#CBD5E1" : "#252D3F"));

        public string BackgroundColorHex => IsCompleted
            ? (IsLightMode ? "#DCFCE7" : "#132E22")
            : (IsActive ? (IsLightMode ? "#FEF3C7" : "#332411") : (IsLightMode ? "#F1F5F9" : "#10141D"));

        public string TextColorHex => IsCompleted
            ? (IsLightMode ? "#15803D" : "#22C55E")
            : (IsActive ? (IsLightMode ? "#B45309" : "#F59E0B") : (IsLightMode ? "#475569" : "#8C9BAE"));

        public string BadgeBgHex => IsCompleted
            ? (IsLightMode ? "#DCFCE7" : "#1D4A32")
            : (IsActive ? (IsLightMode ? "#FEF3C7" : "#4A3419") : (IsLightMode ? "#E2E8F0" : "#1C2333"));
    }
}
