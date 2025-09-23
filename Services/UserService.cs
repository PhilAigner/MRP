using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRP
{
    internal class UserService
    {

        private UserRepository users;

        public UserService(UserRepository _users)
        {
            users = _users;
        }

        public Guid register(string username, string password)
        {
            var user = users.GetUserByUsername(username);
            //test if user exists
            if (user != null) return Guid.Empty;

            //create new user
            User newUser = new User(username, password);
            users.AddUser(newUser);

            return newUser.uuid;
        }

        public Boolean login(string _username, string _password)
        {
            var user = users.GetUserByUsername(_username);
            //test if user exists
            if (user == null) return false;

            //check password    TODO HASH
            if (((User)user).getPassword() == _password) return true;
            else return false;
        }
    }
}