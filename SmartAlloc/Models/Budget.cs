namespace SmartAlloc.Models;

public class Budget
{
    public int Id { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyLimit { get; set; }
    public decimal Deposited { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int UserId { get; set; }
}
