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


        private List<Rating> ratings = new List<Rating>();

        public Handler()
        {
            Console.WriteLine("---Users---");

            /////////////////////////////
            //create sample user entries
            userRepository.AddUser(new User("USer1", "pwd"));
            userRepository.AddUser(new User("User2", "pwd2"));
            userRepository.AddUser(new User("User3", "pwd3"));

            //print all users
            List<User> users = userRepository.GetAll();
            foreach (var user in users)
            {
                Console.WriteLine($"Username: {user.username}, UUID: {user.uuid}");
            }


            Console.WriteLine("---Media---");

            /////////////////////////////
            //create sample media entries
            mediaRepository.AddMedia(new MediaEntry("Title1", EMediaType.Movie, 2001, EFSK.FSK0, "Action", users[0]));
            mediaRepository.AddMedia(new MediaEntry("Title2", EMediaType.Movie, 2002, EFSK.FSK18, "Action", users[1]));
            mediaRepository.AddMedia(new MediaEntry("Title3", EMediaType.Movie, 2003, EFSK.FSK18, "Action", users[2]));

            //print all media entries
            List<MediaEntry> media = mediaRepository.GetAll();
            foreach (var thing in media)
            {
                Console.WriteLine($"Title: {thing.title}, UUID: {thing.uuid}");
            }


            ////////////////////////////
            //create sample rating data

            //TODO
            //ratings.Add(new Rating(mediaRepository.GetMediaByTitle("Title1"), users[0].uuid, 3));
        }

        public int Start()
        {
            //http Handler

            //testcode:
            UserService userService = new UserService(userRepository);

            Boolean res = userService.login("123", "baum");
            res = userService.login("USer1", "wrong");
            res = userService.login("USer1", "pwd");
            Guid id = userService.register("123", "baum");
            res = userService.login("123", "baum");
            id = userService.register("123", "baum");



            return 1;
        }



    }
}