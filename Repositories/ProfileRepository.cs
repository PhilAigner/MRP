using Npgsql;

namespace MRP;

public class ProfileRepository :  IProfileRepository {

    private readonly DatabaseConnection _dbConnection;

    public ProfileRepository(DatabaseConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public List<Profile> GetAll() { 
        var profiles = new List<Profile>();
        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "SELECT uuid, user_uuid, number_of_logins, number_of_ratings_given, number_of_media_added, " +
            "number_of_reviews_written, favorite_genre, favorite_media_type, sobriquet, about_me FROM profiles",
            connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            profiles.Add(new Profile(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetInt32(2),
                reader.GetInt32(3),
                reader.GetInt32(4),
                reader.GetInt32(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.GetString(9)
            ));
        }

        return profiles;
    }

    public Guid AddProfile(Profile profile) {
        // Validate input
        if (profile == null)
            throw new ArgumentNullException(nameof(profile), "Profile cannot be null");
        
        if (profile.uuid == Guid.Empty)
            throw new ArgumentException("Profile UUID cannot be empty", nameof(profile));
        
        if (profile.user == Guid.Empty)
            throw new ArgumentException("User UUID cannot be empty", nameof(profile));
        
        // Validate statistics are non-negative
        if (profile.numberOfLogins < 0)
            throw new ArgumentOutOfRangeException(nameof(profile), "Number of logins cannot be negative");
        
        if (profile.numberOfRatingsGiven < 0)
            throw new ArgumentOutOfRangeException(nameof(profile), "Number of ratings given cannot be negative");
        
        if (profile.numberOfMediaAdded < 0)
            throw new ArgumentOutOfRangeException(nameof(profile), "Number of media added cannot be negative");
        
        if (profile.numberOfReviewsWritten < 0)
            throw new ArgumentOutOfRangeException(nameof(profile), "Number of reviews written cannot be negative");

        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "INSERT INTO profiles (uuid, user_uuid, number_of_logins, number_of_ratings_given, " +
            "number_of_media_added, number_of_reviews_written, favorite_genre, favorite_media_type, " +
            "sobriquet, about_me) VALUES (@uuid, @user_uuid, @logins, @ratings, @media, @reviews, " +
            "@genre, @mediaType, @sobriquet, @aboutMe) ON CONFLICT (uuid) DO NOTHING RETURNING uuid",
            connection);

        cmd.Parameters.AddWithValue("uuid", profile.uuid);
        cmd.Parameters.AddWithValue("user_uuid", profile.user);
        cmd.Parameters.AddWithValue("logins", profile.numberOfLogins);
        cmd.Parameters.AddWithValue("ratings", profile.numberOfRatingsGiven);
        cmd.Parameters.AddWithValue("media", profile.numberOfMediaAdded);
        cmd.Parameters.AddWithValue("reviews", profile.numberOfReviewsWritten);
        cmd.Parameters.AddWithValue("genre", profile.favoriteGenre ?? string.Empty);
        cmd.Parameters.AddWithValue("mediaType", profile.favoriteMediaType ?? string.Empty);
        cmd.Parameters.AddWithValue("sobriquet", profile.sobriquet ?? string.Empty);
        cmd.Parameters.AddWithValue("aboutMe", profile.aboutMe ?? string.Empty);

        var result = cmd.ExecuteScalar();
        return result != null ? (Guid)result : Guid.Empty;
    }

    public Profile? GetById(Guid id)
    {
        // Validate input
        if (id == Guid.Empty)
   throw new ArgumentException("Profile ID cannot be empty", nameof(id));

    using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "SELECT uuid, user_uuid, number_of_logins, number_of_ratings_given, number_of_media_added, " +
            "number_of_reviews_written, favorite_genre, favorite_media_type, sobriquet, about_me FROM profiles WHERE uuid = @uuid",
            connection);
        cmd.Parameters.AddWithValue("uuid", id);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Profile(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetInt32(2),
                reader.GetInt32(3),
                reader.GetInt32(4),
                reader.GetInt32(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.GetString(9)
            );
        }

        return null;
    }


    public Profile? GetByOwnerId(Guid userid)
    {
        // Validate input
        if (userid == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userid));

      using var connection = _dbConnection.CreateConnection();
      connection.Open();

        using var cmd = new NpgsqlCommand(
            "SELECT uuid, user_uuid, number_of_logins, number_of_ratings_given, number_of_media_added, " +
            "number_of_reviews_written, favorite_genre, favorite_media_type, sobriquet, about_me FROM profiles WHERE user_uuid = @user_uuid",
            connection);
        cmd.Parameters.AddWithValue("user_uuid", userid);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Profile(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetInt32(2),
                reader.GetInt32(3),
                reader.GetInt32(4),
                reader.GetInt32(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.GetString(9)
            );
        }

        return null;
    }

    public bool UpdateProfile(Profile profile)
    {
        // Validate input
        if (profile == null)
            throw new ArgumentNullException(nameof(profile), "Profile cannot be null");
        
        if (profile.uuid == Guid.Empty)
            throw new ArgumentException("Profile UUID cannot be empty", nameof(profile));
        
        // Validate statistics are non-negative
        if (profile.numberOfLogins < 0)
  throw new ArgumentOutOfRangeException(nameof(profile), "Number of logins cannot be negative");
    
    if (profile.numberOfRatingsGiven < 0)
        throw new ArgumentOutOfRangeException(nameof(profile), "Number of ratings given cannot be negative");
  
    if (profile.numberOfMediaAdded < 0)
     throw new ArgumentOutOfRangeException(nameof(profile), "Number of media added cannot be negative");
    
  if (profile.numberOfReviewsWritten < 0)
        throw new ArgumentOutOfRangeException(nameof(profile), "Number of reviews written cannot be negative");

        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand(
            "UPDATE profiles SET number_of_logins = @logins, number_of_ratings_given = @ratings, " +
            "number_of_media_added = @media, number_of_reviews_written = @reviews, " +
   "favorite_genre = @genre, favorite_media_type = @mediaType, " +
   "sobriquet = @sobriquet, about_me = @aboutMe " +
 "WHERE uuid = @uuid",
      connection);

    cmd.Parameters.AddWithValue("uuid", profile.uuid);
    cmd.Parameters.AddWithValue("logins", profile.numberOfLogins);
    cmd.Parameters.AddWithValue("ratings", profile.numberOfRatingsGiven);
    cmd.Parameters.AddWithValue("media", profile.numberOfMediaAdded);
    cmd.Parameters.AddWithValue("reviews", profile.numberOfReviewsWritten);
    cmd.Parameters.AddWithValue("genre", profile.favoriteGenre ?? string.Empty);
    cmd.Parameters.AddWithValue("mediaType", profile.favoriteMediaType ?? string.Empty);
    cmd.Parameters.AddWithValue("sobriquet", profile.sobriquet ?? string.Empty);
    cmd.Parameters.AddWithValue("aboutMe", profile.aboutMe ?? string.Empty);

    var rowsAffected = cmd.ExecuteNonQuery();
    return rowsAffected > 0;
    }
}