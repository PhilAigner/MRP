using System;
using System.Runtime;
using Npgsql;

namespace MRP;

public class RatingRepository :  IRatingsRepository {

    private readonly DatabaseConnection _dbConnection;

    public RatingRepository(DatabaseConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }


    public List<Rating> GetAll() {
        var ratings = new List<Rating>();
        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "SELECT uuid, media_entry_uuid, user_uuid, stars, comment, created_at, public_visible FROM ratings",
            connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var rating = new Rating(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetInt32(3),
                reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                reader.GetDateTime(5),
                reader.GetBoolean(6)
            );
            ratings.Add(rating);
        }

        connection.Close();

        foreach (var rating in ratings)
        {
            LoadLikedBy(rating);
        }

        return ratings;
    }

    public Rating? GetById(Guid id) {
        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "SELECT uuid, media_entry_uuid, user_uuid, stars, comment, created_at, public_visible FROM ratings WHERE uuid = @uuid",
            connection);
        cmd.Parameters.AddWithValue("uuid", id);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var rating = new Rating(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetInt32(3),
                reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                reader.GetDateTime(5),
                reader.GetBoolean(6)
            );
            reader.Close();
            LoadLikedBy(rating);
            return rating;
        }

        return null;
    }

    public List<Rating>? GetByCreator(Guid userid) {
        var ratings = new List<Rating>();
        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "SELECT uuid, media_entry_uuid, user_uuid, stars, comment, created_at, public_visible FROM ratings WHERE user_uuid = @userid",
            connection);
        cmd.Parameters.AddWithValue("userid", userid);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var rating = new Rating(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetInt32(3),
                reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                reader.GetDateTime(5),
                reader.GetBoolean(6)
            );
            ratings.Add(rating);
        }

        connection.Close();

        foreach (var rating in ratings)
        {
            LoadLikedBy(rating);
        }

        return ratings;
    }

    public Guid AddRating(Rating rating) {
        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "INSERT INTO ratings (uuid, media_entry_uuid, user_uuid, stars, comment, created_at, public_visible) " +
            "VALUES (@uuid, @mediaEntry, @user, @stars, @comment, @createdAt, @publicVisible) " +
            "ON CONFLICT (uuid) DO NOTHING RETURNING uuid",
            connection);

        cmd.Parameters.AddWithValue("uuid", rating.uuid);
        cmd.Parameters.AddWithValue("mediaEntry", rating.mediaEntry);
        cmd.Parameters.AddWithValue("user", rating.user);
        cmd.Parameters.AddWithValue("stars", rating.stars);
        cmd.Parameters.AddWithValue("comment", rating.comment ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("createdAt", rating.createdAt);
        cmd.Parameters.AddWithValue("publicVisible", rating.publicVisible);

        var result = cmd.ExecuteScalar();
        var insertedUuid = result != null ? (Guid)result : Guid.Empty;

        if (insertedUuid != Guid.Empty && rating.likedBy != null && rating.likedBy.Count > 0)
        {
            SaveLikedBy(rating);
        }

        return insertedUuid;
    }

    public List<Rating>? GetByStarsGreaterEqlThan(int stars) {
        var ratings = new List<Rating>();
        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "SELECT uuid, media_entry_uuid, user_uuid, stars, comment, created_at, public_visible FROM ratings WHERE stars >= @stars",
            connection);
        cmd.Parameters.AddWithValue("stars", stars);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var rating = new Rating(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetInt32(3),
                reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                reader.GetDateTime(5),
                reader.GetBoolean(6)
            );
            ratings.Add(rating);
        }

        connection.Close();

        foreach (var rating in ratings)
        {
            LoadLikedBy(rating);
        }

        return ratings;
    }

    public List<Rating>? GetByStarsLowerEqlThan(int stars) {
        var ratings = new List<Rating>();
        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "SELECT uuid, media_entry_uuid, user_uuid, stars, comment, created_at, public_visible FROM ratings WHERE stars <= @stars",
            connection);
        cmd.Parameters.AddWithValue("stars", stars);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var rating = new Rating(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetInt32(3),
                reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                reader.GetDateTime(5),
                reader.GetBoolean(6)
            );
            ratings.Add(rating);
        }

        connection.Close();

        foreach (var rating in ratings)
        {
            LoadLikedBy(rating);
        }

        return ratings;
    }

    private void LoadLikedBy(Rating rating)
    {
        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "SELECT user_uuid FROM rating_likes WHERE rating_uuid = @ratingUuid",
            connection);
        cmd.Parameters.AddWithValue("ratingUuid", rating.uuid);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            rating.likedBy.Add(reader.GetGuid(0));
        }
    }

    private void SaveLikedBy(Rating rating)
    {
        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        foreach (var userId in rating.likedBy)
        {
            using var cmd = new NpgsqlCommand(
                "INSERT INTO rating_likes (rating_uuid, user_uuid) VALUES (@ratingUuid, @userUuid) ON CONFLICT DO NOTHING",
                connection);
            cmd.Parameters.AddWithValue("ratingUuid", rating.uuid);
            cmd.Parameters.AddWithValue("userUuid", userId);
            cmd.ExecuteNonQuery();
        }
    }
}