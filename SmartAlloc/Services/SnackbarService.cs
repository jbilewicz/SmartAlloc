using MaterialDesignThemes.Wpf;

namespace SmartAlloc.Services;

public class SnackbarService
{
    public SnackbarMessageQueue MessageQueue { get; } = new(TimeSpan.FromSeconds(3));

    public void Show(string message)
        => MessageQueue.Enqueue(message);

    public void ShowSuccess(string message)
        => MessageQueue.Enqueue($"✅ {message}");

    public void ShowWarning(string message)
        => MessageQueue.Enqueue($"⚠️ {message}");

    public void ShowError(string message)
        => MessageQueue.Enqueue($"❌ {message}");
}
