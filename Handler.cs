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
            profileRepository = new ProfileRepository(dbConnection);
            userRepository = new UserRepository(dbConnection, profileRepository);
            mediaRepository = new MediaRepository(dbConnection, userRepository);
            ratingRepository = new RatingRepository(dbConnection);
            tokenService = new TokenService();
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

            // Server runs on localhost for local development
            await HttpServer.RunServer("http://127.0.0.1:8080/api/", HttpEndpoints);

            return 0;
        }
    }
}