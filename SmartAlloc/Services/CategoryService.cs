using Microsoft.Data.Sqlite;
using SmartAlloc.Data;
using SmartAlloc.Models;

namespace SmartAlloc.Services;

public class CategoryService
{
    private readonly DatabaseContext _db;

    public CategoryService(DatabaseContext db) => _db = db;

    public List<Category> GetAll()
    {
        var list = new List<Category>();
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Categories ORDER BY Name";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(new Category
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Icon = reader.IsDBNull(2) ? "💰" : reader.GetString(2),
                Color = reader.IsDBNull(3) ? "#6C63FF" : reader.GetString(3)
            });
        return list;
    }

    public void Add(Category c)
    {
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR IGNORE INTO Categories (Name, Icon, Color)
            VALUES (@name, @icon, @color)";
        cmd.Parameters.AddWithValue("@name", c.Name);
        cmd.Parameters.AddWithValue("@icon", c.Icon);
        cmd.Parameters.AddWithValue("@color", c.Color);
        cmd.ExecuteNonQuery();
    }
}
