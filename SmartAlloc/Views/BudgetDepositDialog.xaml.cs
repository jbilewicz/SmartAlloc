using System.Windows;
using System.Windows.Controls;

namespace SmartAlloc.Views;

public partial class BudgetDepositDialog : Window
{
    public decimal DepositAmount { get; private set; }
    private readonly decimal _maxAmount;

    public BudgetDepositDialog(string budgetName, decimal maxAmount, string symbol)
    {
        InitializeComponent();
        _maxAmount = maxAmount;
        TitleText.Text = $"Deposit to {budgetName}";
        SubtitleText.Text = $"Maximum: {maxAmount:N2} {symbol}";
        AmountBox.Text = "0";
        AmountBox.Focus();
        AmountBox.SelectAll();
    }

    private void AmountBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!decimal.TryParse(AmountBox.Text, out var val)) return;
        if (val > _maxAmount)
        {
            AmountBox.Text = _maxAmount.ToString("N2");
            AmountBox.CaretIndex = AmountBox.Text.Length;
        }
    }

    private void Deposit_Click(object sender, RoutedEventArgs e)
    {
        if (decimal.TryParse(AmountBox.Text, out var val) && val > 0)
        {
            DepositAmount = Math.Min(val, _maxAmount);
            DialogResult = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
