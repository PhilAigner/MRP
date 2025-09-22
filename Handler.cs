using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;

namespace MRP
{
    internal class Handler
    {
        
        private List<User> users = new List<User>();
        private List<MediaEntry> entries = new List<MediaEntry>();
        private List<Rating> ratings = new List<Rating>();

        public Handler()
        {
            users.Add(new User("USer1", "pwd"));
            users.Add(new User("User2", "pwd2"));
            users.Add(new User("User3", "pwd3"));

            entries.Add(new MediaEntry("Title1", MediaType.Movie, 2001, 18, "Action", users[0]));
            entries.Add(new MediaEntry("Title2", MediaType.Movie, 2002, 18, "Action", users[1]));
            entries.Add(new MediaEntry("Title3", MediaType.Movie, 2003, 18, "Action", users[2]));

            ratings.Add(new Rating(entries[0].uuid, users[0].uuid, 3));
        }

        public int Start()
        {
            //http Handler


            
            //testcode:
            Boolean res = login("123", "baum");
            res = login("USer1", "wrong");
            res = login("USer1", "pwd");
            Guid id = register("123", "baum");
            res = login("123", "baum");



            return 1;
        }


        public Guid register(string username, string password)
        {

            //create new user
            User newUser = new User(username, password);
            users.Add(newUser);

            return newUser.uuid;
        }

        public Boolean login(string _username, string _password)
        {
            var user = users.FirstOrDefault(u => u.username == _username);
            //test if user exists
            if (user == null) return false;

            //check password    TODO HASH
            if (((User)user).getPassword() == _password) return true;
            else return false;
        }
    }
}