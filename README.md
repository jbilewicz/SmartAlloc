# SmartAlloc

A personal finance management desktop application built with WPF and .NET 9.

![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)
![Framework](https://img.shields.io/badge/.NET-9.0-purple.svg)
![Language](https://img.shields.io/badge/C%23-13-blue.svg)
[![Download](https://img.shields.io/github/v/release/jbilewicz/SmartAlloc?label=download&logo=github)](https://github.com/jbilewicz/SmartAlloc/releases/latest)

---

## Key Features

- **Dashboard** — spending overview with interactive pie chart (localized category names), recent transactions, and quick stats
- **Transactions** — add, edit, delete income/expense entries with category tagging and notes
- **Budgets** — monthly budget limits per category with progress tracking
- **Goals** — savings goal tracking with month-by-month view mode; year navigation hidden in month view
- **Statistics** — visual spending breakdowns by category and time period
- **Recurring Transactions** — automatic entries on a defined schedule
- **Currency Converter** — live exchange rates via API
- **Reports** — PDF export via QuestPDF
- **Multi-language** — 11 languages (EN, PL, DE, FR, ES, IT, PT, NL, RU, UK, JA); all UI text translated including category names
- **Themes** — light and dark mode
- **Tray mode** — close button shows a dialog: minimize to system tray (app runs in background) or exit completely; double-click tray icon to restore; all tray text is localized
- **Reminders** — scheduled balloon notifications via system tray
- **Secure local DB** — SQLite encrypted with SQLCipher; database stored in `%AppData%\SmartAlloc\`

---

## Tech Stack

| Layer | Technology |
|---|---|
| UI Framework | WPF (.NET 9) |
| MVVM | CommunityToolkit.Mvvm 8.4.0 |
| Database | SQLite + SQLCipher (Microsoft.Data.Sqlite) |
| Charts | LiveChartsCore.SkiaSharpView.WPF |
| PDF | QuestPDF |
| UI Components | MaterialDesignThemes |
| Exchange Rates | Open exchange rates API |

---

## Project Structure

```
SmartAlloc/
├── Models/             # Entity classes (Transaction, Budget, Goal, …)
├── ViewModels/         # MVVM view models
├── Views/              # XAML views and code-behind
├── Services/           # Business logic (Auth, Budget, Currency, Report, …)
├── Converters/         # WPF value converters
├── Data/               # DatabaseContext (EF Core + SQLite)
├── Themes/             # XAML resource dictionaries (Colors, Styles)
└── Resources/          # Icons and other static assets
```

---

## Database Schema

| Table | Key Columns |
|---|---|
| `UserAccounts` | Id, Username, PinHash, Currency, Language, Theme |
| `Transactions` | Id, UserId, Amount, Category, Date, Note, IsRecurring |
| `Budgets` | Id, UserId, Category, MonthlyLimit, Month |
| `Goals` | Id, UserId, Name, TargetAmount, CurrentAmount, Deadline |
| `RecurringTransactions` | Id, UserId, Amount, Category, IntervalDays, NextDate |
| `CurrencyRates` | BaseCurrency, TargetCurrency, Rate, LastUpdated |

---

## Getting Started

### Prerequisites

- Windows 10/11
- [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)

### Build from Source

```bash
git clone https://github.com/jbilewicz/SmartAlloc.git
cd SmartAlloc
dotnet build SmartAlloc.sln --configuration Release
dotnet run --project SmartAlloc/SmartAlloc.csproj
```

### Publish a Self-Contained Executable

```bash
dotnet publish SmartAlloc/SmartAlloc.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/
```

The resulting `SmartAlloc.exe` in `publish/` can be distributed without requiring .NET installed.

---

## Privacy & Security

- All data is stored **locally** in `%AppData%\SmartAlloc\`
- The SQLite database is encrypted with SQLCipher
- The encryption key is stored in `%AppData%\SmartAlloc\db.key` — never in the repository
- No data is sent to any server except optional live currency rate lookups


---

**Author:** [jbilewicz](https://github.com/jbilewicz)
