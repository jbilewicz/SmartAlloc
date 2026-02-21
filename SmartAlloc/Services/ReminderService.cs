using System.Windows;
using System.Windows.Forms;
using SmartAlloc.Models;

namespace SmartAlloc.Services;

public class ReminderService : IDisposable
{
    private readonly RecurringTransactionService _recurringService;
    private NotifyIcon? _notifyIcon;
    private DateTime _lastChecked = DateTime.MinValue;

    public ReminderService(RecurringTransactionService recurringService)
    {
        _recurringService = recurringService;
    }

    public void SetTrayIcon(NotifyIcon icon)
    {
        _notifyIcon = icon;
    }

    public void CheckReminders()
    {
        if (DateTime.Today == _lastChecked.Date) return;
        _lastChecked = DateTime.Now;

        try
        {
            var items   = _recurringService.GetAll();
            var today   = DateTime.Today;
            var alerts  = new List<string>();

            foreach (var r in items)
            {
                if (r.ReminderDaysBefore <= 0) continue;

                var dueDay  = Math.Min(r.DayOfMonth, DateTime.DaysInMonth(today.Year, today.Month));
                var dueDate = new DateTime(today.Year, today.Month, dueDay);
                if (dueDate < today)
                {
                    var nm      = today.AddMonths(1);
                    dueDay      = Math.Min(r.DayOfMonth, DateTime.DaysInMonth(nm.Year, nm.Month));
                    dueDate     = new DateTime(nm.Year, nm.Month, dueDay);
                }

                int daysUntil = (dueDate - today).Days;
                if (daysUntil <= r.ReminderDaysBefore && daysUntil >= 0)
                {
                    string when = daysUntil == 0 ? "TODAY"
                                : daysUntil == 1 ? "tomorrow"
                                : $"in {daysUntil} days";
                    alerts.Add($"{r.CategoryName} ({r.Amount:N2} PLN) – due {when}");
                }
            }

            if (alerts.Count > 0)
            {
                string body = string.Join("\n", alerts.Take(5));
                _notifyIcon?.ShowBalloonTip(
                    timeout : 8000,
                    tipTitle: "💸 SmartAlloc – Upcoming payments",
                    tipText : body,
                    tipIcon : ToolTipIcon.Info);
            }
        }
        catch { }
    }

    public void Dispose()
    {
        // NotifyIcon is owned by MainWindow; nothing to dispose here.
    }
}
