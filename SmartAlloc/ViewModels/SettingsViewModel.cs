using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartAlloc.Data;
using SmartAlloc.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SmartAlloc.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly AuthService _authService;
    private readonly BackupService _backupService;
    private readonly SnackbarService _snackbar;
    private readonly CurrentUserService _currentUser;
    private readonly DatabaseContext _db;

    [ObservableProperty] private string _currentPin = string.Empty;
    [ObservableProperty] private string _newPin = string.Empty;
    [ObservableProperty] private string _confirmNewPin = string.Empty;

    public ObservableCollection<string> AutoLockOptions { get; } =
        ["Disabled", "5 minutes", "10 minutes", "15 minutes", "30 minutes"];

    private static readonly int[] AutoLockMinutes = [0, 5, 10, 15, 30];

    [ObservableProperty] private int _selectedAutoLockIndex;

    public event Action? LogoutRequested;

    public event Action<int>? AutoLockMinutesChanged;

    public SettingsViewModel(
        AuthService authService,
        BackupService backupService,
        SnackbarService snackbar,
        CurrentUserService currentUser,
        DatabaseContext db)
    {
        _authService = authService;
        _backupService = backupService;
        _snackbar = snackbar;
        _currentUser = currentUser;
        _db = db;
    }

    public void Load()
    {
        CurrentPin = string.Empty;
        NewPin = string.Empty;
        ConfirmNewPin = string.Empty;

        var stored = _db.GetSetting("AutoLockMinutes");
        if (int.TryParse(stored, out int minutes))
        {
            var idx = Array.IndexOf(AutoLockMinutes, minutes);
            SelectedAutoLockIndex = idx >= 0 ? idx : 0;
        }
        else
        {
            SelectedAutoLockIndex = 0;
        }
    }

    [RelayCommand]
    private void ChangePin()
    {
        if (string.IsNullOrEmpty(CurrentPin))
        {
            _snackbar.ShowWarning("Enter your current PIN.");
            return;
        }
        if (string.IsNullOrEmpty(NewPin))
        {
            _snackbar.ShowWarning("Enter a new PIN.");
            return;
        }
        if (NewPin != ConfirmNewPin)
        {
            _snackbar.ShowWarning("New PINs do not match.");
            return;
        }

        bool ok = _authService.ChangePin(_currentUser.CurrentUserId, CurrentPin, NewPin);
        if (ok)
        {
            _snackbar.ShowSuccess("PIN changed successfully.");
            CurrentPin = string.Empty;
            NewPin = string.Empty;
            ConfirmNewPin = string.Empty;
        }
        else
        {
            _snackbar.ShowError("Current PIN is incorrect.");
        }
    }

    [RelayCommand]
    private void SaveAutoLock()
    {
        int minutes = AutoLockMinutes[SelectedAutoLockIndex];
        _db.SetSetting("AutoLockMinutes", minutes.ToString());
        AutoLockMinutesChanged?.Invoke(minutes);
        _snackbar.ShowSuccess(minutes == 0
            ? "Auto-lock disabled."
            : $"Auto-lock set to {minutes} minutes.");
    }

    [RelayCommand]
    private void Backup()
    {
        bool ok = _backupService.Backup();
        if (ok) _snackbar.ShowSuccess("Backup exported successfully.");
    }

    [RelayCommand]
    private void Restore()
    {
        _backupService.Restore();
    }

    [RelayCommand]
    private void DeleteAccount()
    {
        var name = _currentUser.DisplayName;
        var result = MessageBox.Show(
            $"Delete account \"{name}\"?\n\nThis will permanently delete all transactions, budgets, goals and recurring transactions for this account.\n\nThis action cannot be undone.",
            "Delete Account",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        _authService.DeleteUser(_currentUser.CurrentUserId);
        _currentUser.Clear();
        _snackbar.ShowSuccess($"Account \"{name}\" deleted.");
        LogoutRequested?.Invoke();
    }

    public int GetAutoLockMinutes() => AutoLockMinutes[SelectedAutoLockIndex];
}
