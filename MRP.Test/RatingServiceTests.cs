using Moq;

namespace MRP.Tests
{
    [TestFixture]
    public class RatingServiceTests
    {
        private Mock<IRatingsRepository> _mockRatingRepository;
        private Mock<IProfileRepository> _mockProfileRepository;
        private Mock<IMediaRepository> _mockMediaRepository;
        private RatingService _ratingService;
        private User _testUser;
        private MediaEntry _testMedia;
        private Profile _testProfile;

        [SetUp]
        public void Setup()
        {
            _mockRatingRepository = new Mock<IRatingsRepository>();
            _mockProfileRepository = new Mock<IProfileRepository>();
            _mockMediaRepository = new Mock<IMediaRepository>();
            _ratingService = new RatingService(_mockRatingRepository.Object, _mockProfileRepository.Object, _mockMediaRepository.Object);

            _testUser = new User("testuser", "hashedpassword", Guid.NewGuid());
            _testProfile = new Profile(_testUser.uuid);
            _testMedia = new MediaEntry(
                "Test Movie",
                EMediaType.Movie,
                2024,
                EFSK.FSK12,
                "Action",
                _testUser
            );
        }

        [Test]
        public void RateMediaEntry_NewRating_ReturnsTrue()
        {
            // Arrange
            Guid mediaId = _testMedia.uuid;
            Guid userId = _testUser.uuid;
            int stars = 4;

            _mockMediaRepository.Setup(repo => repo.GetMediaById(mediaId))
                .Returns(_testMedia);

            _mockRatingRepository.Setup(repo => repo.GetByMediaAndUser(mediaId, userId))
                .Returns((Rating?)null);

            _mockRatingRepository.Setup(repo => repo.AddRating(It.IsAny<Rating>()))
                .Returns(Guid.NewGuid());

            _mockProfileRepository.Setup(repo => repo.GetByOwnerId(userId))
                .Returns(_testProfile);

            _mockProfileRepository.Setup(repo => repo.UpdateProfile(It.IsAny<Profile>()))
                .Returns(true);

            _mockRatingRepository.Setup(repo => repo.GetByCreator(userId))
                .Returns(new List<Rating>());

            // Act
            bool result = _ratingService.rateMediaEntry(mediaId, userId, stars);

            // Assert
            Assert.That(result, Is.True);
            _mockRatingRepository.Verify(repo => repo.AddRating(It.IsAny<Rating>()), Times.Once);
            _mockProfileRepository.Verify(repo => repo.UpdateProfile(It.IsAny<Profile>()), Times.AtLeastOnce);
        }

        [Test]
        public void RateMediaEntry_UpdateExisting_UpdatesRating()
        {
            // Arrange
            Guid mediaId = _testMedia.uuid;
            Guid userId = _testUser.uuid;
            int newStars = 5;
            Rating existingRating = new Rating(mediaId, userId, 3);

            _mockMediaRepository.Setup(repo => repo.GetMediaById(mediaId))
                .Returns(_testMedia);

            _mockRatingRepository.Setup(repo => repo.GetByMediaAndUser(mediaId, userId))
                .Returns(existingRating);

            _mockRatingRepository.Setup(repo => repo.UpdateRating(It.IsAny<Rating>()))
                .Returns(true);

            _mockProfileRepository.Setup(repo => repo.GetByOwnerId(userId))
                .Returns(_testProfile);

            _mockRatingRepository.Setup(repo => repo.GetByCreator(userId))
                .Returns(new List<Rating> { existingRating });

            // Act
            bool result = _ratingService.rateMediaEntry(mediaId, userId, newStars);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(existingRating.stars, Is.EqualTo(newStars));
            _mockRatingRepository.Verify(repo => repo.UpdateRating(existingRating), Times.Once);
            _mockRatingRepository.Verify(repo => repo.AddRating(It.IsAny<Rating>()), Times.Never);
        }

        [Test]
        public void RemoveRating_ExistingRating_ReturnsTrue()
        {
            // Arrange
            Guid mediaId = _testMedia.uuid;
            Guid userId = _testUser.uuid;
            Rating existingRating = new Rating(mediaId, userId, 4, "Great movie!");

            _mockMediaRepository.Setup(repo => repo.GetMediaById(mediaId))
                .Returns(_testMedia);

            _mockRatingRepository.Setup(repo => repo.GetByMediaAndUser(mediaId, userId))
                .Returns(existingRating);

            _mockRatingRepository.Setup(repo => repo.DeleteRating(existingRating.uuid))
                .Returns(true);

            _mockProfileRepository.Setup(repo => repo.GetByOwnerId(userId))
                .Returns(_testProfile);

            _mockProfileRepository.Setup(repo => repo.UpdateProfile(It.IsAny<Profile>()))
                .Returns(true);

            _mockRatingRepository.Setup(repo => repo.GetByCreator(userId))
                .Returns(new List<Rating>());

            // Act
            bool result = _ratingService.removeRating(mediaId, userId);

            // Assert
            Assert.That(result, Is.True);
            _mockRatingRepository.Verify(repo => repo.DeleteRating(existingRating.uuid), Times.Once);
            _mockProfileRepository.Verify(repo => repo.UpdateProfile(It.IsAny<Profile>()), Times.AtLeastOnce);
        }

        [Test]
        public void ApproveRating_OwnerApproves_SetsPublicVisible()
        {
            // Arrange
            Guid ratingId = Guid.NewGuid();
            Guid ownerId = _testUser.uuid;
            Rating rating = new Rating(_testMedia.uuid, Guid.NewGuid(), 5, "Amazing!");

            _mockRatingRepository.Setup(repo => repo.GetById(ratingId))
                .Returns(rating);

            _mockMediaRepository.Setup(repo => repo.GetMediaById(_testMedia.uuid))
                .Returns(_testMedia);

            _mockRatingRepository.Setup(repo => repo.UpdateRating(It.IsAny<Rating>()))
                .Returns(true);

            // Act
            bool result = _ratingService.approveRating(ratingId, ownerId);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(rating.publicVisible, Is.True);
            _mockRatingRepository.Verify(repo => repo.UpdateRating(rating), Times.Once);
        }

        [Test]
        public void ApproveRating_NonOwnerApproves_ReturnsFalse()
        {
            // Arrange
            Guid ratingId = Guid.NewGuid();
            Guid nonOwnerId = Guid.NewGuid();
            Rating rating = new Rating(_testMedia.uuid, Guid.NewGuid(), 5);

            _mockRatingRepository.Setup(repo => repo.GetById(ratingId))
                .Returns(rating);

            _mockMediaRepository.Setup(repo => repo.GetMediaById(_testMedia.uuid))
                .Returns(_testMedia);

            // Act
            bool result = _ratingService.approveRating(ratingId, nonOwnerId);

            // Assert
            Assert.That(result, Is.False);
            _mockRatingRepository.Verify(repo => repo.UpdateRating(It.IsAny<Rating>()), Times.Never);
        }

        [Test]
        public void LikeRating_ValidRating_ReturnsTrue()
        {
            // Arrange
            Guid ratingId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();
            Rating rating = new Rating(_testMedia.uuid, _testUser.uuid, 5);

            _mockRatingRepository.Setup(repo => repo.GetById(ratingId))
                .Returns(rating);

            _mockRatingRepository.Setup(repo => repo.AddLike(ratingId, userId))
                .Returns(true);

            // Act
            bool result = _ratingService.likeRating(ratingId, userId);

            // Assert
            Assert.That(result, Is.True);
            _mockRatingRepository.Verify(repo => repo.AddLike(ratingId, userId), Times.Once);
        }
    }
}
