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

    public Guid AddUser(User user)
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

    internal User? GetUserByUsername(string username)
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
}