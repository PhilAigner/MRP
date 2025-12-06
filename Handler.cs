using DotNetEnv;

namespace MRP
{
    internal class Handler
    {
        private DatabaseConnection dbConnection;
        private ProfileRepository profileRepository;
        private UserRepository userRepository;
        private MediaRepository mediaRepository;
        private RatingRepository ratingRepository;
        private TokenService tokenService;


        public Handler()
        {
            // Load environment variables from .env file
            Env.Load();

            // Get database credentials from environment variables
            string db_host = Env.GetString("DB_HOST");
            int db_port = Env.GetInt("DB_PORT");
            string db_database = Env.GetString("DB_NAME");
            string db_username = Env.GetString("DB_USER");
            string db_password = Env.GetString("DB_PASSWORD");

            string connectionString = DatabaseConnection.BuildConnectionString(
                host: db_host,
                port: db_port,
                database: db_database,
                username: db_username,
                password: db_password
            );

            dbConnection = new DatabaseConnection(connectionString);
            
            // Test database connection
            if (!TestDatabaseConnection())
            {
                throw new Exception($"Datenbankverbindung zu {db_host}:{db_port}/{db_database} fehlgeschlagen. Bitte überprüfen Sie die Verbindungsparameter und stellen Sie sicher, dass die Datenbank erreichbar ist.");
            }
            
            Console.WriteLine($"✓ Datenbankverbindung erfolgreich hergestellt ({db_host}:{db_port}/{db_database})");
  
            profileRepository = new ProfileRepository(dbConnection);
            userRepository = new UserRepository(dbConnection, profileRepository);
            mediaRepository = new MediaRepository(dbConnection, userRepository);
            ratingRepository = new RatingRepository(dbConnection);
            tokenService = new TokenService();
        }

        private bool TestDatabaseConnection()
        {
            try
            {
                return dbConnection.TestConnectionAsync().GetAwaiter().GetResult();
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> StartAsync()
        {
            List<IHttpEndpoint> HttpEndpoints = new List<IHttpEndpoint>
            {
                // User endpoints (login and register don't require authentication)
                new UserLoginHTTPEndpoint(userRepository, profileRepository, tokenService),
                new UserRegisterHTTPEndpoint(userRepository, profileRepository, tokenService),
                new UserProfileHTTPEndpoint(userRepository, profileRepository, ratingRepository, mediaRepository, tokenService),
                
                // Media endpoint (requires authentication)
                new MediaHTTPEndpoint(mediaRepository, userRepository, ratingRepository, profileRepository, tokenService),
                
                // Ratings endpoint (requires authentication)
                new RatingsHTTPEndpoint(ratingRepository, userRepository, mediaRepository, profileRepository, tokenService),
            };

            await HttpServer.RunServer("http://127.0.0.1:8080/api/", HttpEndpoints);

            return 0;
        }
    }
}