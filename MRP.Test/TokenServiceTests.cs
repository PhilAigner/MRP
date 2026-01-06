using Moq;

namespace MRP.Tests
{
    [TestFixture]
    public class TokenServiceTests
    {
        private TokenService _tokenService;

        [SetUp]
        public void Setup()
        {
            _tokenService = new TokenService();
        }

        [Test]
        public void GenerateToken_ValidUsername_ReturnsToken()
        {
            // Arrange
            string username = "testuser";
            Guid userId = Guid.NewGuid();

            // Act
            string token = _tokenService.GenerateToken(username, userId);

            // Assert
            Assert.That(token, Is.Not.Null);
            Assert.That(token, Is.Not.Empty);
            Assert.That(token, Does.Contain(username));
            Assert.That(token, Does.Contain("-token"));
        }

        [Test]
        public void GenerateToken_ExistingUser_ReturnsSameToken()
        {
            // Arrange
            string username = "testuser";
            Guid userId = Guid.NewGuid();

            // Act
            string token1 = _tokenService.GenerateToken(username, userId);
            string token2 = _tokenService.GenerateToken(username, userId);

            // Assert
            Assert.That(token1, Is.EqualTo(token2));
        }

        [Test]
        public void ValidateToken_ValidToken_ReturnsUserId()
        {
            // Arrange
            string username = "testuser";
            Guid userId = Guid.NewGuid();
            string token = _tokenService.GenerateToken(username, userId);

            // Act
            Guid? validatedUserId = _tokenService.ValidateToken(token);

            // Assert
            Assert.That(validatedUserId, Is.Not.Null);
            Assert.That(validatedUserId, Is.EqualTo(userId));
        }

        [Test]
        public void ExtractBearerToken_ValidHeader_ReturnsToken()
        {
            // Arrange
            string token = "abc123-token";
            string authHeader = $"Bearer {token}";

            // Act
            string? extractedToken = _tokenService.ExtractBearerToken(authHeader);

            // Assert
            Assert.That(extractedToken, Is.Not.Null);
            Assert.That(extractedToken, Is.EqualTo(token));
        }

        [Test]
        public void RevokeToken_ValidToken_RemovesToken()
        {
            // Arrange
            string username = "testuser";
            Guid userId = Guid.NewGuid();
            string token = _tokenService.GenerateToken(username, userId);

            // Act
            bool revoked = _tokenService.RevokeToken(token);
            Guid? validatedUserId = _tokenService.ValidateToken(token);

            // Assert
            Assert.That(revoked, Is.True);
            Assert.That(validatedUserId, Is.Null);
        }
    }
}
