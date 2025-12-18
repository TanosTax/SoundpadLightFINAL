using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace SoundpadLightFINAL.Data;

public class Database
{
    // ЗАМЕНИ эту строку на свою реальную строку подключения к MSSQL
    // пример: Server=localhost;Database=SoundpadLight;Trusted_Connection=True;TrustServerCertificate=True;
    private const string ConnectionString =
        "Server=DESKTOP-LSOM170\\SQLEXPRESS;Database=SoundpadLight;Trusted_Connection=True;TrustServerCertificate=True;";

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(ConnectionString);
    }

    public void EnsureCreated()
    {
        using var connection = CreateConnection();
        connection.Open();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText =
                """
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                BEGIN
                    CREATE TABLE Users
                    (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Username NVARCHAR(64) NOT NULL UNIQUE,
                        PasswordHash NVARCHAR(256) NOT NULL
                    );
                END;
                """;
            cmd.ExecuteNonQuery();
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText =
                """
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sounds')
                BEGIN
                    CREATE TABLE Sounds
                    (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        UserId INT NOT NULL,
                        Name NVARCHAR(128) NOT NULL,
                        FilePath NVARCHAR(512) NOT NULL,
                        CONSTRAINT FK_Sounds_Users FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
                    );
                END;
                """;
            cmd.ExecuteNonQuery();
        }

        // Playlists table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText =
                """
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Playlists')
                BEGIN
                    CREATE TABLE Playlists
                    (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        UserId INT NOT NULL,
                        Name NVARCHAR(128) NOT NULL,
                        CONSTRAINT FK_Playlists_Users FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
                    );
                END;
                """;
            cmd.ExecuteNonQuery();
        }

        // PlaylistId column on Sounds
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText =
                """
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE Name = 'PlaylistId' AND Object_ID = Object_ID('Sounds')
                )
                BEGIN
                    ALTER TABLE Sounds ADD PlaylistId INT NULL;
                END;
                """;
            cmd.ExecuteNonQuery();
        }

        // Foreign key Sounds.PlaylistId -> Playlists.Id
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText =
                """
                IF NOT EXISTS (
                    SELECT * FROM sys.foreign_keys 
                    WHERE name = 'FK_Sounds_Playlists'
                )
                BEGIN
                    ALTER TABLE Sounds
                    ADD CONSTRAINT FK_Sounds_Playlists 
                    FOREIGN KEY (PlaylistId) REFERENCES Playlists(Id);
                END;
                """;
            cmd.ExecuteNonQuery();
        }
    }
}


