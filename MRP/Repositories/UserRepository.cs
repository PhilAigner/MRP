using Npgsql;

namespace MRP;

public class UserRepository : IUserRepository {

    private readonly DatabaseConnection _dbConnection;
    private readonly ProfileRepository _profileRepository;

    public UserRepository(DatabaseConnection dbConnection, ProfileRepository profileRepository)
    {
        _dbConnection = dbConnection;
        _profileRepository = profileRepository;
    }

    public List<User> GetAll()
    {
        var users = new List<User>();
        using var connection = _dbConnection.CreateConnection();
        connection.Open();

        using var cmd = new NpgsqlCommand("SELECT uuid, username, password, created FROM users", connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var profileUuid = _profileRepository.GetByOwnerId(reader.GetGuid(0))?.uuid ?? Guid.Empty;
            users.Add(new User(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetDateTime(3),
                profileUuid
            ));
        }

        return users;
    }

    public User? GetUserById(Guid id)
    {
        // Validate input
        if (id == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(id));

        try
        {
            using var connection = _dbConnection.CreateConnection();
            connection.Open();

            using var cmd = new NpgsqlCommand("SELECT uuid, username, password, created FROM users WHERE uuid = @uuid", connection);
            cmd.Parameters.AddWithValue("uuid", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var profileUuid = _profileRepository.GetByOwnerId(reader.GetGuid(0))?.uuid ?? Guid.Empty;
                return new User(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetDateTime(3),
                    profileUuid
                );
            }

            return null;
        }
        catch (NpgsqlException ex)
        {
            throw new DatabaseException(
                "Failed to retrieve user by ID",
                "SELECT",
                "users",
                ex
            );
        }
        catch (Exception ex)
        {
            throw new DatabaseException(
                "Unexpected error while retrieving user",
                ex
            );
        }
    }

    public Guid AddUser(User user)
    {
        // Validate input
        if (user == null)
            throw new ArgumentNullException(nameof(user), "User cannot be null");

        if (user.uuid == Guid.Empty)
            throw new ArgumentException("User UUID cannot be empty", nameof(user));

        if (string.IsNullOrWhiteSpace(user.username))
            throw new ArgumentException("Username cannot be empty", nameof(user));

        if (string.IsNullOrWhiteSpace(user.getPassword()))
            throw new ArgumentException("Password cannot be empty", nameof(user));

        try
        {
            using var connection = _dbConnection.CreateConnection();
            connection.Open();

            using var cmd = new NpgsqlCommand(
                "INSERT INTO users (uuid, username, password, created) VALUES (@uuid, @username, @password, @created) ON CONFLICT (uuid) DO NOTHING RETURNING uuid",
                connection);

            cmd.Parameters.AddWithValue("uuid", user.uuid);
            cmd.Parameters.AddWithValue("username", user.username);
            cmd.Parameters.AddWithValue("password", user.getPassword());
            cmd.Parameters.AddWithValue("created", user.created);

            var result = cmd.ExecuteScalar();
            return result != null ? (Guid)result : Guid.Empty;
        }
        catch (NpgsqlException ex)
        {
            throw new DatabaseException(
                "Failed to add user to database",
                "INSERT",
                "users",
                ex
            );
        }
        catch (Exception ex)
        {
            throw new DatabaseException(
                "Unexpected error while adding user",
                ex
            );
        }
    }

    public User? GetUserByUsername(string username)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));

        try
        {
            using var connection = _dbConnection.CreateConnection();
            connection.Open();

            using var cmd = new NpgsqlCommand("SELECT uuid, username, password, created FROM users WHERE username = @username", connection);
            cmd.Parameters.AddWithValue("username", username);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var profileUuid = _profileRepository.GetByOwnerId(reader.GetGuid(0))?.uuid ?? Guid.Empty;
                return new User(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetDateTime(3),
                    profileUuid
                );
            }

            return null;
        }
        catch (NpgsqlException ex)
        {
            throw new DatabaseException(
                "Failed to retrieve user by username",
                "SELECT",
                "users",
                ex
            );
        }
        catch (Exception ex)
        {
            throw new DatabaseException(
                "Unexpected error while retrieving user by username",
                ex
            );
        }
    }
}
