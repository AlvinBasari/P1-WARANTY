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

        public string BorderColorHex => IsCompleted ? "#22C55E" : (IsActive ? "#F59E0B" : "#252D3F");
        public string BackgroundColorHex => IsCompleted ? "#132E22" : (IsActive ? "#332411" : "#10141D");
        public string TextColorHex => IsCompleted ? "#22C55E" : (IsActive ? "#F59E0B" : "#8C9BAE");
        public string BadgeBgHex => IsCompleted ? "#1D4A32" : (IsActive ? "#4A3419" : "#1C2333");
    }
}
