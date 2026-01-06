using System;
using System.Runtime;
using Npgsql;

namespace MRP;

public class RatingRepository :  IRatingsRepository {

    private readonly DatabaseConnection _dbConnection;

    private const string SelectQuery = "SELECT uuid, media_entry_uuid, user_uuid, stars, comment, created_at, public_visible FROM ratings";

    public RatingRepository(DatabaseConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    private Rating MapReaderToRating(NpgsqlDataReader reader)
    {
        return new Rating(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetGuid(2),
            reader.GetInt32(3),
            reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            reader.GetDateTime(5),
            reader.GetBoolean(6)
        );
    }

    private List<Rating> ExecuteQuery(string query, Action<NpgsqlCommand>? addParameters = null)
    {
        var ratings = new List<Rating>();
        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(query, connection);
        addParameters?.Invoke(cmd);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            ratings.Add(MapReaderToRating(reader));
        }

        connection.Close();

        if (ratings.Any())
        {
            LoadLikedByBatch(ratings);
        }

        return ratings;
    }

    public List<Rating> GetAll()
    {
        return ExecuteQuery(SelectQuery);
    }

    public Rating? GetById(Guid id)
    {
        var results = ExecuteQuery(
            $"{SelectQuery} WHERE uuid = @uuid",
            cmd => cmd.Parameters.AddWithValue("uuid", id)
        );
        return results.FirstOrDefault();
    }

    public List<Rating>? GetByCreator(Guid userid)
    {
        return ExecuteQuery(
            $"{SelectQuery} WHERE user_uuid = @userid",
            cmd => cmd.Parameters.AddWithValue("userid", userid)
        );
    }

    public Rating? GetByMediaAndUser(Guid mediaId, Guid userId)
    {
        var results = ExecuteQuery(
            $"{SelectQuery} WHERE media_entry_uuid = @mediaId AND user_uuid = @userId",
            cmd =>
            {
                cmd.Parameters.AddWithValue("mediaId", mediaId);
                cmd.Parameters.AddWithValue("userId", userId);
            }
        );
        return results.FirstOrDefault();
    }

    public List<Rating>? GetByMedia(Guid mediaId)
    {
        return ExecuteQuery(
            $"{SelectQuery} WHERE media_entry_uuid = @mediaId",
            cmd => cmd.Parameters.AddWithValue("mediaId", mediaId)
        );
    }

