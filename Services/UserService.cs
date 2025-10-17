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
        private ProfileRepository profileRepository;
        private TokenService tokenService;

        public UserService(UserRepository _users, ProfileRepository _profileRepository, TokenService _tokenService)
        {
            users = _users;
            profileRepository = _profileRepository;
            tokenService = _tokenService;
        }

        public Guid register(string username, string password)
        {
            var user = users.GetUserByUsername(username);
            //test if user exists
            if (user != null) return Guid.Empty;

            //create new user
            User newUser = new User(username, password, profileRepository);
            users.AddUser(newUser);

            return newUser.uuid;
        }

        public string? login(string _username, string _password)
        {
            var user = users.GetUserByUsername(_username);
            //test if user exists
            if (user == null) return null;

            //check password    TODO HASH
            if (((User)user).getPassword() == _password)
            {
                // Update login count
                var profile = profileRepository.GetByOwnerId(user.uuid);
                if (profile != null)
                {
                    profile.numberOfLogins++;
                }
                
                // Generate and return token
                return tokenService.GenerateToken(_username, user.uuid);
            }
            else return null;
        }

        public Profile? getProfile(Guid userId)
        {
            var profile = profileRepository.GetByOwnerId(userId);
            return profile;
        }

        public bool editProfile(Profile newProfile) {
            var profile = profileRepository.GetByOwnerId(newProfile.user);
            if (profile == null) return false;
            profileRepository.GetAll().Remove(profile);
            profileRepository.GetAll().Add(newProfile);
            return true;
        }
    }
}