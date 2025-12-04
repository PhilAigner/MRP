using Npgsql;

namespace MRP;

public class MediaRepository :  IMediaRepository {

    private readonly DatabaseConnection _dbConnection;
    private readonly UserRepository _userRepository;

    public MediaRepository(DatabaseConnection dbConnection, UserRepository userRepository)
    {
        _dbConnection = dbConnection;
        _userRepository = userRepository;
    }


    public List<MediaEntry> GetAll() {
        var mediaEntries = new List<MediaEntry>();
        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "SELECT uuid, title, description, media_type, release_year, age_restriction, genre, created_at, created_by_uuid FROM media_entries",
            connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var creatorUuid = reader.GetGuid(8);
            var creator = _userRepository.GetUserById(creatorUuid);
            if (creator != null)
            {
                mediaEntries.Add(new MediaEntry(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    Enum.Parse<EMediaType>(reader.GetString(3)),
                    reader.GetInt32(4),
                    Enum.Parse<EFSK>(reader.GetString(5)),
                    reader.GetString(6),
                    reader.GetDateTime(7),
                    creator
                ));
            }
        }

        return mediaEntries;
    }

    public MediaEntry? GetMediaById(Guid id) {
        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "SELECT uuid, title, description, media_type, release_year, age_restriction, genre, created_at, created_by_uuid FROM media_entries WHERE uuid = @uuid",
            connection);
        cmd.Parameters.AddWithValue("uuid", id);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var creatorUuid = reader.GetGuid(8);
            var creator = _userRepository.GetUserById(creatorUuid);
            if (creator != null)
            {
                return new MediaEntry(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    Enum.Parse<EMediaType>(reader.GetString(3)),
                    reader.GetInt32(4),
                    Enum.Parse<EFSK>(reader.GetString(5)),
                    reader.GetString(6),
                    reader.GetDateTime(7),
                    creator
                );
            }
        }

        return null;
    }

    public List<MediaEntry>? GetMediaByTitle(String title) {
        var mediaEntries = new List<MediaEntry>();
        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "SELECT uuid, title, description, media_type, release_year, age_restriction, genre, created_at, created_by_uuid FROM media_entries WHERE title = @title",
            connection);
        cmd.Parameters.AddWithValue("title", title);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var creatorUuid = reader.GetGuid(8);
            var creator = _userRepository.GetUserById(creatorUuid);
            if (creator != null)
            {
                mediaEntries.Add(new MediaEntry(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    Enum.Parse<EMediaType>(reader.GetString(3)),
                    reader.GetInt32(4),
                    Enum.Parse<EFSK>(reader.GetString(5)),
                    reader.GetString(6),
                    reader.GetDateTime(7),
                    creator
                ));
            }
        }

        return mediaEntries;
    }

    public List<MediaEntry>? GetMediaByCreator(Guid userid) {
        var mediaEntries = new List<MediaEntry>();
        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "SELECT uuid, title, description, media_type, release_year, age_restriction, genre, created_at, created_by_uuid FROM media_entries WHERE created_by_uuid = @userid",
            connection);
        cmd.Parameters.AddWithValue("userid", userid);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var creatorUuid = reader.GetGuid(8);
            var creator = _userRepository.GetUserById(creatorUuid);
            if (creator != null)
            {
                mediaEntries.Add(new MediaEntry(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    Enum.Parse<EMediaType>(reader.GetString(3)),
                    reader.GetInt32(4),
                    Enum.Parse<EFSK>(reader.GetString(5)),
                    reader.GetString(6),
                    reader.GetDateTime(7),
                    creator
                ));
            }
        }

        return mediaEntries;
    }

    public Guid AddMedia(MediaEntry mediaEntry) {
        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "INSERT INTO media_entries (uuid, title, description, media_type, release_year, age_restriction, genre, created_at, created_by_uuid) " +
            "VALUES (@uuid, @title, @description, @mediaType, @releaseYear, @ageRestriction, @genre, @createdAt, @createdBy) " +
            "ON CONFLICT (uuid) DO NOTHING RETURNING uuid",
            connection);

        cmd.Parameters.AddWithValue("uuid", mediaEntry.uuid);
        cmd.Parameters.AddWithValue("title", mediaEntry.title);
        cmd.Parameters.AddWithValue("description", mediaEntry.description);
        cmd.Parameters.AddWithValue("mediaType", mediaEntry.mediaType.ToString());
        cmd.Parameters.AddWithValue("releaseYear", mediaEntry.releaseYear);
        cmd.Parameters.AddWithValue("ageRestriction", mediaEntry.ageRestriction.ToString());
        cmd.Parameters.AddWithValue("genre", mediaEntry.genre);
        cmd.Parameters.AddWithValue("createdAt", mediaEntry.createdAt);
        cmd.Parameters.AddWithValue("createdBy", mediaEntry.createdBy.uuid);

        var result = cmd.ExecuteScalar();
        return result != null ? (Guid)result : Guid.Empty;
    }

    public bool UpdateMedia(MediaEntry mediaEntry)
    {
        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "UPDATE media_entries SET title = @title, description = @description, media_type = @mediaType, " +
            "release_year = @releaseYear, age_restriction = @ageRestriction, genre = @genre " +
            "WHERE uuid = @uuid",
            connection);

        cmd.Parameters.AddWithValue("uuid", mediaEntry.uuid);
        cmd.Parameters.AddWithValue("title", mediaEntry.title);
        cmd.Parameters.AddWithValue("description", mediaEntry.description);
        cmd.Parameters.AddWithValue("mediaType", mediaEntry.mediaType.ToString());
        cmd.Parameters.AddWithValue("releaseYear", mediaEntry.releaseYear);
        cmd.Parameters.AddWithValue("ageRestriction", mediaEntry.ageRestriction.ToString());
        cmd.Parameters.AddWithValue("genre", mediaEntry.genre);

        var rowsAffected = cmd.ExecuteNonQuery();
        return rowsAffected > 0;
    }

    public bool DeleteMedia(Guid id)
    {
        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand("DELETE FROM media_entries WHERE uuid = @uuid", connection);
        cmd.Parameters.AddWithValue("uuid", id);

        var rowsAffected = cmd.ExecuteNonQuery();
        return rowsAffected > 0;
    }
}