using System;
using System.Collections.Generic;
using SoundpadLightFINAL.Data;
using SoundpadLightFINAL.Models;

namespace SoundpadLightFINAL.Controllers;

public class PlaylistController
{
    private readonly Database _database;

    public PlaylistController(Database database)
    {
        _database = database;
    }

    public IList<Playlist> GetUserPlaylists(int userId)
    {
        var result = new List<Playlist>();

        using var connection = _database.CreateConnection();
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, UserId, Name FROM Playlists WHERE UserId = @userId ORDER BY Name";
        cmd.Parameters.AddWithValue("@userId", userId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Playlist
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                Name = reader.GetString(2)
            });
        }

        return result;
    }

    public int AddPlaylist(int userId, string name)
    {
        using var connection = _database.CreateConnection();
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO Playlists (UserId, Name) OUTPUT INSERTED.Id VALUES (@userId, @name)";
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@name", name);

        var idObj = cmd.ExecuteScalar();
        return (int)(idObj ?? 0);
    }

    public void RenamePlaylist(int playlistId, string newName)
    {
        using var connection = _database.CreateConnection();
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE Playlists SET Name = @name WHERE Id = @id";
        cmd.Parameters.AddWithValue("@name", newName);
        cmd.Parameters.AddWithValue("@id", playlistId);

        cmd.ExecuteNonQuery();
    }

    public void DeletePlaylist(int playlistId)
    {
        using var connection = _database.CreateConnection();
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Playlists WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", playlistId);

        cmd.ExecuteNonQuery();
    }
}


