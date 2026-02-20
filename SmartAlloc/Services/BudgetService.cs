using Microsoft.Data.Sqlite;
using SmartAlloc.Data;
using SmartAlloc.Models;

namespace SmartAlloc.Services;

public class BudgetService
{
    private readonly DatabaseContext _db;

    public BudgetService(DatabaseContext db) => _db = db;

    public List<Budget> GetByMonth(int year, int month)
    {
        var list = new List<Budget>();
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT * FROM Budgets WHERE Year=@y AND Month=@m";
        cmd.Parameters.AddWithValue("@y", year);
        cmd.Parameters.AddWithValue("@m", month);
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

    public void Upsert(Budget b)
    {
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Budgets (CategoryName, MonthlyLimit, Month, Year)
            VALUES (@cat, @limit, @m, @y)
            ON CONFLICT(CategoryName, Month, Year)
            DO UPDATE SET MonthlyLimit=excluded.MonthlyLimit";
        cmd.Parameters.AddWithValue("@cat", b.CategoryName);
        cmd.Parameters.AddWithValue("@limit", b.MonthlyLimit);
        cmd.Parameters.AddWithValue("@m", b.Month);
        cmd.Parameters.AddWithValue("@y", b.Year);
        cmd.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Budgets WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }
}
