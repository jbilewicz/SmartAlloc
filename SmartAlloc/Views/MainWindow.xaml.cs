using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using SmartAlloc.Data;
using SmartAlloc.Services;
using SmartAlloc.ViewModels;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace SmartAlloc.Views;

public partial class MainWindow : Window
{
    private DispatcherTimer? _autoLockTimer;
    private DispatcherTimer? _reminderTimer;
    private int _autoLockMinutes;
    private bool _isAutoLock;
    private NotifyIcon? _trayIcon;
    private bool _isTrayClose;

    public MainWindow()
    {
        InitializeComponent();

        PreviewMouseMove += (_, _) => ResetAutoLockTimer();
        PreviewMouseDown += (_, _) => ResetAutoLockTimer();

        InitTrayIcon();

        _reminderTimer = new DispatcherTimer { Interval = TimeSpan.FromHours(1) };
        _reminderTimer.Tick += (_, _) =>
        {
            try
            {
                var rs = App.Services.GetRequiredService<ReminderService>();
                rs.CheckReminders();
            }
            catch { }
        };
        _reminderTimer.Start();
    }

    private void InitTrayIcon()
    {
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("💰 SmartAlloc – Open", null, (_, _) => RestoreFromTray());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("❌ Exit", null, (_, _) => ExitApp());

        var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "SmartAlloc.ico");
        var appIcon = System.IO.File.Exists(iconPath)
            ? new Icon(iconPath)
            : SystemIcons.Application;

        _trayIcon = new NotifyIcon
        {
            Text        = "SmartAlloc – Personal Finance",
            Visible     = true,
            Icon        = appIcon,
            ContextMenuStrip = contextMenu
        };
        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();

