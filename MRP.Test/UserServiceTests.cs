using Moq;

namespace MRP.Test
{
    [TestFixture]
    public class UserServiceTests
    {
        private Mock<IUserRepository> _mockUserRepository;
        private Mock<IProfileRepository> _mockProfileRepository;
        private TokenService _tokenService;
        private UserService _userService;

        [SetUp]
        public void Setup()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockProfileRepository = new Mock<IProfileRepository>();
            _tokenService = new TokenService();
            _userService = new UserService(_mockUserRepository.Object, _mockProfileRepository.Object, _tokenService);
        }

        [Test]
        public void Register_NewUser_ReturnsUserId()
        {
            // Arrange
            string username = "newuser_123123123123";
            string password = "password123";
            Guid capturedUserId = Guid.Empty;

            _mockUserRepository.Setup(repo => repo.GetUserByUsername(username))
                .Returns((User?)null);

            _mockUserRepository.Setup(repo => repo.AddUser(It.IsAny<User>()))
                .Callback<User>(user => 
                {
                    // Capture the user's uuid (read-only property set in constructor)
                    capturedUserId = user.uuid;
                })
                .Returns<User>(user => user.uuid);

            _mockProfileRepository.Setup(repo => repo.AddProfile(It.IsAny<Profile>()))
                .Returns(Guid.NewGuid());

            // Act
            Guid userId = _userService.register(username, password);

            // Assert
            Assert.That(userId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(userId, Is.EqualTo(capturedUserId));
            _mockUserRepository.Verify(repo => repo.GetUserByUsername(username), Times.Once);
            _mockUserRepository.Verify(repo => repo.AddUser(It.IsAny<User>()), Times.Once);
            _mockProfileRepository.Verify(repo => repo.AddProfile(It.IsAny<Profile>()), Times.Once);
        }

        [Test]
        public void Register_ExistingUsername_ReturnsEmptyGuid()
        {
            // Arrange
            string username = "existinguser";
            string password = "password123";
            User existingUser = new User(username, "hashedpassword", Guid.NewGuid());

            _mockUserRepository.Setup(repo => repo.GetUserByUsername(username))
                .Returns(existingUser);

            // Act
            Guid userId = _userService.register(username, password);

            // Assert
            Assert.That(userId, Is.EqualTo(Guid.Empty));
            _mockUserRepository.Verify(repo => repo.GetUserByUsername(username), Times.Once);
            _mockUserRepository.Verify(repo => repo.AddUser(It.IsAny<User>()), Times.Never);
        }

        [Test]
        public void Login_ValidCredentials_ReturnsToken()
        {
            // Arrange
            string username = "testuser";
            string password = "password123";
            string hashedPassword = PasswordHasher.Hash(password);
            Guid userId = Guid.NewGuid();
            
            User user = new User(userId, username, hashedPassword, DateTime.Now, Guid.NewGuid());
            Profile profile = new Profile(userId);

            _mockUserRepository.Setup(repo => repo.GetUserByUsername(username))
                .Returns(user);

            _mockProfileRepository.Setup(repo => repo.GetByOwnerId(userId))
                .Returns(profile);

            _mockProfileRepository.Setup(repo => repo.UpdateProfile(It.IsAny<Profile>()))
                .Returns(true);

            // Act
            string? token = _userService.login(username, password);

            // Assert
            Assert.That(token, Is.Not.Null);
            Assert.That(token, Is.Not.Empty);
            Assert.That(token, Does.Contain(username));
            _mockProfileRepository.Verify(repo => repo.UpdateProfile(It.IsAny<Profile>()), Times.Once);
        }

        [Test]
        public void Login_InvalidUsername_ReturnsNull()
        {
            // Arrange
            string username = "nonexistentuser";
            string password = "password123";

            _mockUserRepository.Setup(repo => repo.GetUserByUsername(username))
                .Returns((User?)null);

            // Act
            string? token = _userService.login(username, password);

            // Assert
            Assert.That(token, Is.Null);
        }

        [Test]
        public void Login_InvalidPassword_ReturnsNull()
        {
            // Arrange
            string username = "testuser";
            string correctPassword = "correctpassword";
            string wrongPassword = "wrongpassword";
            string hashedPassword = PasswordHasher.Hash(correctPassword);
            Guid userId = Guid.NewGuid();
            
            User user = new User(userId, username, hashedPassword, DateTime.Now, Guid.NewGuid());

            _mockUserRepository.Setup(repo => repo.GetUserByUsername(username))
                .Returns(user);

            // Act
            string? token = _userService.login(username, wrongPassword);

            // Assert
            Assert.That(token, Is.Null);
        }
    }
}
