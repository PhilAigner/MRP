using Moq;

namespace MRP.Test
{
    [TestFixture]
    public class ProfileStatisticsServiceTests
    {
        private Mock<IProfileRepository> _mockProfileRepository;
        private Mock<IRatingsRepository> _mockRatingRepository;
        private Mock<IMediaRepository> _mockMediaRepository;
        private ProfileStatisticsService _statisticsService;
        private User _testUser;
        private Profile _testProfile;

        [SetUp]
        public void Setup()
        {
            _mockProfileRepository = new Mock<IProfileRepository>();
            _mockRatingRepository = new Mock<IRatingsRepository>();
            _mockMediaRepository = new Mock<IMediaRepository>();
            _statisticsService = new ProfileStatisticsService(
                _mockProfileRepository.Object,
                _mockRatingRepository.Object,
                _mockMediaRepository.Object
            );

            _testUser = new User("testuser", "hashedpassword", Guid.NewGuid());
            _testProfile = new Profile(_testUser.uuid);
        }

        [Test]
        public void UpdateFavorites_UserWithRatings_UpdatesFavorites()
        {
            // Arrange
            Guid userId = _testUser.uuid;

            // Create test media entries
            MediaEntry movie1 = new MediaEntry(Guid.NewGuid(), "Movie 1", "Desc", EMediaType.Movie, 2023, EFSK.FSK12, "Action", DateTime.Now, _testUser);
            MediaEntry movie2 = new MediaEntry(Guid.NewGuid(), "Movie 2", "Desc", EMediaType.Movie, 2024, EFSK.FSK16, "Action", DateTime.Now, _testUser);
            MediaEntry series1 = new MediaEntry(Guid.NewGuid(), "Series 1", "Desc", EMediaType.Series, 2023, EFSK.FSK12, "Drama", DateTime.Now, _testUser);

            // Create test ratings
            List<Rating> userRatings = new List<Rating>
            {
                new Rating(movie1.uuid, userId, 5),
                new Rating(movie2.uuid, userId, 4),
                new Rating(series1.uuid, userId, 5)
            };

            _mockProfileRepository.Setup(repo => repo.GetByOwnerId(userId))
                .Returns(_testProfile);

            _mockRatingRepository.Setup(repo => repo.GetByCreator(userId))
                .Returns(userRatings);

            _mockMediaRepository.Setup(repo => repo.GetMediaById(movie1.uuid))
                .Returns(movie1);
            _mockMediaRepository.Setup(repo => repo.GetMediaById(movie2.uuid))
                .Returns(movie2);
            _mockMediaRepository.Setup(repo => repo.GetMediaById(series1.uuid))
                .Returns(series1);

            _mockProfileRepository.Setup(repo => repo.UpdateProfile(It.IsAny<Profile>()))
                .Returns(true);

            // Act
            _statisticsService.UpdateFavorites(userId);

            // Assert
            Assert.That(_testProfile.favoriteGenre, Is.EqualTo("Action"));
            Assert.That(_testProfile.favoriteMediaType, Is.EqualTo("Movie"));
            _mockProfileRepository.Verify(repo => repo.UpdateProfile(_testProfile), Times.Once);
        }

        [Test]
        public void RecalculateStatistics_ValidUser_UpdatesAllCounts()
        {
            // Arrange
            Guid userId = _testUser.uuid;

            // Create test ratings (some with comments)
            List<Rating> userRatings = new List<Rating>
            {
                new Rating(Guid.NewGuid(), userId, 5, "Great movie!"),
                new Rating(Guid.NewGuid(), userId, 4, "Good"),
                new Rating(Guid.NewGuid(), userId, 3)  // No comment
            };

            // Create test media entries
            MediaEntry media1 = new MediaEntry(Guid.NewGuid(), "Media 1", "Desc", EMediaType.Movie, 2023, EFSK.FSK12, "Action", DateTime.Now, _testUser);
            MediaEntry media2 = new MediaEntry(Guid.NewGuid(), "Media 2", "Desc", EMediaType.Series, 2024, EFSK.FSK16, "Drama", DateTime.Now, _testUser);

            List<MediaEntry> userMedia = new List<MediaEntry> { media1, media2 };

            _mockProfileRepository.Setup(repo => repo.GetByOwnerId(userId))
                .Returns(_testProfile);

            _mockRatingRepository.Setup(repo => repo.GetByCreator(userId))
                .Returns(userRatings);

            _mockMediaRepository.Setup(repo => repo.GetMediaByCreator(userId))
                .Returns(userMedia);

            _mockMediaRepository.Setup(repo => repo.GetMediaById(It.IsAny<Guid>()))
                .Returns(media1);

            _mockProfileRepository.Setup(repo => repo.UpdateProfile(It.IsAny<Profile>()))
                .Returns(true);

            // Act
            _statisticsService.RecalculateStatistics(userId);

            // Assert
            Assert.That(_testProfile.numberOfRatingsGiven, Is.EqualTo(3));
            Assert.That(_testProfile.numberOfReviewsWritten, Is.EqualTo(2));
            Assert.That(_testProfile.numberOfMediaAdded, Is.EqualTo(2));
            _mockProfileRepository.Verify(repo => repo.UpdateProfile(_testProfile), Times.AtLeastOnce);
        }

        [Test]
        public void UpdateFavorites_UserWithNoRatings_DoesNotUpdateProfile()
        {
            // Arrange
            Guid userId = _testUser.uuid;

            _mockProfileRepository.Setup(repo => repo.GetByOwnerId(userId))
                .Returns(_testProfile);

            _mockRatingRepository.Setup(repo => repo.GetByCreator(userId))
                .Returns(new List<Rating>());

            // Act
            _statisticsService.UpdateFavorites(userId);

            // Assert
            _mockProfileRepository.Verify(repo => repo.UpdateProfile(It.IsAny<Profile>()), Times.Never);
        }
    }
}
