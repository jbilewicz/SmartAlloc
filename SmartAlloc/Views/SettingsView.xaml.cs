using SmartAlloc.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartAlloc.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();

        foreach (var box in new[] { PinCurrentBox, PinNewBox, PinConfirmBox })
        {
            box.PreviewTextInput += OnPinPreviewTextInput;
            DataObject.AddPastingHandler(box, OnPinPaste);
        }

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

    private void ChangePinButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.CurrentPin = PinCurrentBox.Password;
            vm.NewPin = PinNewBox.Password;
            vm.ConfirmNewPin = PinConfirmBox.Password;
        }
    }

    private static void OnPinPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsDigit);
    }

    private static void OnPinPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetData(DataFormats.Text) is string text && text.All(char.IsDigit))
            return;
        e.CancelCommand();
    }
}
