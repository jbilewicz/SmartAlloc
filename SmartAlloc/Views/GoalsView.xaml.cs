using SmartAlloc.ViewModels;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace SmartAlloc.Views;

internal sealed class GoalDragAdorner : Adorner
{
    private readonly VisualBrush _brush;
    private readonly Size _cardSize;
    private Point _position;

    public GoalDragAdorner(UIElement adornedRoot, UIElement draggedCard)
        : base(adornedRoot)
    {
        IsHitTestVisible = false;
        _cardSize = draggedCard.RenderSize;
        _brush = new VisualBrush(draggedCard)
        {
            Opacity = 0.82,
            Stretch = Stretch.None,
            AlignmentX = AlignmentX.Left,
            AlignmentY = AlignmentY.Top
        };

        Effect = new DropShadowEffect
        {
            BlurRadius = 32,
            ShadowDepth = 10,
            Direction = 270,
            Color = Colors.Black,
            Opacity = 0.55
        };
    }

    public void UpdatePosition(Point screenPos)
    {
        var root = AdornedElement as Visual;
        _position = root?.PointFromScreen(screenPos) ?? screenPos;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        const double scale = 1.05;
        double w = _cardSize.Width  > 0 ? _cardSize.Width  : 300;
        double h = _cardSize.Height > 0 ? _cardSize.Height : 220;

        var rect = new Rect(
            _position.X - w / 2,
            _position.Y - h / 2,
            w * scale,
            h * scale);

        dc.DrawRectangle(_brush, null, rect);
    }
}

public partial class GoalsView : UserControl
{
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT pt);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    private GoalDragAdorner? _adorner;
    private Border?          _highlightedCard;
    private bool             _isDragging;

    private static readonly SolidColorBrush HighlightBrush =
        new(Color.FromArgb(255, 99, 179, 237));
    private static readonly Thickness HighlightThickness = new(2);

    public GoalsView()
    {
        InitializeComponent();
    }

    private void MonthCell_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe
            && fe.DataContext is GoalItemVM == false
            && fe.DataContext is MonthCellVM cell
            && DataContext is GoalsViewModel vm)
        {
            vm.SelectMonthCommand.Execute(cell);
        }
    }

    private void GoalCard_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed
            || sender is not Border border
            || _isDragging)
            return;

        var pos = e.GetPosition(border);
        if (pos.X < 6 && pos.Y < 6) return;

        _isDragging = true;

        border.Opacity = 0.30;
        border.RenderTransform = new ScaleTransform(0.97, 0.97, border.Width / 2, 0);

        ShowAdorner(border);

        var data = new DataObject("GoalItem", border.DataContext);
        DragDrop.DoDragDrop(border, data, DragDropEffects.Move);

        border.Opacity         = 1.0;
        border.RenderTransform = Transform.Identity;
        RemoveHighlight();
        RemoveAdorner();
        _isDragging = false;
    }


    private void GoalCard_GiveFeedback(object sender, GiveFeedbackEventArgs e)
    {
        if (_adorner != null && GetCursorPos(out var pt))
            _adorner.UpdatePosition(new Point(pt.X, pt.Y));

        e.UseDefaultCursors = false;
        Mouse.SetCursor(Cursors.Hand);
        e.Handled = true;
    }


    private void GoalCard_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("GoalItem"))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.Move;

        if (sender is Border target && target != _highlightedCard)
        {
            RemoveHighlight();
            _highlightedCard = target;

            var colorAnim = new ColorAnimation(
                Color.FromArgb(255, 99, 179, 237),
                new Duration(TimeSpan.FromMilliseconds(120)));
            target.BorderBrush     = new SolidColorBrush();
            target.BorderThickness = HighlightThickness;
            ((SolidColorBrush)target.BorderBrush).BeginAnimation(
                SolidColorBrush.ColorProperty, colorAnim);

            var scaleAnim = new DoubleAnimation(1.03,
                new Duration(TimeSpan.FromMilliseconds(120)));
            var st = new ScaleTransform(1, 1, target.Width / 2, target.ActualHeight / 2);
            target.RenderTransform = st;
            st.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
        }

        e.Handled = true;
    }

    private void GoalCard_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border b && b == _highlightedCard)
            RemoveHighlight();
    }


    private void GoalCard_Drop(object sender, DragEventArgs e)
    {
        RemoveHighlight();

        if (e.Data.GetDataPresent("GoalItem")
            && sender is FrameworkElement target
            && target.DataContext is GoalItemVM targetItem
            && e.Data.GetData("GoalItem") is GoalItemVM sourceItem
            && sourceItem != targetItem
            && DataContext is GoalsViewModel vm)
        {
            var items    = vm.GoalItems;
            int oldIndex = items.IndexOf(sourceItem);
            int newIndex = items.IndexOf(targetItem);
            if (oldIndex >= 0 && newIndex >= 0)
                items.Move(oldIndex, newIndex);
        }

        e.Handled = true;
    }

    private void ShowAdorner(Border draggedCard)
    {
        var layer = AdornerLayer.GetAdornerLayer(this);
        if (layer == null) return;

        _adorner = new GoalDragAdorner(this, draggedCard);
        layer.Add(_adorner);

        if (GetCursorPos(out var pt))
            _adorner.UpdatePosition(new Point(pt.X, pt.Y));
    }

    private void RemoveAdorner()
    {
        if (_adorner == null) return;
        AdornerLayer.GetAdornerLayer(this)?.Remove(_adorner);
        _adorner = null;
    }


    private void RemoveHighlight()
    {
        if (_highlightedCard == null) return;

        var colorAnim = new ColorAnimation(
            Color.FromArgb(255, 50, 50, 70),
            new Duration(TimeSpan.FromMilliseconds(150)));

        if (_highlightedCard.BorderBrush is SolidColorBrush scb)
            scb.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
        else
            _highlightedCard.ClearValue(Border.BorderBrushProperty);

        if (_highlightedCard.RenderTransform is ScaleTransform st)
        {
            var back = new DoubleAnimation(1.0,
                new Duration(TimeSpan.FromMilliseconds(150)));
            st.BeginAnimation(ScaleTransform.ScaleXProperty, back);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, back);
        }

        _highlightedCard.BorderThickness = new Thickness(1);
        _highlightedCard = null;
    }
}
