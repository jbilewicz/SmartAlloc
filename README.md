# SmartAlloc — Personal Finance Manager

**SmartAlloc** is a desktop application for personal wealth management. Built with C# and WPF.

![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)
![Framework](https://img.shields.io/badge/.NET-9.0-purple.svg)
![Language](https://img.shields.io/badge/C%23-13-blue.svg)
[![Download](https://img.shields.io/github/v/release/jbilewicz/SmartAlloc?label=download&logo=github)](https://github.com/jbilewicz/SmartAlloc/releases/latest)

> **No installation required.** Download `SmartAlloc.exe` from [Releases](https://github.com/jbilewicz/SmartAlloc/releases/latest) and run it directly.

---

## Key Features

- **Zero-Knowledge Security:** Local SQLite database encrypted with **SQLCipher (AES-256)**. The database is stored in `%AppData%\SmartAlloc\` and never leaves your machine.
- **Interactive Dashboard:** Real-time KPI cards (balance, income, expenses), a pie chart of current-month spending by category, and a 6-month balance history line chart — all powered by **LiveCharts2**.
- **Transaction Management:** Add income and expense transactions with a category, date, and note. Filter the list by category or free-text search in real time.
- **Category Management:** Create custom categories with a name, icon, and color. Ten default categories are seeded on first launch (Food, Housing, Transport, Health, Entertainment, Clothing, Education, Travel, Bills, Other).
- **Budget Tracking:** Set monthly spending limits per category and track how close you are to each limit.
- **Smart Goal Tracker:** Create savings goals with a target amount and optional deadline. The app predicts your achievement date based on your average monthly savings velocity.
- **Live Currency Engine:** Real-time exchange rate integration (PLN / USD / EUR / CHF / GBP) via the **NBP API**, with a 15-minute in-memory cache and automatic fallback values when offline.
- **Monthly PDF Reports:** One-click generation of styled monthly financial summaries (transactions table, category breakdown, KPI summary) saved to your Documents folder via **QuestPDF**.
- **Dark / Light Theme:** Toggle between dark and light Material Design themes at runtime without restarting the app.

---

## Tech Stack

- **Language:** C# 13 / .NET 9
- **UI Framework:** WPF (Windows Presentation Foundation)
- **Architecture:** MVVM — `CommunityToolkit.Mvvm` (source-generated `ObservableProperty`, `RelayCommand`)
- **Dependency Injection:** `Microsoft.Extensions.DependencyInjection` + `Microsoft.Extensions.Hosting`
- **Design:** Material Design in XAML (`MaterialDesignThemes` 5.1)
- **Database:** SQLite + SQLCipher (AES-256 encryption) via `Microsoft.Data.Sqlite` + `SQLitePCLRaw.bundle_e_sqlcipher`
- **Libraries:**
  - `LiveChartsCore.SkiaSharpView.WPF` — pie and line charts
  - `QuestPDF` — PDF report generation
  - `System.Net.Http.Json` — NBP API consumption

---

## Project Structure

```
SmartAlloc/
  Data/
    DatabaseContext.cs       # SQLite connection, schema init, default category seed
  Models/
    Transaction.cs           # Transaction + TransactionType enum
    Category.cs              # Category (name, icon, color)
    Budget.cs                # Monthly budget limit per category
    Goal.cs                  # Savings goal with progress and remaining amount
    CurrencyRate.cs          # NBP API response models
  Services/
    TransactionService.cs    # CRUD + aggregations (balance, income, expenses, history)
    CategoryService.cs       # Category CRUD
    BudgetService.cs         # Budget CRUD
    GoalService.cs           # Goal CRUD + achievement date prediction
    CurrencyService.cs       # NBP API fetch, 15-min cache, PLN conversion
    ReportService.cs         # QuestPDF monthly report generation
    ThemeService.cs          # Dark/Light theme switching
  ViewModels/
    BaseViewModel.cs
    MainViewModel.cs         # Navigation, window chrome
    DashboardViewModel.cs    # KPI, charts, currency conversion
    TransactionsViewModel.cs # Transaction list, filters, add/delete
    BudgetsViewModel.cs      # Budget list, add/delete
    GoalsViewModel.cs        # Goal list, add funds, achievement prediction
  Views/
    MainWindow.xaml          # Shell with side navigation
    DashboardView.xaml
    TransactionsView.xaml
    BudgetsView.xaml
    GoalsView.xaml
  Themes/
    Colors.xaml              # Dark palette
    ColorsLight.xaml         # Light palette
    Styles.xaml              # Shared control styles
```

---

## Database Schema

| Table          | Key Columns                                                          |
|----------------|----------------------------------------------------------------------|
| `Transactions` | Id, Amount, Date, CategoryName, Note, Type (Income/Expense)          |
| `Categories`   | Id, Name, Icon, Color                                                |
| `Budgets`      | Id, CategoryName, MonthlyLimit, Month, Year                          |
| `Goals`        | Id, Name, Icon, TargetAmount, CurrentAmount, CreatedDate, TargetDate |

---

## Getting Started

**Prerequisites:** Windows 10/11, .NET 9 SDK

```bash
git clone https://github.com/your-username/SmartAlloc.git
cd SmartAlloc
dotnet run --project SmartAlloc/SmartAlloc.csproj
```

The database is created automatically on first launch at `%AppData%\SmartAlloc\SmartAlloc.db`.

---
**Author:**
jbilewicz
