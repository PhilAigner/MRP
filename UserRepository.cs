using Dapper;

namespace MRP
{
    public class UserRepository : IDisposable
    {
        private readonly DbConnectionFactory _dbFactory;

        public UserRepository(DbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        // Tabelle anlegen
        public async Task InitAsync()
        {
            await using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();

            var sql = """
                CREATE TABLE IF NOT EXISTS users (
                    uuid UUID PRIMARY KEY,
                    username TEXT NOT NULL UNIQUE,
                    password TEXT NOT NULL,
                    created TIMESTAMPTZ NOT NULL
                );
            """;
            await conn.ExecuteAsync(sql);
        }

        public async Task DropUserTableAsync()
        {
            await using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();

            var sql = """
                DROP TABLE IF EXISTS users;
            """;
            await conn.ExecuteAsync(sql);
        }



        // Benutzer einfügen
        public async Task<Guid> InsertUserAsync(string username, string password)
        {
            var user = new User(username, password);

            await using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();

            var sql = """
                INSERT INTO users(uuid, username, password, created)
                VALUES (@uuid, @username, @password, @created)
                RETURNING uuid;
            """;
            return await conn.ExecuteScalarAsync<Guid>(sql, new
            {
                uuid = user.uuid,
                username = user.username,
                password = user.getPassword(),
                created = user.created
            });
        }

        // Benutzer lesen
        // UserRepository: User-Objekt mit allen Feldern erzeugen
        public async Task<User?> GetUserAsync(Guid uuid)
        {
            await using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();

            var sql = "SELECT uuid, username, password, created FROM users WHERE uuid = @uuid;";
            var dbUser = await conn.QuerySingleOrDefaultAsync(sql, new { uuid });

            if (dbUser == null)
                return null;

            return new User((Guid)dbUser.uuid, (string)dbUser.username, (string)dbUser.password, (DateTime)dbUser.created);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            await using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();

            var sql = "SELECT uuid, username, password, created FROM users ORDER BY created;";
            var dbUsers = await conn.QueryAsync(sql);

            var users = new List<User>();
            foreach (var dbUser in dbUsers)
            {
                users.Add(new User((Guid)dbUser.uuid, (string)dbUser.username, (string)dbUser.password, (DateTime)dbUser.created));
            }
            return users;
        }

        // Benutzer löschen
        public async Task<int> DeleteUserAsync(Guid uuid)
        {
            await using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();

            var sql = "DELETE FROM users WHERE uuid = @uuid;";
            return await conn.ExecuteAsync(sql, new { uuid });
        }

        public void Dispose() { }
    }
}
