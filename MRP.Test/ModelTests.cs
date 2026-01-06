namespace MRP.Test
{
    [TestFixture]
    public class UserModelTests
    {
        [Test]
        public void User_Constructor_InitializesCorrectly()
        {
            // Arrange
            string username = "testuser";
            string password = "testpassword";
            Guid profileUuid = Guid.NewGuid();

            // Act
            User user = new User(username, password, profileUuid);

            // Assert
            Assert.That(user.uuid, Is.Not.EqualTo(Guid.Empty));
            Assert.That(user.username, Is.EqualTo(username));
            Assert.That(user.getPassword(), Is.EqualTo(password));
            Assert.That(user.profileUuid, Is.EqualTo(profileUuid));
            Assert.That(user.created, Is.Not.EqualTo(default(DateTime)));
            Assert.That(user.created, Is.LessThanOrEqualTo(DateTime.Now));
        }

        [Test]
        public void User_FullConstructor_InitializesCorrectly()
        {
            // Arrange
            Guid uuid = Guid.NewGuid();
            string username = "testuser";
            string password = "hashedpassword";
            DateTime created = new DateTime(2024, 1, 1);
            Guid profileUuid = Guid.NewGuid();

            // Act
            User user = new User(uuid, username, password, created, profileUuid);

            // Assert
            Assert.That(user.uuid, Is.EqualTo(uuid));
            Assert.That(user.username, Is.EqualTo(username));
            Assert.That(user.getPassword(), Is.EqualTo(password));
            Assert.That(user.created, Is.EqualTo(created));
            Assert.That(user.profileUuid, Is.EqualTo(profileUuid));
        }
    }

    [TestFixture]
    public class ProfileModelTests
    {
        [Test]
        public void Profile_Constructor_InitializesWithDefaults()
        {
            // Arrange
            Guid ownerId = Guid.NewGuid();

            // Act
            Profile profile = new Profile(ownerId);

            // Assert
            Assert.That(profile.uuid, Is.Not.EqualTo(Guid.Empty));
            Assert.That(profile.user, Is.EqualTo(ownerId));
            Assert.That(profile.numberOfLogins, Is.EqualTo(0));
            Assert.That(profile.numberOfRatingsGiven, Is.EqualTo(0));
            Assert.That(profile.numberOfMediaAdded, Is.EqualTo(0));
            Assert.That(profile.numberOfReviewsWritten, Is.EqualTo(0));
            Assert.That(profile.favoriteGenre, Is.Empty);
            Assert.That(profile.favoriteMediaType, Is.Empty);
            Assert.That(profile.sobriquet, Is.Empty);
            Assert.That(profile.aboutMe, Is.Empty);
        }
    }

    [TestFixture]
    public class MediaEntryModelTests
    {
        [Test]
        public void MediaEntry_Constructor_InitializesCorrectly()
        {
            // Arrange
            string title = "Test Movie";
            EMediaType mediaType = EMediaType.Movie;
            int releaseYear = 2024;
            EFSK ageRestriction = EFSK.FSK12;
            string genre = "Action";
            User creator = new User("creator", "password", Guid.NewGuid());
            string description = "A test movie";

            // Act
            MediaEntry media = new MediaEntry(title, mediaType, releaseYear, ageRestriction, genre, creator, description);

            // Assert
            Assert.That(media.uuid, Is.Not.EqualTo(Guid.Empty));
            Assert.That(media.title, Is.EqualTo(title));
            Assert.That(media.description, Is.EqualTo(description));
            Assert.That(media.mediaType, Is.EqualTo(mediaType));
            Assert.That(media.releaseYear, Is.EqualTo(releaseYear));
            Assert.That(media.ageRestriction, Is.EqualTo(ageRestriction));
            Assert.That(media.genre, Is.EqualTo(genre));
            Assert.That(media.createdBy, Is.EqualTo(creator));
            Assert.That(media.createdAt, Is.LessThanOrEqualTo(DateTime.Now));
            Assert.That(media.averageScore, Is.EqualTo(0f));
            Assert.That(media.ratings, Is.Not.Null);
            Assert.That(media.ratings.Count, Is.EqualTo(0));
        }
    }

    [TestFixture]
    public class RatingModelTests
    {
        [Test]
        public void Rating_Constructor_InitializesCorrectly()
        {
            // Arrange
            Guid mediaId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();
            int stars = 4;

            // Act
            Rating rating = new Rating(mediaId, userId, stars);

            // Assert
            Assert.That(rating.uuid, Is.Not.EqualTo(Guid.Empty));
            Assert.That(rating.mediaEntry, Is.EqualTo(mediaId));
            Assert.That(rating.user, Is.EqualTo(userId));
            Assert.That(rating.stars, Is.EqualTo(stars));
            Assert.That(rating.createdAt, Is.LessThanOrEqualTo(DateTime.Now));
            Assert.That(rating.likedBy, Is.Not.Null);
            Assert.That(rating.likedBy.Count, Is.EqualTo(0));
            Assert.That(rating.publicVisible, Is.False);
        }

        [Test]
        public void Rating_ConstructorWithComment_InitializesCorrectly()
        {
            // Arrange
            Guid mediaId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();
            int stars = 5;
            string comment = "Excellent movie!";

            // Act
            Rating rating = new Rating(mediaId, userId, stars, comment);

            // Assert
            Assert.That(rating.uuid, Is.Not.EqualTo(Guid.Empty));
            Assert.That(rating.mediaEntry, Is.EqualTo(mediaId));
            Assert.That(rating.user, Is.EqualTo(userId));
            Assert.That(rating.stars, Is.EqualTo(stars));
            Assert.That(rating.comment, Is.EqualTo(comment));
            Assert.That(rating.publicVisible, Is.False);
        }
    }
}
