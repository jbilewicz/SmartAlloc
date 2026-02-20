namespace SmartAlloc.Models;

public class Goal
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "🎯";
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? TargetDate { get; set; }
    public int UserId { get; set; }

    public double ProgressPercent =>
        TargetAmount > 0 ? Math.Min(100, (double)(CurrentAmount / TargetAmount * 100)) : 0;

    public decimal RemainingAmount => Math.Max(0, TargetAmount - CurrentAmount);
}
