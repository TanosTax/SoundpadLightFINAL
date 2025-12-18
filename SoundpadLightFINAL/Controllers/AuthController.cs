using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using SoundpadLightFINAL.Data;
using SoundpadLightFINAL.Models;

namespace SoundpadLightFINAL.Controllers;

public class AuthController
{
    private readonly Database _database;

    public AuthController(Database database)
    {
        _database = database;
    }

    public User? Login(string username, string password)
    {
        using var connection = _database.CreateConnection();
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Username, PasswordHash FROM Users WHERE Username = @username";
        cmd.Parameters.AddWithValue("@username", username);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var storedHash = reader.GetString(2);
        var passwordHash = HashPassword(password);

        if (!string.Equals(storedHash, passwordHash, StringComparison.Ordinal))
        {
            return null;
        }

        return new User
        {
            Id = reader.GetInt32(0),
            Username = reader.GetString(1),
            PasswordHash = storedHash
        };
    }

    public bool Register(string username, string password, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            error = "Username and password are required.";
            return false;
        }

        using var connection = _database.CreateConnection();
        connection.Open();

        using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(1) FROM Users WHERE Username = @username";
            checkCmd.Parameters.AddWithValue("@username", username);

            var exists = (int)checkCmd.ExecuteScalar()!;
            if (exists > 0)
            {
                error = "User with this username already exists.";
                return false;
            }
        }

        using (var insertCmd = connection.CreateCommand())
        {
            insertCmd.CommandText = "INSERT INTO Users (Username, PasswordHash) VALUES (@username, @passwordHash)";
            insertCmd.Parameters.AddWithValue("@username", username);
            insertCmd.Parameters.AddWithValue("@passwordHash", HashPassword(password));

            var rows = insertCmd.ExecuteNonQuery();
            return rows == 1;
        }
    }

    public bool ChangeUsername(int userId, string newUsername, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(newUsername))
        {
            error = "Username is required.";
            return false;
        }

        using var connection = _database.CreateConnection();
        connection.Open();

        // Проверяем, что такого имени ещё нет
        using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(1) FROM Users WHERE Username = @username AND Id <> @id";
            checkCmd.Parameters.AddWithValue("@username", newUsername);
            checkCmd.Parameters.AddWithValue("@id", userId);

            var exists = (int)checkCmd.ExecuteScalar()!;
            if (exists > 0)
            {
                error = "User with this username already exists.";
                return false;
            }
        }

        using (var updateCmd = connection.CreateCommand())
        {
            updateCmd.CommandText = "UPDATE Users SET Username = @username WHERE Id = @id";
            updateCmd.Parameters.AddWithValue("@username", newUsername);
            updateCmd.Parameters.AddWithValue("@id", userId);

            var rows = updateCmd.ExecuteNonQuery();
            return rows == 1;
        }
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}


