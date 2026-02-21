using SmartAlloc.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SmartAlloc.Views;

public partial class CalendarView : UserControl
{
    public CalendarView()
    {
        InitializeComponent();
    }

    private void DayCell_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe
            && fe.DataContext is CalendarDayVM day
            && !day.IsPadding
            && DataContext is CalendarViewModel vm)
        {
            vm.SelectedDay = day;
        }
    }

    private void ColorSwatch_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is string color
            && DataContext is CalendarViewModel vm)
        {
            vm.QuickColor = color;
            HighlightSwatch(border);
        }
    }

    private void RecurringColorSwatch_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is string color
            && DataContext is CalendarViewModel vm)
        {
            vm.NewRecurringColor = color;
            HighlightSwatch(border);
        }
    }

    private static void HighlightSwatch(Border selected)
    {
        if (selected.Parent is WrapPanel panel)
        {
            foreach (var child in panel.Children.OfType<Border>())
            {
                bool isSel = child == selected;
                child.BorderThickness = new Thickness(isSel ? 2 : 0);
                child.BorderBrush = isSel ? System.Windows.Media.Brushes.White : null;

                var check = child.Child as System.Windows.Controls.TextBlock;
                if (check != null)
                    check.Visibility = isSel ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
