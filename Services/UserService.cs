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

            // Hash password before storing
            string hashedPassword = Services.PasswordHasher.Hash(password);

            //create new user with hashed password and temporary profileUuid
            User newUser = new User(username, hashedPassword, Guid.Empty);
            
            //save user first (must exist before profile due to foreign key)
            var userUuid = users.AddUser(newUser);
            if (userUuid == Guid.Empty) return Guid.Empty;

            //now create and save the profile (linked via user_uuid foreign key)
            Profile newProfile = new Profile(newUser.uuid);
            profileRepository.AddProfile(newProfile);

            return newUser.uuid;
        }

        public string? login(string _username, string _password)
        {
            var user = users.GetUserByUsername(_username);
            //test if user exists
            if (user == null) return null;

            //verify hashed password
            if (Services.PasswordHasher.Verify(_password, ((User)user).getPassword()))
            {
                // Update login count
                var profile = profileRepository.GetByOwnerId(user.uuid);
                if (profile != null)
                {
                    profile.numberOfLogins++;
                    profileRepository.UpdateProfile(profile);
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
            
            // Update the profile in the database
            return profileRepository.UpdateProfile(newProfile);
        }
    }
}