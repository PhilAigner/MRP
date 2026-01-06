using Npgsql;

namespace MRP;

public class MediaRepository :  IMediaRepository {

    private readonly DatabaseConnection _dbConnection;
    private readonly UserRepository _userRepository;

    private const string SelectQuery = "SELECT uuid, title, description, media_type, release_year, age_restriction, genre, created_at, created_by_uuid FROM media_entries";

    public MediaRepository(DatabaseConnection dbConnection, UserRepository userRepository)
    {
        _dbConnection = dbConnection;
        _userRepository = userRepository;
    }

    private MediaEntry? MapReaderToMediaEntry(NpgsqlDataReader reader)
    {
        var creatorUuid = reader.GetGuid(8);
        var creator = _userRepository.GetUserById(creatorUuid);
        
        if (creator == null) return null;

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

    private List<MediaEntry> ExecuteQuery(string query, Action<NpgsqlCommand>? addParameters = null)
    {
        var mediaEntries = new List<MediaEntry>();
        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(query, connection);
        addParameters?.Invoke(cmd);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var entry = MapReaderToMediaEntry(reader);
            if (entry != null)
            {
                mediaEntries.Add(entry);
            }
        }

        return mediaEntries;
    }

    public List<MediaEntry> GetAll()
    {
        return ExecuteQuery(SelectQuery);
    }

    public MediaEntry? GetMediaById(Guid id)
    {
        // Validate input
        if (id == Guid.Empty)
            throw new ArgumentException("Media ID cannot be empty", nameof(id));

        try
        {
            var results = ExecuteQuery(
                $"{SelectQuery} WHERE uuid = @uuid",
                cmd => cmd.Parameters.AddWithValue("uuid", id)
            );
            return results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            throw new 
                (
                "Failed to retrieve media entry by ID",
                ex
            );
        }
    }

    public List<MediaEntry>? GetMediaByTitle(string title)
    {
        return ExecuteQuery(
            $"{SelectQuery} WHERE title = @title",
            cmd => cmd.Parameters.AddWithValue("title", title)
        );
    }

    public List<MediaEntry>? GetMediaByCreator(Guid userid)
    {
        return ExecuteQuery(
            $"{SelectQuery} WHERE created_by_uuid = @userid",
            cmd => cmd.Parameters.AddWithValue("userid", userid)
        );
    }

    public Guid AddMedia(MediaEntry mediaEntry)
    {
        // Validate input
        if (mediaEntry == null)
            throw new ArgumentNullException(nameof(mediaEntry), "Media entry cannot be null");

        if (mediaEntry.uuid == Guid.Empty)
            throw new ArgumentException("Media UUID cannot be empty", nameof(mediaEntry));

        if (string.IsNullOrWhiteSpace(mediaEntry.title))
            throw new ArgumentException("Title cannot be empty", nameof(mediaEntry));

        if (mediaEntry.createdBy == null || mediaEntry.createdBy.uuid == Guid.Empty)
            throw new ArgumentException("Creator user cannot be null or empty", nameof(mediaEntry));

        if (mediaEntry.releaseYear < 1800 || mediaEntry.releaseYear > 2200)
            throw new ArgumentOutOfRangeException(nameof(mediaEntry), "Release year must be between 1800 and 2200");

        try
        {
            using var connection = _dbConnection.CreateConnection();
            connection.Open();

            using var cmd = new NpgsqlCommand(
                "INSERT INTO media_entries (uuid, title, description, media_type, release_year, age_restriction, genre, created_at, created_by_uuid) " +
                "VALUES (@uuid, @title, @description, @mediaType, @releaseYear, @ageRestriction, @genre, @createdAt, @createdBy) " +
                "ON CONFLICT (uuid) DO NOTHING RETURNING uuid",
                connection);

            cmd.Parameters.AddWithValue("uuid", mediaEntry.uuid);
            cmd.Parameters.AddWithValue("title", mediaEntry.title);
            cmd.Parameters.AddWithValue("description", mediaEntry.description ?? string.Empty);
            cmd.Parameters.AddWithValue("mediaType", mediaEntry.mediaType.ToString());
            cmd.Parameters.AddWithValue("releaseYear", mediaEntry.releaseYear);
            cmd.Parameters.AddWithValue("ageRestriction", mediaEntry.ageRestriction.ToString());
            cmd.Parameters.AddWithValue("genre", mediaEntry.genre ?? string.Empty);
            cmd.Parameters.AddWithValue("createdAt", mediaEntry.createdAt);
            cmd.Parameters.AddWithValue("createdBy", mediaEntry.createdBy.uuid);

            var result = cmd.ExecuteScalar();
            return result != null ? (Guid)result : Guid.Empty;
        }
        catch (NpgsqlException ex)
        {
            throw new DatabaseException(
                "Failed to add media entry to database",
                "INSERT",
                "media_entries",
                ex
            );
        }
        catch (Exception ex)
        {
            throw new DatabaseException(
                "Unexpected error while adding media entry",
                ex
            );
        }
    }

    public bool UpdateMedia(MediaEntry mediaEntry)
    {
        // Validate input
        if (mediaEntry == null)
            throw new ArgumentNullException(nameof(mediaEntry), "Media entry cannot be null");

        if (mediaEntry.uuid == Guid.Empty)
            throw new ArgumentException("Media UUID cannot be empty", nameof(mediaEntry));

        if (string.IsNullOrWhiteSpace(mediaEntry.title))
            throw new ArgumentException("Title cannot be empty", nameof(mediaEntry));

        if (mediaEntry.releaseYear < 1800 || mediaEntry.releaseYear > 2200)
            throw new ArgumentOutOfRangeException(nameof(mediaEntry), "Release year must be between 1800 and 2200");

        try
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
            cmd.Parameters.AddWithValue("description", mediaEntry.description ?? string.Empty);
            cmd.Parameters.AddWithValue("mediaType", mediaEntry.mediaType.ToString());
            cmd.Parameters.AddWithValue("releaseYear", mediaEntry.releaseYear);
            cmd.Parameters.AddWithValue("ageRestriction", mediaEntry.ageRestriction.ToString());
            cmd.Parameters.AddWithValue("genre", mediaEntry.genre ?? string.Empty);

            var rowsAffected = cmd.ExecuteNonQuery();
            return rowsAffected > 0;
        }
        catch (NpgsqlException ex)
        {
            throw new DatabaseException(
                "Failed to update media entry in database",
                "UPDATE",
                "media_entries",
                ex
            );
        }
        catch (Exception ex)
        {
            throw new DatabaseException(
                "Unexpected error while updating media entry",
                ex
            );
        }
    }

    public bool DeleteMedia(Guid id)
    {
        // Validate input
        if (id == Guid.Empty)
            throw new ArgumentException("Media ID cannot be empty", nameof(id));

        try
        {
            using var connection = _dbConnection.CreateConnection();
            connection.Open();

            using var cmd = new NpgsqlCommand("DELETE FROM media_entries WHERE uuid = @uuid", connection);
            cmd.Parameters.AddWithValue("uuid", id);

            var rowsAffected = cmd.ExecuteNonQuery();
            return rowsAffected > 0;
        }
        catch (NpgsqlException ex)
        {
            throw new DatabaseException(
                "Failed to delete media entry from database",
                "DELETE",
                "media_entries",
                ex
            );
        }
        catch (Exception ex)
        {
            throw new DatabaseException(
                "Unexpected error while deleting media entry",
                ex
            );
        }
    }
}
