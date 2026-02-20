using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartAlloc.Models;
using SmartAlloc.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SmartAlloc.ViewModels;

public partial class GoalItemVM : ObservableObject
{
    public Goal Goal { get; set; } = null!;
    public string PredictedDate { get; set; } = "";
    public string ProgressText => $"{Goal.CurrentAmount:N0} / {Goal.TargetAmount:N0} PLN";
    public double Progress => Goal.ProgressPercent;
    public string StatusColor => Progress >= 100 ? "#27AE60"
                                : Progress >= 50 ? "#F39C12"
                                : "#6C63FF";

    [ObservableProperty] private decimal _depositAmount;
}

public partial class GoalsViewModel : BaseViewModel
{
    private readonly GoalService _goalService;
    private readonly TransactionService _txService;

    [ObservableProperty] private ObservableCollection<GoalItemVM> _goalItems = [];
    [ObservableProperty] private Goal? _selectedGoal;

    [ObservableProperty] private string _newName = string.Empty;
    [ObservableProperty] private string _newIcon = "🎯";
    [ObservableProperty] private decimal _newTargetAmount;
    [ObservableProperty] private decimal _newCurrentAmount;
    [ObservableProperty] private DateTime? _newTargetDate;

    public List<string> AvailableIcons { get; } =
        ["🎯", "🏠", "🚗", "✈️", "💍", "🎓", "🏖️", "💻", "📱", "🏋️", "🌍", "💎"];

    public GoalsViewModel(GoalService goalService, TransactionService txService)
    {
        _goalService = goalService;
        _txService = txService;
    }

    [RelayCommand]
    public void Load()
    {
        var avgSavings = _txService.GetAverageMonthlySavings(3);
        GoalItems.Clear();
        foreach (var g in _goalService.GetAll())
        {
            var predicted = _goalService.PredictAchievementDate(g, avgSavings);
            GoalItems.Add(new GoalItemVM
            {
                Goal = g,
                PredictedDate = predicted.HasValue
                    ? $"Goal achievable: {predicted.Value:MMMM yyyy}"
                    : "Insufficient data for prediction"
            });
        }
    }

    [RelayCommand]
    private void AddGoal()
    {
        if (string.IsNullOrWhiteSpace(NewName) || NewTargetAmount <= 0)
        {
            MessageBox.Show("Please enter a name and target amount.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _goalService.Add(new Goal
        {
            Name = NewName,
            Icon = NewIcon,
            TargetAmount = NewTargetAmount,
            CurrentAmount = NewCurrentAmount,
            CreatedDate = DateTime.Today,
            TargetDate = NewTargetDate
        });
        NewName = string.Empty;
        NewTargetAmount = 0;
        NewCurrentAmount = 0;
        NewTargetDate = null;
        NewIcon = "🎯";
        Load();
    }

    [RelayCommand]
    private void Deposit(GoalItemVM item)
    {
        if (item == null || item.DepositAmount <= 0) return;
        _goalService.AddFunds(item.Goal.Id, item.DepositAmount);
        item.DepositAmount = 0;
        Load();
    }

    [RelayCommand]
    private void DeleteGoal(GoalItemVM item)
    {
        if (item == null) return;
        var res = MessageBox.Show($"Delete goal '{item.Goal.Name}'?",
            "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res == MessageBoxResult.Yes)
        {
            _goalService.Delete(item.Goal.Id);
            Load();
        }
    }
}
