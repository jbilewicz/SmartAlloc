namespace SmartAlloc.Services;

public class CurrentUserService
{
    public int CurrentUserId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string? AvatarPath { get; private set; }

    public void SetUser(int id, string displayName, string? avatarPath)
    {
        CurrentUserId = id;
        DisplayName = displayName;
        AvatarPath = avatarPath;
    }

    public void Clear()
    {
        CurrentUserId = 0;
        DisplayName = string.Empty;
        AvatarPath = null;
    }
}
