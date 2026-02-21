using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SmartAlloc.ViewModels;

namespace SmartAlloc.Views;

public partial class LoginView : UserControl
{
    private LoginViewModel? _vm;

    public LoginView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        foreach (var box in new[] { PinCreateBox, PinConfirmCreateBox })
        {
            box.PreviewTextInput += OnPinPreviewTextInput;
            DataObject.AddPastingHandler(box, OnPinPaste);
        }

        PinCreateBox.PasswordChanged += (_, _) =>
        {
            if (_vm != null) _vm.NewPin = PinCreateBox.Password;
        };
        PinConfirmCreateBox.PasswordChanged += (_, _) =>
        {
            if (_vm != null) _vm.NewConfirmPin = PinConfirmCreateBox.Password;
        };

        LoginPinBox.PreviewTextInput += OnPinPreviewTextInput;
        DataObject.AddPastingHandler(LoginPinBox, OnPinPaste);
        LoginPinBox.PasswordChanged += (_, _) =>
        {
            if (_vm != null) _vm.LoginPin = LoginPinBox.Password;
        };
        LoginPinBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter && _vm != null)
                _vm.SubmitPinCommand.Execute(null);
        };
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (_vm != null)
            _vm.PropertyChanged -= OnVmPropertyChanged;

        _vm = e.NewValue as LoginViewModel;

        if (_vm != null)
            _vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LoginViewModel.NewPin) && _vm!.NewPin == string.Empty)
            PinCreateBox.Password = string.Empty;
        if (e.PropertyName == nameof(LoginViewModel.NewConfirmPin) && _vm!.NewConfirmPin == string.Empty)
            PinConfirmCreateBox.Password = string.Empty;
        if (e.PropertyName == nameof(LoginViewModel.LoginPin) && _vm!.LoginPin == string.Empty)
            LoginPinBox.Password = string.Empty;
        if (e.PropertyName == nameof(LoginViewModel.IsEnterPin) && _vm!.IsEnterPin)
            Dispatcher.BeginInvoke(() => LoginPinBox.Focus(), System.Windows.Threading.DispatcherPriority.Input);
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
