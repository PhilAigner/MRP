namespace MRP.Tests
{
    [TestFixture]
    public class PasswordHasherTests
    {
        [Test]
        public void Hash_ValidPassword_ReturnsHashedPassword()
        {
            // Arrange
            string password = "MySecurePassword123!";

            // Act
            string hashedPassword = PasswordHasher.Hash(password);

            // Assert
            Assert.That(hashedPassword, Is.Not.Null);
            Assert.That(hashedPassword, Is.Not.Empty);
            Assert.That(hashedPassword, Is.Not.EqualTo(password));
            Assert.That(hashedPassword, Does.StartWith("$2"));  // BCrypt hash starts with $2
        }

        [Test]
        public void Verify_CorrectPassword_ReturnsTrue()
        {
            // Arrange
            string password = "MySecurePassword123!";
            string hashedPassword = PasswordHasher.Hash(password);

            // Act
            bool isValid = PasswordHasher.Verify(password, hashedPassword);

            // Assert
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void Verify_IncorrectPassword_ReturnsFalse()
        {
            // Arrange
            string correctPassword = "MySecurePassword123!";
            string wrongPassword = "WrongPassword456!";
            string hashedPassword = PasswordHasher.Hash(correctPassword);

            // Act
            bool isValid = PasswordHasher.Verify(wrongPassword, hashedPassword);

            // Assert
            Assert.That(isValid, Is.False);
        }
    }
}
