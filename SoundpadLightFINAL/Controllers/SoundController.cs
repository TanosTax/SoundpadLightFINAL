using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using SoundpadLightFINAL.Data;
using SoundpadLightFINAL.Models;

namespace SoundpadLightFINAL.Controllers;

public class SoundController
{
    private readonly Database _database;

    public SoundController(Database database)
    {
        _database = database;
    }

    public IList<SoundItem> GetUserSounds(int userId, int? playlistId = null)
    {
        var result = new List<SoundItem>();

        using var connection = _database.CreateConnection();
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, UserId, Name, FilePath, PlaylistId FROM Sounds WHERE UserId = @userId AND (@playlistId IS NULL OR PlaylistId = @playlistId) ORDER BY Id";
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@playlistId", (object?)playlistId ?? DBNull.Value);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var item = new SoundItem
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                Name = reader.GetString(2),
                FilePath = reader.GetString(3)
            };

            if (!reader.IsDBNull(4))
            {
                item.PlaylistId = reader.GetInt32(4);
            }

            result.Add(item);
        }

        return result;
    }

    public void AddSound(int userId, string name, string filePath, int? playlistId = null)
    {
        using var connection = _database.CreateConnection();
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO Sounds (UserId, Name, FilePath, PlaylistId) VALUES (@userId, @name, @filePath, @playlistId)";
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@filePath", filePath);
        cmd.Parameters.AddWithValue("@playlistId", (object?)playlistId ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    public void DeleteSound(int soundId)
    {
        using var connection = _database.CreateConnection();
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Sounds WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", soundId);

        cmd.ExecuteNonQuery();
    }
}