        try
        {
            var rs = App.Services.GetRequiredService<ReminderService>();
            rs.SetTrayIcon(_trayIcon);
        }
        catch { }
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        _trayIcon!.ShowBalloonTip(1500, "SmartAlloc",
            LocalizationService.Current.Get("Tray.WelcomeBack"), ToolTipIcon.None);
    }

    private void ExitApp()
    {
        _isTrayClose = true;
        _trayIcon?.Dispose();
        _trayIcon = null;
        System.Windows.Application.Current.Shutdown();
    }

    public static void SetBlur(bool enabled)
    {
        if (System.Windows.Application.Current.MainWindow is MainWindow mw)
        {
            mw.MainContent.Effect = enabled
                ? new BlurEffect { Radius = 8, KernelType = KernelType.Gaussian }
                : null;
        }
    }

    public void Initialize(MainViewModel mainVM)
    {
        mainVM.LogoutRequested += () =>
            Dispatcher.Invoke(() =>
            {
                _isAutoLock = false;
                var loginVM = App.Services.GetRequiredService<LoginViewModel>();
                ShowLoginOverlay(loginVM);
            });

        mainVM.SettingsVM.AutoLockMinutesChanged += ApplyAutoLock;
    }

    public void ShowLoginOverlay(LoginViewModel loginVM)
    {
        LoginViewControl.DataContext = loginVM;
        LoginOverlay.Visibility = Visibility.Visible;

        loginVM.PropertyChanged += OnLoginPropertyChanged;
    }

    private async void OnLoginPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LoginViewModel.IsUnlocked) && sender is LoginViewModel vm && vm.IsUnlocked)
        {
            vm.PropertyChanged -= OnLoginPropertyChanged;
            LoginOverlay.Visibility = Visibility.Collapsed;

            if (!_isAutoLock)
            {
                if (DataContext is MainViewModel mainVM)
                {
                    await mainVM.InitializeAsync();

                    var db = App.Services.GetRequiredService<DatabaseContext>();
                    if (int.TryParse(db.GetSetting("AutoLockMinutes"), out int stored))
                        ApplyAutoLock(stored);

                    var recurringService = App.Services.GetRequiredService<RecurringTransactionService>();
                    int processed = recurringService.ProcessDue();
                    if (processed > 0)
                    {
                        var snackSvc = App.Services.GetRequiredService<SnackbarService>();
                        snackSvc.ShowSuccess($"{processed} recurring transaction{(processed > 1 ? "s" : "")} added.");
                    }

                    try
                    {
                        var rs = App.Services.GetRequiredService<ReminderService>();
                        rs.CheckReminders();
                    }
                    catch { }
                }
            }
            else
            {
                _isAutoLock = false;
                ResetAutoLockTimer();
            }
        }
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        var currentUser = App.Services.GetRequiredService<CurrentUserService>();
        currentUser.Clear();
        StopAutoLockTimer();

        _isAutoLock = false;
        var loginVM = App.Services.GetRequiredService<LoginViewModel>();
        ShowLoginOverlay(loginVM);
    }


    private void ApplyAutoLock(int minutes)
    {
        _autoLockMinutes = minutes;
        StopAutoLockTimer();

        if (minutes <= 0) return;

        _autoLockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(minutes)
        };
        _autoLockTimer.Tick += AutoLock_Tick;
        _autoLockTimer.Start();
    }

    private void ResetAutoLockTimer()
    {
        if (_autoLockTimer == null || !_autoLockTimer.IsEnabled) return;
        _autoLockTimer.Stop();
        _autoLockTimer.Start();
    }

    private void StopAutoLockTimer()
    {
        if (_autoLockTimer == null) return;
        _autoLockTimer.Stop();
        _autoLockTimer.Tick -= AutoLock_Tick;
        _autoLockTimer = null;
    }

    private void AutoLock_Tick(object? sender, EventArgs e)
    {
        StopAutoLockTimer();

        if (LoginOverlay.Visibility == Visibility.Visible) return;

        var currentUser = App.Services.GetRequiredService<CurrentUserService>();
        var authService = App.Services.GetRequiredService<AuthService>();

        var users = authService.GetAllUsers();
        var user = users.FirstOrDefault(u => u.Id == currentUser.CurrentUserId);
        if (user == null) return;

        _isAutoLock = true;

        var loginVM = App.Services.GetRequiredService<LoginViewModel>();
        loginVM.LockToUser(user);
        ShowLoginOverlay(loginVM);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        ResetAutoLockTimer();

        if (LoginOverlay.Visibility == Visibility.Visible) return;

        switch (e.Key)
        {
            case Key.F1:
                ToggleShortcutsOverlay();
                e.Handled = true;
                return;

            case Key.Escape:
                if (ShortcutsOverlay.Visibility == Visibility.Visible)
                {
                    ShortcutsOverlay.Visibility = Visibility.Collapsed;
                    e.Handled = true;
                }
                else if (DataContext is MainViewModel mvm)
                {
                    mvm.NavigateToDashboardCommand.Execute(null);
                    e.Handled = true;
                }
                return;
        }

        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.L:
                    AutoLock_Tick(null, EventArgs.Empty);
                    e.Handled = true;
                    break;

                case Key.W:
                    WindowState = WindowState.Minimized;
                    e.Handled = true;
                    break;

                case Key.I:
                    if (DataContext is MainViewModel mvm2)
                        mvm2.TogglePrivacyCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
    }

    private void ToggleShortcutsOverlay()
        => ShortcutsOverlay.Visibility = ShortcutsOverlay.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;

    private void HelpButton_Click(object sender, RoutedEventArgs e)
        => ToggleShortcutsOverlay();

    private void CloseShortcuts_Click(object sender, RoutedEventArgs e)
        => ShortcutsOverlay.Visibility = Visibility.Collapsed;

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            MaximizeButton_Click(sender, e);
        else
            DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        var loc = SmartAlloc.Services.LocalizationService.Current;
        var result = System.Windows.MessageBox.Show(
            loc.Get("Tray.CloseMessage"),
            loc.Get("Tray.CloseTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            Hide();
            _trayIcon?.ShowBalloonTip(3000, "SmartAlloc",
                $"{loc.Get("Tray.Running")} – {loc.Get("Tray.RunningHint")}", ToolTipIcon.Info);
        }
        else
        {
            ExitApp();
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isTrayClose)
        {
            // Window X button or Alt+F4 – treat same as CloseButton
            e.Cancel = true;
            CloseButton_Click(this, new RoutedEventArgs());
            return;
        }
        base.OnClosing(e);
    }
}
