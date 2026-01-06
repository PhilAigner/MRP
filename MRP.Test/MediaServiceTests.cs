using Moq;

namespace MRP.Tests
{
    [TestFixture]
    public class MediaServiceTests
    {
        private Mock<IMediaRepository> _mockMediaRepository;
        private Mock<IRatingsRepository> _mockRatingRepository;
        private Mock<IProfileRepository> _mockProfileRepository;
        private MediaService _mediaService;
        private User _testUser;
        private Profile _testProfile;

        [SetUp]
        public void Setup()
        {
            _mockMediaRepository = new Mock<IMediaRepository>();
            _mockRatingRepository = new Mock<IRatingsRepository>();
            _mockProfileRepository = new Mock<IProfileRepository>();
            _mediaService = new MediaService(_mockMediaRepository.Object, _mockRatingRepository.Object, _mockProfileRepository.Object);

            _testUser = new User("testuser", "hashedpassword", Guid.NewGuid());
            _testProfile = new Profile(_testUser.uuid);
        }

        [Test]
        public void CreateMediaEntry_ValidEntry_ReturnsMediaId()
        {
            // Arrange
            Guid expectedMediaId = Guid.NewGuid();
            MediaEntry mediaEntry = new MediaEntry(
                "Test Movie",
                EMediaType.Movie,
                2024,
                EFSK.FSK12,
                "Action",
                _testUser,
                "A test movie"
            );

            _mockMediaRepository.Setup(repo => repo.AddMedia(It.IsAny<MediaEntry>()))
                .Returns(expectedMediaId);

            _mockProfileRepository.Setup(repo => repo.GetByOwnerId(_testUser.uuid))
                .Returns(_testProfile);

            _mockProfileRepository.Setup(repo => repo.UpdateProfile(It.IsAny<Profile>()))
                .Returns(true);

            // Act
            Guid mediaId = _mediaService.createMediaEntry(mediaEntry);

            // Assert
            Assert.That(mediaId, Is.EqualTo(expectedMediaId));
            Assert.That(mediaId, Is.Not.EqualTo(Guid.Empty));
            _mockMediaRepository.Verify(repo => repo.AddMedia(It.IsAny<MediaEntry>()), Times.Once);
            _mockProfileRepository.Verify(repo => repo.UpdateProfile(It.IsAny<Profile>()), Times.Once);
        }

        [Test]
        public void UpdateMediaEntry_ExistingEntry_ReturnsTrue()
        {
            // Arrange
            MediaEntry existingEntry = new MediaEntry(
                Guid.NewGuid(),
                "Existing Movie",
                "Description",
                EMediaType.Movie,
                2023,
                EFSK.FSK12,
                "Drama",
                DateTime.Now,
                _testUser
            );

            _mockMediaRepository.Setup(repo => repo.GetMediaById(existingEntry.uuid))
                .Returns(existingEntry);

            _mockMediaRepository.Setup(repo => repo.UpdateMedia(It.IsAny<MediaEntry>()))
                .Returns(true);

            // Act
            bool result = _mediaService.updateMediaEntry(existingEntry);

            // Assert
            Assert.That(result, Is.True);
            _mockMediaRepository.Verify(repo => repo.GetMediaById(existingEntry.uuid), Times.Once);
            _mockMediaRepository.Verify(repo => repo.UpdateMedia(existingEntry), Times.Once);
        }

        [Test]
        public void DeleteMediaEntry_ExistingEntry_ReturnsTrue()
        {
            // Arrange
            Guid mediaId = Guid.NewGuid();
            MediaEntry existingEntry = new MediaEntry(
                mediaId,
                "Movie to Delete",
                "Description",
                EMediaType.Movie,
                2023,
                EFSK.FSK12,
                "Action",
                DateTime.Now,
                _testUser
            );

            _mockMediaRepository.Setup(repo => repo.GetMediaById(mediaId))
                .Returns(existingEntry);

            _mockMediaRepository.Setup(repo => repo.DeleteMedia(mediaId))
                .Returns(true);

            _mockProfileRepository.Setup(repo => repo.GetByOwnerId(_testUser.uuid))
                .Returns(_testProfile);

            _mockProfileRepository.Setup(repo => repo.UpdateProfile(It.IsAny<Profile>()))
                .Returns(true);

            _mockRatingRepository.Setup(repo => repo.GetAll())
                .Returns(new List<Rating>());

            // Act
            bool result = _mediaService.deleteMediaEntry(mediaId);

            // Assert
            Assert.That(result, Is.True);
            _mockMediaRepository.Verify(repo => repo.DeleteMedia(mediaId), Times.Once);
        }

        [Test]
        public void DeleteMediaEntry_NonExistentEntry_ReturnsFalse()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            _mockMediaRepository.Setup(repo => repo.GetMediaById(nonExistentId))
                .Returns((MediaEntry?)null);

            // Act
            bool result = _mediaService.deleteMediaEntry(nonExistentId);

            // Assert
            Assert.That(result, Is.False);
            _mockMediaRepository.Verify(repo => repo.DeleteMedia(It.IsAny<Guid>()), Times.Never);
        }
    }
}
