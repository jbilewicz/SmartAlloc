using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartAlloc.Services;

public partial class PrivacyService : ObservableObject
{
    public static PrivacyService Current { get; private set; } = null!;

    [ObservableProperty] private bool _isPrivate;

    public event Action? PrivacyChanged;

    public PrivacyService()
    {
        Current = this;
    }

    public void Toggle()
    {
        IsPrivate = !IsPrivate;
        PrivacyChanged?.Invoke();
    }
}
