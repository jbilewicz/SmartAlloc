using Microsoft.Data.Sqlite;
using SmartAlloc.Data;
using SmartAlloc.Models;

namespace SmartAlloc.Services;

public class GoalService
{
    private readonly DatabaseContext _db;

    public GoalService(DatabaseContext db) => _db = db;

    public List<Goal> GetAll()
    {
        var list = new List<Goal>();
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Goals ORDER BY CreatedDate DESC";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(Map(reader));
        return list;
    }

    public void Add(Goal g)
    {
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Goals (Name, Icon, TargetAmount, CurrentAmount, CreatedDate, TargetDate)
            VALUES (@name, @icon, @target, @current, @created, @targetDate)";
        cmd.Parameters.AddWithValue("@name", g.Name);
        cmd.Parameters.AddWithValue("@icon", g.Icon);
        cmd.Parameters.AddWithValue("@target", g.TargetAmount);
        cmd.Parameters.AddWithValue("@current", g.CurrentAmount);
        cmd.Parameters.AddWithValue("@created", g.CreatedDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@targetDate", (object?)g.TargetDate?.ToString("yyyy-MM-dd") ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void AddFunds(int id, decimal amount)
    {
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Goals
            SET CurrentAmount = MIN(TargetAmount, CurrentAmount + @amount)
            WHERE Id = @id";
        cmd.Parameters.AddWithValue("@amount", amount);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Goals WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public DateTime? PredictAchievementDate(Goal goal, decimal avgMonthlySavings)
    {
        if (avgMonthlySavings <= 0) return null;
        var remaining = goal.RemainingAmount;
        if (remaining <= 0) return DateTime.Now;
        var monthsNeeded = (double)(remaining / avgMonthlySavings);
        return DateTime.Now.AddMonths((int)Math.Ceiling(monthsNeeded));
    }

    private static Goal Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        Name = r.GetString(1),
        Icon = r.IsDBNull(2) ? "🎯" : r.GetString(2),
        TargetAmount = r.GetDecimal(3),
        CurrentAmount = r.GetDecimal(4),
        CreatedDate = DateTime.Parse(r.GetString(5)),
        TargetDate = r.IsDBNull(6) ? null : DateTime.Parse(r.GetString(6))
    };
}
