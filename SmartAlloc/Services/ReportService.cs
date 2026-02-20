using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartAlloc.Models;
using System.IO;

namespace SmartAlloc.Services;

public class ReportService
{
    static ReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public string GenerateMonthlyReport(
        int year, int month,
        List<Transaction> transactions,
        Dictionary<string, decimal> expensesByCategory,
        decimal totalIncome, decimal totalExpense, decimal balance)
    {
        var fileName = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            $"SmartAlloc_Report_{year}_{month:D2}.pdf");

        var monthName = new DateTime(year, month, 1).ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("SmartAlloc")
                                .FontSize(26).Bold().FontColor("#6C63FF");
                            c.Item().Text("Monthly Report – " + monthName)
                                .FontSize(14).FontColor("#888888");
                        });
                        row.ConstantItem(100).Height(60).Background("#6C63FF")
                            .AlignCenter().AlignMiddle()
                            .Text("💳").FontSize(36);
                    });
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#6C63FF");
                });

                page.Content().PaddingTop(20).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        KpiCard(row.RelativeItem(), "Income", $"{totalIncome:N2} PLN", "#27AE60");
                        row.ConstantItem(12);
                        KpiCard(row.RelativeItem(), "Expenses", $"{totalExpense:N2} PLN", "#E74C3C");
                        row.ConstantItem(12);
                        string balColor = balance >= 0 ? "#27AE60" : "#E74C3C";
                        KpiCard(row.RelativeItem(), "Balance", $"{balance:N2} PLN", balColor);
                    });

                    col.Item().PaddingTop(20).Text("Transactions").FontSize(14).Bold();
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(90);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(3);
                            cols.ConstantColumn(90);
                        });

                        table.Header(header =>
                        {
                            foreach (var h in new[] { "Date", "Category", "Note", "Amount" })
                                header.Cell().Background("#6C63FF").Padding(6)
                                    .Text(t => { t.DefaultTextStyle(s => s.Bold().FontColor("#FFFFFF")); t.Span(h); });
                        });

                        bool alt = false;
                        foreach (var t in transactions)
                        {
                            string rowBg = alt ? "#F8F9FA" : "#FFFFFF";
                            alt = !alt;
                            string amtColor = t.Type == TransactionType.Income ? "#27AE60" : "#E74C3C";
                            string sign = t.Type == TransactionType.Income ? "+" : "-";
                            table.Cell().Background(rowBg).Padding(5).Text(t.Date.ToString("dd.MM.yyyy"));
                            table.Cell().Background(rowBg).Padding(5).Text(t.CategoryName);
                            table.Cell().Background(rowBg).Padding(5).Text(t.Note);
                            table.Cell().Background(rowBg).Padding(5)
                                .Text(tx => { tx.DefaultTextStyle(s => s.FontColor(amtColor)); tx.Span($"{sign}{t.Amount:N2} PLN"); });
                        }
                    });

                    if (expensesByCategory.Count > 0)
                    {
                        col.Item().PaddingTop(20).Text("Expenses by category").FontSize(14).Bold();
                        col.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                foreach (var h in new[] { "Category", "Amount", "Share" })
                                    header.Cell().Background("#333366").Padding(6)
                                        .Text(t => { t.DefaultTextStyle(s => s.Bold().FontColor("#FFFFFF")); t.Span(h); });
                            });

                            bool a = false;
                            foreach (var kvp in expensesByCategory.OrderByDescending(x => x.Value))
                            {
                                string rowBg2 = a ? "#F8F9FA" : "#FFFFFF";
                                a = !a;
                                string pct = totalExpense > 0
                                    ? $"{kvp.Value / totalExpense * 100:N1}%" : "0%";
                                table.Cell().Background(rowBg2).Padding(5).Text(kvp.Key);
                                table.Cell().Background(rowBg2).Padding(5).Text($"{kvp.Value:N2} PLN");
                                table.Cell().Background(rowBg2).Padding(5).Text(pct);
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.DefaultTextStyle(s => s.FontColor("#888888"));
                    x.Span("SmartAlloc \u00a9 2026  |  Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf(fileName);

        return fileName;
    }

    private static void KpiCard(IContainer c, string label, string value, string color)
    {
        c.Border(1).BorderColor(color).Padding(12).Column(col =>
        {
            col.Item().Text(label).FontSize(10).FontColor("#888888");
            col.Item().PaddingTop(4).Text(t => { t.DefaultTextStyle(s => s.FontSize(18).Bold().FontColor(color)); t.Span(value); });
        });
    }
}
