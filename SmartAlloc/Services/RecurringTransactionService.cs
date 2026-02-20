using Microsoft.Data.Sqlite;
using SmartAlloc.Data;
using SmartAlloc.Models;

namespace SmartAlloc.Services;

public class RecurringTransactionService
{
    private readonly DatabaseContext _db;
    private readonly TransactionService _txService;
    private readonly CurrentUserService _currentUser;

    public RecurringTransactionService(DatabaseContext db, TransactionService txService, CurrentUserService currentUser)
    {
        _db = db;
        _txService = txService;
        _currentUser = currentUser;
    }

    private int Uid => _currentUser.CurrentUserId;

    public List<RecurringTransaction> GetAll()
    {
        var list = new List<RecurringTransaction>();
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM RecurringTransactions WHERE UserId=@uid ORDER BY CategoryName";
        cmd.Parameters.AddWithValue("@uid", Uid);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(Map(reader));
        return list;
    }

    public void Add(RecurringTransaction r)
    {
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO RecurringTransactions (Amount, CategoryName, Note, Type, DayOfMonth, UserId)
            VALUES (@amount, @cat, @note, @type, @day, @uid)";
        cmd.Parameters.AddWithValue("@amount", r.Amount);
        cmd.Parameters.AddWithValue("@cat", r.CategoryName);
        cmd.Parameters.AddWithValue("@note", r.Note);
        cmd.Parameters.AddWithValue("@type", (int)r.Type);
        cmd.Parameters.AddWithValue("@day", r.DayOfMonth);
        cmd.Parameters.AddWithValue("@uid", Uid);
        cmd.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM RecurringTransactions WHERE Id=@id AND UserId=@uid";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@uid", Uid);
        cmd.ExecuteNonQuery();
    }

    public int ProcessDue()
    {
        var today = DateTime.Today;
        var currentYearMonth = today.ToString("yyyy-MM");
        int count = 0;

        foreach (var r in GetAll())
        {
            if (today.Day < r.DayOfMonth) continue;
            if (r.LastRunYearMonth == currentYearMonth) continue;

            _txService.Add(new Transaction
            {
                Amount = r.Amount,
                Date = new DateTime(today.Year, today.Month,
                    Math.Min(r.DayOfMonth, DateTime.DaysInMonth(today.Year, today.Month))),
                CategoryName = r.CategoryName,
                Note = string.IsNullOrWhiteSpace(r.Note)
                    ? $"Recurring – {r.CategoryName}"
                    : r.Note,
                Type = r.Type
            });

            var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE RecurringTransactions SET LastRunYearMonth=@ym WHERE Id=@id";
            cmd.Parameters.AddWithValue("@ym", currentYearMonth);
            cmd.Parameters.AddWithValue("@id", r.Id);
            cmd.ExecuteNonQuery();

            count++;
        }

        return count;
    }

    private static RecurringTransaction Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        Amount = r.GetDecimal(1),
        CategoryName = r.GetString(2),
        Note = r.IsDBNull(3) ? "" : r.GetString(3),
        Type = (TransactionType)r.GetInt32(4),
        DayOfMonth = r.GetInt32(5),
        LastRunYearMonth = r.IsDBNull(6) ? null : r.GetString(6)
    };
}
