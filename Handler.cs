using HttpServerDemo.WeatherServer;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;

namespace MRP
{
    internal class Handler
    {
        private UserRepository userRepository = new UserRepository();

        private MediaRepository mediaRepository = new MediaRepository();

        private RatingRepository ratingRepository = new RatingRepository();

        private ProfileRepository profileRepository = new ProfileRepository();


        public Handler()
        {
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


            //testcode:
            //UserService userService = new UserService(userRepository, profileRepository);

            /*
            Boolean res = userService.login("123", "baum");
            res = userService.login("USer1", "wrong");
            res = userService.login("USer1", "pwd");
            Guid id = userService.register("123", "baum");
            res = userService.login("123", "baum");
            id = userService.register("123", "baum");
            */

            List<IHttpEndpoint> httpEndpoints = new List<IHttpEndpoint>
            {
                new WeatherServiceEndpoint(),
            };


            await HttpServer.RunServer("http://localhost:8080/", httpEndpoints);


            return 0;
        }
    }
}