using Microsoft.Data.Sqlite;
using SmartAlloc.Data;
using SmartAlloc.Models;

namespace SmartAlloc.Services;

public class TransactionService
{
    private readonly DatabaseContext _db;

    public TransactionService(DatabaseContext db) => _db = db;

    public List<Transaction> GetAll()
    {
        var list = new List<Transaction>();
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Transactions ORDER BY Date DESC";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(Map(reader));
        return list;
    }

    public List<Transaction> GetByMonth(int year, int month)
    {
        var list = new List<Transaction>();
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT * FROM Transactions
            WHERE strftime('%Y', Date) = @year
              AND strftime('%m', Date) = @month
            ORDER BY Date DESC";
        cmd.Parameters.AddWithValue("@year", year.ToString());
        cmd.Parameters.AddWithValue("@month", month.ToString("D2"));
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(Map(reader));
        return list;
    }

    public void Add(Transaction t)
    {
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Transactions (Amount, Date, CategoryName, Note, Type)
            VALUES (@amount, @date, @cat, @note, @type)";
        cmd.Parameters.AddWithValue("@amount", t.Amount);
        cmd.Parameters.AddWithValue("@date", t.Date.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@cat", t.CategoryName);
        cmd.Parameters.AddWithValue("@note", t.Note);
        cmd.Parameters.AddWithValue("@type", (int)t.Type);
        cmd.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Transactions WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public decimal GetTotalIncome() => GetSum(TransactionType.Income);
    public decimal GetTotalExpense() => GetSum(TransactionType.Expense);
    public decimal GetBalance() => GetTotalIncome() - GetTotalExpense();

    private decimal GetSum(TransactionType type)
    {
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(SUM(Amount),0) FROM Transactions WHERE Type = @type";
        cmd.Parameters.AddWithValue("@type", (int)type);
        return Convert.ToDecimal(cmd.ExecuteScalar());
    }

    public List<(DateTime Month, decimal Balance)> GetMonthlyBalanceHistory(int months = 6)
    {
        var result = new List<(DateTime, decimal)>();
        var conn = _db.GetConnection();
        for (int i = months - 1; i >= 0; i--)
        {
            var d = DateTime.Now.AddMonths(-i);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    COALESCE(SUM(CASE WHEN Type=0 THEN Amount ELSE 0 END), 0) -
                    COALESCE(SUM(CASE WHEN Type=1 THEN Amount ELSE 0 END), 0)
                FROM Transactions
                WHERE strftime('%Y', Date)=@y AND strftime('%m', Date)=@m";
            cmd.Parameters.AddWithValue("@y", d.Year.ToString());
            cmd.Parameters.AddWithValue("@m", d.Month.ToString("D2"));
            var val = Convert.ToDecimal(cmd.ExecuteScalar());
            result.Add((new DateTime(d.Year, d.Month, 1), val));
        }
        return result;
    }

    public Dictionary<string, decimal> GetExpensesByCategory(int year, int month)
    {
        var result = new Dictionary<string, decimal>();
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT CategoryName, SUM(Amount) as Total
            FROM Transactions
            WHERE Type=1
              AND strftime('%Y', Date)=@y
              AND strftime('%m', Date)=@m
            GROUP BY CategoryName";
        cmd.Parameters.AddWithValue("@y", year.ToString());
        cmd.Parameters.AddWithValue("@m", month.ToString("D2"));
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result[reader.GetString(0)] = reader.GetDecimal(1);
        return result;
    }

    public decimal GetAverageMonthlySavings(int months = 3)
    {
        decimal total = 0;
        var conn = _db.GetConnection();
        for (int i = 1; i <= months; i++)
        {
            var d = DateTime.Now.AddMonths(-i);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    COALESCE(SUM(CASE WHEN Type=0 THEN Amount ELSE 0 END), 0) -
                    COALESCE(SUM(CASE WHEN Type=1 THEN Amount ELSE 0 END), 0)
                FROM Transactions
                WHERE strftime('%Y', Date)=@y AND strftime('%m', Date)=@m";
            cmd.Parameters.AddWithValue("@y", d.Year.ToString());
            cmd.Parameters.AddWithValue("@m", d.Month.ToString("D2"));
            total += Convert.ToDecimal(cmd.ExecuteScalar());
        }
        return total / months;
    }

    private static Transaction Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        Amount = r.GetDecimal(1),
        Date = DateTime.Parse(r.GetString(2)),
        CategoryName = r.GetString(3),
        Note = r.IsDBNull(4) ? "" : r.GetString(4),
        Type = (TransactionType)r.GetInt32(5)
    };
}
