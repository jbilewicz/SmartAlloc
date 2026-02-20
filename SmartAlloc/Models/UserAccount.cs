namespace SmartAlloc.Models;

public class UserAccount
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string PinHash { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
}
