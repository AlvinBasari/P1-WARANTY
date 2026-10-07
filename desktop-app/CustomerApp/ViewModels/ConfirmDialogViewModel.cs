using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CustomerApp.ViewModels
{
    public partial class ConfirmDialogViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _title = "Konfirmasi Tindakan";

        [ObservableProperty]
        private string _message = string.Empty;

        [ObservableProperty]
        private string? _detailNote;

        [ObservableProperty]
        private string _confirmText = "Ya, Lanjutkan";

        [ObservableProperty]
        private string _cancelText = "Batal";

        [ObservableProperty]
        private string _dialogType = "info"; // "info", "warning", "danger", "success"

        [ObservableProperty]
        private string? _badgeText;

        private Action _onConfirm;
        private Action _onCancel;

        public Action ConfirmAction => _onConfirm;
        public Action CancelAction => _onCancel;

        public void SetCallbacks(Action onConfirm, Action onCancel)
        {
            _onConfirm = onConfirm;
            _onCancel = onCancel;
        }

        public bool IsDanger => DialogType.Equals("danger", StringComparison.OrdinalIgnoreCase);
        public bool IsWarning => DialogType.Equals("warning", StringComparison.OrdinalIgnoreCase);
        public bool IsSuccess => DialogType.Equals("success", StringComparison.OrdinalIgnoreCase);
        public bool IsInfo => !IsDanger && !IsWarning && !IsSuccess;

        public bool HasDetailNote => !string.IsNullOrWhiteSpace(DetailNote);

        public ConfirmDialogViewModel(
            string title,
            string message,
            Action onConfirm,
            Action onCancel,
            string? detailNote = null,
            string confirmText = "Ya, Lanjutkan",
            string cancelText = "Batal",
            string dialogType = "info",
            string? badgeText = null)
        {
            _title = title;
            _message = message;
            _detailNote = detailNote;
            _confirmText = confirmText;
            _cancelText = cancelText;
            _dialogType = dialogType;
            _badgeText = badgeText;
            _onConfirm = onConfirm;
            _onCancel = onCancel;
        }

        [RelayCommand]
        public void Confirm()
        {
            _onConfirm?.Invoke();
        }

        [RelayCommand]
        public void Cancel()
        {
            _onCancel?.Invoke();
        }
    }
}
