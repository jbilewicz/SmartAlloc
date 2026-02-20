using Microsoft.Data.Sqlite;
using SmartAlloc.Models;
using System.IO;

namespace SmartAlloc.Data;

public class DatabaseContext : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    private const string DbPassword = "SmartAlloc@AES256#SecureKey!2026";
    private static readonly string DbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SmartAlloc", "SmartAlloc.db");

    public DatabaseContext()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DbPath,
            Password = DbPassword,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        InitializeDatabase();
    }

    public SqliteConnection GetConnection()
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
        {
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
        ";
        cmd.ExecuteNonQuery();

        SeedDefaultCategories(conn);
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

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