    public Guid AddRating(Rating rating)
    {
        // Validate input
        if (rating == null)
            throw new ArgumentNullException(nameof(rating), "Rating cannot be null");

        if (rating.uuid == Guid.Empty)
            throw new ArgumentException("Rating UUID cannot be empty", nameof(rating));

        if (rating.mediaEntry == Guid.Empty)
            throw new ArgumentException("Media entry UUID cannot be empty", nameof(rating));

        if (rating.user == Guid.Empty)
            throw new ArgumentException("User UUID cannot be empty", nameof(rating));

        if (rating.stars < 1 || rating.stars > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Stars must be between 1 and 5");

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

    public List<Rating>? GetByStarsGreaterEqlThan(int stars)
    {
        return ExecuteQuery(
            $"{SelectQuery} WHERE stars >= @stars",
            cmd => cmd.Parameters.AddWithValue("stars", stars)
        );
    }

    public List<Rating>? GetByStarsLowerEqlThan(int stars)
    {
        return ExecuteQuery(
            $"{SelectQuery} WHERE stars <= @stars",
            cmd => cmd.Parameters.AddWithValue("stars", stars)
        );
    }

    private void LoadLikedByBatch(List<Rating> ratings)
    {
        if (!ratings.Any()) return;

        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        // Get all rating UUIDs
        var ratingIds = ratings.Select(r => r.uuid).ToArray();
        
        // Create SQL with IN clause for batch loading
        var placeholders = string.Join(",", ratingIds.Select((_, i) => $"@id{i}"));
        var query = $"SELECT rating_uuid, user_uuid FROM rating_likes WHERE rating_uuid IN ({placeholders})";

        using var cmd = new NpgsqlCommand(query, connection);
      
        // Add parameters
        for (int i = 0; i < ratingIds.Length; i++)
        {
            cmd.Parameters.AddWithValue($"@id{i}", ratingIds[i]);
        }

        using var reader = cmd.ExecuteReader();

        // Build dictionary for O(1) lookup
        var likesByRating = new Dictionary<Guid, List<Guid>>();
        while (reader.Read())
        {
            var ratingUuid = reader.GetGuid(0);
            var userUuid = reader.GetGuid(1);

            if (!likesByRating.ContainsKey(ratingUuid))
            {
                likesByRating[ratingUuid] = new List<Guid>();
            }
            likesByRating[ratingUuid].Add(userUuid);
        }

        // Assign likes to ratings
        foreach (var rating in ratings)
        {
            if (likesByRating.TryGetValue(rating.uuid, out var likes))
            {
                rating.likedBy.AddRange(likes);
            }
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

    public bool UpdateRating(Rating rating)
    {
        // Validate input
        if (rating == null)
            throw new ArgumentNullException(nameof(rating), "Rating cannot be null");

        if (rating.uuid == Guid.Empty)
            throw new ArgumentException("Rating UUID cannot be empty", nameof(rating));

        if (rating.stars < 1 || rating.stars > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Stars must be between 1 and 5");

        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "UPDATE ratings SET stars = @stars, comment = @comment, public_visible = @publicVisible " +
            "WHERE uuid = @uuid",
            connection);

        cmd.Parameters.AddWithValue("uuid", rating.uuid);
        cmd.Parameters.AddWithValue("stars", rating.stars);
        cmd.Parameters.AddWithValue("comment", rating.comment ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("publicVisible", rating.publicVisible);

        var rowsAffected = cmd.ExecuteNonQuery();
        return rowsAffected > 0;
    }

    public bool DeleteRating(Guid id)
    {
        // Validate input
        if (id == Guid.Empty)
            throw new ArgumentException("Rating ID cannot be empty", nameof(id));

        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        // Delete rating_likes first (if not using CASCADE)
        using var deleteLikesCmd = new NpgsqlCommand(
            "DELETE FROM rating_likes WHERE rating_uuid = @uuid",
            connection);
        deleteLikesCmd.Parameters.AddWithValue("uuid", id);
        deleteLikesCmd.ExecuteNonQuery();

        // Delete rating
        using var cmd = new NpgsqlCommand("DELETE FROM ratings WHERE uuid = @uuid", connection);
        cmd.Parameters.AddWithValue("uuid", id);

        var rowsAffected = cmd.ExecuteNonQuery();
        return rowsAffected > 0;
    }

    public bool AddLike(Guid ratingId, Guid userId)
    {
        // Validate input
        if (ratingId == Guid.Empty)
            throw new ArgumentException("Rating ID cannot be empty", nameof(ratingId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "INSERT INTO rating_likes (rating_uuid, user_uuid) VALUES (@ratingUuid, @userUuid) ON CONFLICT DO NOTHING",
            connection);
        cmd.Parameters.AddWithValue("ratingUuid", ratingId);
        cmd.Parameters.AddWithValue("userUuid", userId);

        var rowsAffected = cmd.ExecuteNonQuery();
        return rowsAffected > 0;
    }

    public bool RemoveLike(Guid ratingId, Guid userId)
    {
        // Validate input
        if (ratingId == Guid.Empty)
            throw new ArgumentException("Rating ID cannot be empty", nameof(ratingId));
    
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "DELETE FROM rating_likes WHERE rating_uuid = @ratingUuid AND user_uuid = @userUuid",
            connection);
        cmd.Parameters.AddWithValue("ratingUuid", ratingId);
        cmd.Parameters.AddWithValue("userUuid", userId);

        var rowsAffected = cmd.ExecuteNonQuery();
        return rowsAffected > 0;
    }
}