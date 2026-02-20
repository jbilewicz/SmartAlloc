using Microsoft.Data.Sqlite;
using SmartAlloc.Data;
using SmartAlloc.Models;

namespace SmartAlloc.Services;

public class BudgetService
{
    private readonly DatabaseContext _db;
    private readonly CurrentUserService _currentUser;

    public BudgetService(DatabaseContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private int Uid => _currentUser.CurrentUserId;

    public List<Budget> GetByMonth(int year, int month)
    {
        var list = new List<Budget>();
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT * FROM Budgets WHERE Year=@y AND Month=@m AND UserId=@uid";
        cmd.Parameters.AddWithValue("@y", year);
        cmd.Parameters.AddWithValue("@m", month);
        cmd.Parameters.AddWithValue("@uid", Uid);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(new Budget
            {
                Id = reader.GetInt32(0),
                CategoryName = reader.GetString(1),
                MonthlyLimit = reader.GetDecimal(2),
                Month = reader.GetInt32(3),
                Year = reader.GetInt32(4)
            });
        return list;
    }

    public Budget? GetForCategory(int year, int month, string categoryName)
    {
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Budgets WHERE Year=@y AND Month=@m AND CategoryName=@cat AND UserId=@uid LIMIT 1";
        cmd.Parameters.AddWithValue("@y", year);
        cmd.Parameters.AddWithValue("@m", month);
        cmd.Parameters.AddWithValue("@cat", categoryName);
        cmd.Parameters.AddWithValue("@uid", Uid);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new Budget
        {
            Id = reader.GetInt32(0),
            CategoryName = reader.GetString(1),
            MonthlyLimit = reader.GetDecimal(2),
            Month = reader.GetInt32(3),
            Year = reader.GetInt32(4)
        };
    }

    public void Upsert(Budget b)
    {
        var conn = _db.GetConnection();

        using var check = conn.CreateCommand();
        check.CommandText = "SELECT Id FROM Budgets WHERE CategoryName=@cat AND Month=@m AND Year=@y AND UserId=@uid LIMIT 1";
        check.Parameters.AddWithValue("@cat", b.CategoryName);
        check.Parameters.AddWithValue("@m", b.Month);
        check.Parameters.AddWithValue("@y", b.Year);
        check.Parameters.AddWithValue("@uid", Uid);
        var existingId = check.ExecuteScalar();

        if (existingId != null)
        {
            using var update = conn.CreateCommand();
            update.CommandText = "UPDATE Budgets SET MonthlyLimit=@limit WHERE Id=@id";
            update.Parameters.AddWithValue("@limit", b.MonthlyLimit);
            update.Parameters.AddWithValue("@id", existingId);
            update.ExecuteNonQuery();
        }
        else
        {
            using var insert = conn.CreateCommand();
            insert.CommandText = @"
                INSERT INTO Budgets (CategoryName, MonthlyLimit, Month, Year, UserId)
                VALUES (@cat, @limit, @m, @y, @uid)";
            insert.Parameters.AddWithValue("@cat", b.CategoryName);
            insert.Parameters.AddWithValue("@limit", b.MonthlyLimit);
            insert.Parameters.AddWithValue("@m", b.Month);
            insert.Parameters.AddWithValue("@y", b.Year);
            insert.Parameters.AddWithValue("@uid", Uid);
            insert.ExecuteNonQuery();
        }
    }

    public void Delete(int id)
    {
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Budgets WHERE Id=@id AND UserId=@uid";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@uid", Uid);
        cmd.ExecuteNonQuery();
    }
}
