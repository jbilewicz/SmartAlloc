using SmartAlloc.ViewModels;
using System.Windows.Controls;

namespace SmartAlloc.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();

        PinCurrentBox.PasswordChanged += (_, _) =>
        {
            if (DataContext is SettingsViewModel vm)
                vm.CurrentPin = PinCurrentBox.Password;
        };
        PinNewBox.PasswordChanged += (_, _) =>
        {
            if (DataContext is SettingsViewModel vm)
                vm.NewPin = PinNewBox.Password;
        };
        PinConfirmBox.PasswordChanged += (_, _) =>
        {
            if (DataContext is SettingsViewModel vm)
                vm.ConfirmNewPin = PinConfirmBox.Password;
        };

        DataContextChanged += (_, _) =>
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(SettingsViewModel.CurrentPin) && vm.CurrentPin == string.Empty)
                        PinCurrentBox.Password = string.Empty;
                    if (e.PropertyName == nameof(SettingsViewModel.NewPin) && vm.NewPin == string.Empty)
                        PinNewBox.Password = string.Empty;
                    if (e.PropertyName == nameof(SettingsViewModel.ConfirmNewPin) && vm.ConfirmNewPin == string.Empty)
                        PinConfirmBox.Password = string.Empty;
                };
            }
        };
    }

    private void ChangePinButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.CurrentPin = PinCurrentBox.Password;
            vm.NewPin = PinNewBox.Password;
            vm.ConfirmNewPin = PinConfirmBox.Password;
        }
    }
}
