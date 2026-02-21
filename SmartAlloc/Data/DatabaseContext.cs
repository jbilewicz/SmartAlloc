using Microsoft.Data.Sqlite;
using SmartAlloc.Models;
using System.IO;
using System.Security.Cryptography;

namespace SmartAlloc.Data;

public class DatabaseContext : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    private static readonly string _appDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SmartAlloc");

    public static readonly string DbPath = Path.Combine(_appDataDir, "SmartAlloc.db");

    private static string GetOrCreateDbPassword()
    {
        var keyPath = Path.Combine(_appDataDir, "db.key");
        Directory.CreateDirectory(_appDataDir);

        if (File.Exists(keyPath))
        {
            var encrypted = File.ReadAllBytes(keyPath);
            var raw = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(raw);
        }
        else
        {
            var raw = RandomNumberGenerator.GetBytes(32);
            var encrypted = ProtectedData.Protect(raw, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(keyPath, encrypted);
            return Convert.ToBase64String(raw);
        }
    }

    public DatabaseContext()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DbPath,
            Password = GetOrCreateDbPassword(),
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        InitializeDatabase();
    }

    public SqliteConnection GetConnection()
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
        {
            _connection?.Dispose();
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();
        }
        return _connection;
    }

    private void InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Transactions (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Amount      REAL    NOT NULL,
                Date        TEXT    NOT NULL,
                CategoryName TEXT   NOT NULL,
                Note        TEXT    DEFAULT '',
                Type        INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS Categories (
                Id   INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT    NOT NULL UNIQUE,
                Icon TEXT    DEFAULT '💰',
                Color TEXT   DEFAULT '#6C63FF'
            );

            CREATE TABLE IF NOT EXISTS Budgets (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                CategoryName TEXT    NOT NULL,
                MonthlyLimit REAL    NOT NULL,
                Month        INTEGER NOT NULL,
                Year         INTEGER NOT NULL,
                UNIQUE(CategoryName, Month, Year)
            );

            CREATE TABLE IF NOT EXISTS Goals (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                Name          TEXT    NOT NULL,
                Icon          TEXT    DEFAULT '🎯',
                TargetAmount  REAL    NOT NULL,
                CurrentAmount REAL    NOT NULL DEFAULT 0,
                CreatedDate   TEXT    NOT NULL,
                TargetDate    TEXT    NULL
            );

            CREATE TABLE IF NOT EXISTS RecurringTransactions (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                Amount          REAL    NOT NULL,
                CategoryName    TEXT    NOT NULL,
                Note            TEXT    DEFAULT '',
                Type            INTEGER NOT NULL DEFAULT 1,
                DayOfMonth      INTEGER NOT NULL DEFAULT 1,
                LastRunYearMonth TEXT   NULL
            );

            CREATE TABLE IF NOT EXISTS AppSettings (
                Key   TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Users (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                DisplayName TEXT    NOT NULL,
                PinHash     TEXT    NOT NULL,
                AvatarPath  TEXT    NULL
            );
        ";
        cmd.ExecuteNonQuery();

        MigrateAddUserIdColumns(conn);
        MigrateAddReminderColumn(conn);
        MigrateAddBudgetDescriptionAndRemoveUnique(conn);
        MigrateAddRecurringColor(conn);
        MigrateAddBudgetDeposited(conn);
        SeedDefaultCategories(conn);
    }

    private static void MigrateAddUserIdColumns(SqliteConnection conn)
    {
        var tables = new[] { "Transactions", "Budgets", "Goals", "RecurringTransactions" };
        foreach (var table in tables)
        {
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"ALTER TABLE {table} ADD COLUMN UserId INTEGER NOT NULL DEFAULT 0";
                cmd.ExecuteNonQuery();

                using var update = conn.CreateCommand();
                update.CommandText = $"UPDATE {table} SET UserId = (SELECT MIN(Id) FROM Users) WHERE UserId = 0";
                update.ExecuteNonQuery();
            }
            catch { }
        }
    }

    private static void MigrateAddReminderColumn(SqliteConnection conn)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "ALTER TABLE RecurringTransactions ADD COLUMN ReminderDaysBefore INTEGER NOT NULL DEFAULT 0";
            cmd.ExecuteNonQuery();
        }
        catch { }
    }

    private static void MigrateAddRecurringColor(SqliteConnection conn)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "ALTER TABLE RecurringTransactions ADD COLUMN Color TEXT NOT NULL DEFAULT '#6C63FF'";
            cmd.ExecuteNonQuery();
        }
        catch { }
    }

    private static void MigrateAddBudgetDeposited(SqliteConnection conn)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "ALTER TABLE Budgets ADD COLUMN Deposited REAL NOT NULL DEFAULT 0";
            cmd.ExecuteNonQuery();
        }
        catch { }
    }

    private static void MigrateAddBudgetDescriptionAndRemoveUnique(SqliteConnection conn)
    {
        try
        {
            using var addCol = conn.CreateCommand();
            addCol.CommandText = "ALTER TABLE Budgets ADD COLUMN Description TEXT NOT NULL DEFAULT ''";
            addCol.ExecuteNonQuery();
        }
        catch { }

        using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='Budgets'";
        var tableSql = checkCmd.ExecuteScalar() as string ?? "";

        if (tableSql.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
        {
            using var t1 = conn.CreateCommand();
            t1.CommandText = @"
                CREATE TABLE Budgets_new (
                    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                    CategoryName TEXT    NOT NULL,
                    MonthlyLimit REAL    NOT NULL,
                    Month        INTEGER NOT NULL,
                    Year         INTEGER NOT NULL,
                    UserId       INTEGER NOT NULL DEFAULT 0,
                    Description  TEXT    NOT NULL DEFAULT ''
                )";
            t1.ExecuteNonQuery();

            using var t2 = conn.CreateCommand();
            t2.CommandText = "INSERT INTO Budgets_new SELECT Id, CategoryName, MonthlyLimit, Month, Year, UserId, Description FROM Budgets";
            t2.ExecuteNonQuery();

            using var t3 = conn.CreateCommand();
            t3.CommandText = "DROP TABLE Budgets";
            t3.ExecuteNonQuery();

            using var t4 = conn.CreateCommand();
            t4.CommandText = "ALTER TABLE Budgets_new RENAME TO Budgets";
            t4.ExecuteNonQuery();
        }
    }

    private static void SeedDefaultCategories(SqliteConnection conn)
    {
        var defaultCats = new[]
        {
            ("🍔 Food",        "🍔", "#FF6B6B"),
            ("🏠 Housing",     "🏠", "#4ECDC4"),
            ("🚗 Transport",   "🚗", "#45B7D1"),
            ("💊 Health",      "💊", "#96CEB4"),
            ("🎮 Entertainment","🎮","#DDA0DD"),
            ("👕 Clothing",    "👕", "#F7DC6F"),
            ("📚 Education",   "📚", "#82E0AA"),
            ("✈️ Travel",     "✈️", "#F1948A"),
            ("💡 Bills",       "💡", "#85C1E9"),
            ("💰 Other",       "💰", "#D7BDE2"),
        };

        foreach (var (name, icon, color) in defaultCats)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT OR IGNORE INTO Categories (Name, Icon, Color)
                VALUES (@name, @icon, @color)";
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@icon", icon);
            cmd.Parameters.AddWithValue("@color", color);
            cmd.ExecuteNonQuery();
        }
    }

    public void CloseConnection()
    {
        _connection?.Close();
        _connection?.Dispose();
        _connection = null;
    }

    public string? GetSetting(string key)
    {
        var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Value FROM AppSettings WHERE Key=@k";
        cmd.Parameters.AddWithValue("@k", key);
        return cmd.ExecuteScalar() as string;
    }

    public void SetSetting(string key, string value)
    {
        var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO AppSettings (Key,Value) VALUES (@k,@v) ON CONFLICT(Key) DO UPDATE SET Value=@v";
        cmd.Parameters.AddWithValue("@k", key);
        cmd.Parameters.AddWithValue("@v", value);
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
