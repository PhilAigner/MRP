using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;
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

            //SAMPLE CODE TO SHOWCASE Repositories

            /*

            Console.WriteLine("\n---Users---");

            /////////////////////////////
            //create sample user entries
            userRepository.AddUser(new User("USer1", "pwd", profileRepository));
            userRepository.AddUser(new User("User2", "pwd2", profileRepository));
            userRepository.AddUser(new User("User3", "pwd3", profileRepository));

            //print all users
            List<User> users = userRepository.GetAll();
            foreach (var thing in users)
            {
                Console.WriteLine($"Username: {thing.username}, UUID: {thing.uuid}");
            }


            Console.WriteLine("\n---Media---");

            /////////////////////////////
            //create sample media entries
            Guid media1 = mediaRepository.AddMedia(new MediaEntry("Title1", EMediaType.Movie, 2001, EFSK.FSK0, "Action", users[0]));
            Guid media2 = mediaRepository.AddMedia(new MediaEntry("Title2", EMediaType.Movie, 2002, EFSK.FSK18, "Action", users[1]));
            Guid media3 = mediaRepository.AddMedia(new MediaEntry("Title3", EMediaType.Movie, 2003, EFSK.FSK18, "Action", users[2]));

            //print all media entries
            List<MediaEntry> media = mediaRepository.GetAll();
            foreach (var thing in media)
            {
                Console.WriteLine($"Title: {thing.title}, UUID: {thing.uuid}");
            }



            Console.WriteLine("\n---Ratings---");

            ////////////////////////////
            //create sample rating data
            ratingRepository.AddRating(new Rating(media[0].uuid, users[0].uuid, 1));
            ratingRepository.AddRating(new Rating(media[1].uuid, users[1].uuid, 2));
            ratingRepository.AddRating(new Rating(media[2].uuid, users[2].uuid, 3));

            //print all rating entries
            List<Rating> ratings = ratingRepository.GetAll();
            foreach (var thing in ratings)
            {
                Console.WriteLine($"Rating from: {thing.user} on {thing.mediaEntry} - {thing.stars} stars, with UUID: {thing.uuid}");
            }

            */

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

            // Ändern Sie localhost zu 0.0.0.0 für Container-Kompatibilität
            await HttpServer.RunServer("http://127.0.0.1:8080/api/", HttpEndpoints);

            return 0;
        }
    }
}