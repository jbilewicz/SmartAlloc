using System.Security.Cryptography;
using System.Text;
using SmartAlloc.Data;
using SmartAlloc.Models;

namespace SmartAlloc.Services;

public class AuthService
{
    private readonly DatabaseContext _db;

    public AuthService(DatabaseContext db) => _db = db;

    public List<UserAccount> GetAllUsers()
    {
        var list = new List<UserAccount>();
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, DisplayName, PinHash, AvatarPath FROM Users ORDER BY Id";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new UserAccount
            {
                Id = reader.GetInt32(0),
                DisplayName = reader.GetString(1),
                PinHash = reader.GetString(2),
                AvatarPath = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }
        return list;
    }

    public bool HasUsers() => GetAllUsers().Count > 0;

    public bool VerifyPin(int userId, string pin)
    {
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT PinHash FROM Users WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", userId);
        var stored = cmd.ExecuteScalar() as string;
        return stored != null && stored == HashPin(pin);
    }

    public UserAccount CreateUser(string displayName, string pin, string? avatarPath)
    {
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Users (DisplayName, PinHash, AvatarPath)
            VALUES (@name, @hash, @avatar);
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@name", displayName);
        cmd.Parameters.AddWithValue("@hash", HashPin(pin));
        cmd.Parameters.AddWithValue("@avatar", (object?)avatarPath ?? DBNull.Value);
        var id = Convert.ToInt32(cmd.ExecuteScalar());
        return new UserAccount
        {
            Id = id,
            DisplayName = displayName,
            PinHash = HashPin(pin),
            AvatarPath = avatarPath
        };
    }

    public bool ChangePin(int userId, string currentPin, string newPin)
    {
        if (!VerifyPin(userId, currentPin)) return false;
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Users SET PinHash=@hash WHERE Id=@id";
        cmd.Parameters.AddWithValue("@hash", HashPin(newPin));
        cmd.Parameters.AddWithValue("@id", userId);
        cmd.ExecuteNonQuery();
        return true;
    }

    public void DeleteUser(int userId)
    {
        var conn = _db.GetConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var table in new[] { "Transactions", "Budgets", "Goals", "RecurringTransactions" })
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = $"DELETE FROM {table} WHERE UserId=@uid";
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.ExecuteNonQuery();
            }
            using var delUser = conn.CreateCommand();
            delUser.Transaction = tx;
            delUser.CommandText = "DELETE FROM Users WHERE Id=@id";
            delUser.Parameters.AddWithValue("@id", userId);
            delUser.ExecuteNonQuery();
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static string HashPin(string pin)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"SmartAlloc_PIN_{pin}"));
        return Convert.ToBase64String(bytes);
    }
}
