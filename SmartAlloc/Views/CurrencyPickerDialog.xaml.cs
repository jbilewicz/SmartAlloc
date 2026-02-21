using System.Windows;
using System.Windows.Controls;

namespace SmartAlloc.Views;

public partial class CurrencyPickerDialog : Window
{
    public string SelectedCurrency { get; private set; } = "PLN";

    public CurrencyPickerDialog(string currentCurrency)
    {
        InitializeComponent();

        foreach (ListBoxItem item in CurrencyList.Items)
        {
            if (item.Tag?.ToString() == currentCurrency)
            {
                CurrencyList.SelectedItem = item;
                break;
            }
        }

        if (CurrencyList.SelectedItem == null)
            CurrencyList.SelectedIndex = 0;
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        if (CurrencyList.SelectedItem is ListBoxItem item)
            SelectedCurrency = item.Tag?.ToString() ?? "PLN";
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
