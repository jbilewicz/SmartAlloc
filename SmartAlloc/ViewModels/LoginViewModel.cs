using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SmartAlloc.Models;
using SmartAlloc.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SmartAlloc.ViewModels;

public enum LoginScreen { Welcome, CreateAccount, SelectUser, EnterPin }

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;
    private readonly SnackbarService _snackbar;
    private readonly CurrentUserService _currentUser;

    [ObservableProperty] private LoginScreen _currentScreen = LoginScreen.Welcome;

    [ObservableProperty] private bool _isUnlocked;
    [ObservableProperty] private string _errorMessage = string.Empty;

    [ObservableProperty] private string _newDisplayName = string.Empty;
    [ObservableProperty] private string _newPin = string.Empty;
    [ObservableProperty] private string _newConfirmPin = string.Empty;
    [ObservableProperty] private string? _newAvatarPath;
    [ObservableProperty] private ImageSource? _newAvatarPreview;

    [ObservableProperty] private ObservableCollection<UserAccount> _users = [];
    [ObservableProperty] private UserAccount? _selectedUser;
    [ObservableProperty] private string _loginPin = string.Empty;
    [ObservableProperty] private ImageSource? _selectedUserAvatar;

    public bool IsWelcome => CurrentScreen == LoginScreen.Welcome;
    public bool IsCreateAccount => CurrentScreen == LoginScreen.CreateAccount;
    public bool IsSelectUser => CurrentScreen == LoginScreen.SelectUser;
    public bool IsEnterPin => CurrentScreen == LoginScreen.EnterPin;

    private static readonly string AvatarDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SmartAlloc", "avatars");

    public LoginViewModel(AuthService authService, SnackbarService snackbar, CurrentUserService currentUser)
    {
        _authService = authService;
        _snackbar = snackbar;
        _currentUser = currentUser;
    }

    partial void OnCurrentScreenChanged(LoginScreen value)
    {
        OnPropertyChanged(nameof(IsWelcome));
        OnPropertyChanged(nameof(IsCreateAccount));
        OnPropertyChanged(nameof(IsSelectUser));
        OnPropertyChanged(nameof(IsEnterPin));
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void GoToCreateAccount()
    {
        NewDisplayName = string.Empty;
        NewPin = string.Empty;
        NewConfirmPin = string.Empty;
        NewAvatarPath = null;
        NewAvatarPreview = null;
        CurrentScreen = LoginScreen.CreateAccount;
    }

    [RelayCommand]
    private void GoToLogin()
    {
        var userList = _authService.GetAllUsers();
        Users = new ObservableCollection<UserAccount>(userList);
        SelectedUser = null;
        LoginPin = string.Empty;

        if (userList.Count == 0)
        {
            _snackbar.ShowWarning("No accounts found. Please create one first.");
            CurrentScreen = LoginScreen.Welcome;
            return;
        }

        CurrentScreen = LoginScreen.SelectUser;
    }

    [RelayCommand]
    private void GoBack()
    {
        CurrentScreen = CurrentScreen switch
        {
            LoginScreen.CreateAccount => LoginScreen.Welcome,
            LoginScreen.SelectUser => LoginScreen.Welcome,
            LoginScreen.EnterPin => LoginScreen.SelectUser,
            _ => LoginScreen.Welcome
        };
    }

    [RelayCommand]
    private void PickAvatar()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose profile picture",
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            NewAvatarPath = dialog.FileName;
            NewAvatarPreview = LoadImage(dialog.FileName);
        }
    }

    [RelayCommand]
    private void CreateAccount()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(NewDisplayName))
        {
            ErrorMessage = "Please enter a display name.";
            return;
        }
        if (NewPin.Length < 1)
        {
            ErrorMessage = "Please enter a PIN.";
            return;
        }
        if (NewPin != NewConfirmPin)
        {
            ErrorMessage = "PINs do not match.";
            return;
        }

        string? savedAvatar = null;
        if (!string.IsNullOrEmpty(NewAvatarPath) && File.Exists(NewAvatarPath))
        {
            Directory.CreateDirectory(AvatarDir);
            var ext = Path.GetExtension(NewAvatarPath);
            var fileName = $"avatar_{Guid.NewGuid():N}{ext}";
            var destPath = Path.Combine(AvatarDir, fileName);
            File.Copy(NewAvatarPath, destPath, true);
            savedAvatar = destPath;
        }

        var user = _authService.CreateUser(NewDisplayName, NewPin, savedAvatar);
        _currentUser.SetUser(user.Id, user.DisplayName, user.AvatarPath);
        _snackbar.ShowSuccess($"Account '{NewDisplayName}' created!");
        IsUnlocked = true;
    }

    [RelayCommand]
    private void SelectUserAccount(UserAccount user)
    {
        SelectedUser = user;
        LoginPin = string.Empty;
        SelectedUserAvatar = !string.IsNullOrEmpty(user.AvatarPath) && File.Exists(user.AvatarPath)
            ? LoadImage(user.AvatarPath)
            : null;
        CurrentScreen = LoginScreen.EnterPin;
    }

    [RelayCommand]
    private void PinDigit(string digit)
    {
        LoginPin += digit;
    }

    [RelayCommand]
    private void PinBackspace()
    {
        if (LoginPin.Length > 0)
            LoginPin = LoginPin[..^1];
    }

    [RelayCommand]
    private void SubmitPin()
    {
        ErrorMessage = string.Empty;

        if (SelectedUser == null) return;

        if (_authService.VerifyPin(SelectedUser.Id, LoginPin))
        {
            _currentUser.SetUser(SelectedUser.Id, SelectedUser.DisplayName, SelectedUser.AvatarPath);
            IsUnlocked = true;
        }
        else
        {
            ErrorMessage = "Incorrect PIN.";
            LoginPin = string.Empty;
        }
    }

    private static BitmapImage? LoadImage(string path)
    {
        try
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(path, UriKind.Absolute);
            bi.DecodePixelWidth = 120;
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.EndInit();
            bi.Freeze();
            return bi;
        }
        catch { return null; }
    }

    public void LockToUser(UserAccount user)
    {
        IsUnlocked = false;
        ErrorMessage = string.Empty;
        SelectedUser = user;
        LoginPin = string.Empty;
        SelectedUserAvatar = !string.IsNullOrEmpty(user.AvatarPath) && File.Exists(user.AvatarPath)
            ? LoadImage(user.AvatarPath)
            : null;
        CurrentScreen = LoginScreen.EnterPin;
    }
}
