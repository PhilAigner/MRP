using System;
using System.Collections.Generic;
using System.Linq;

namespace MRP
{
    internal class ProfileStatisticsService
    {
        private readonly ProfileRepository _profileRepository;
        private readonly RatingRepository _ratingRepository;
        private readonly MediaRepository _mediaRepository;

        public ProfileStatisticsService(ProfileRepository profileRepository, RatingRepository ratingRepository, MediaRepository mediaRepository)
        {
            _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            _ratingRepository = ratingRepository ?? throw new ArgumentNullException(nameof(ratingRepository));
            _mediaRepository = mediaRepository ?? throw new ArgumentNullException(nameof(mediaRepository));
        }

        /// <summary>
        /// Updates the favorite genre and media type based on user's ratings
        /// </summary>
        public void UpdateFavorites(Guid userId)
        {
            var profile = _profileRepository.GetByOwnerId(userId);
            if (profile == null) return;

            var userRatings = _ratingRepository.GetByCreator(userId);
            if (userRatings == null || !userRatings.Any()) return;

            // Get all media entries that the user has rated
            var ratedMedia = userRatings
                .Select(r => _mediaRepository.GetMediaById(r.mediaEntry))
                .Where(m => m != null)
                .ToList();

            if (!ratedMedia.Any()) return;

            // Calculate favorite genre (most frequently rated genre)
            var genreCounts = ratedMedia
                .Where(m => !string.IsNullOrWhiteSpace(m!.genre))
                .GroupBy(m => m!.genre)
                .Select(g => new { Genre = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .FirstOrDefault();

            if (genreCounts != null)
            {
                profile.favoriteGenre = genreCounts.Genre;
            }

            // Calculate favorite media type (most frequently rated type)
            var mediaTypeCounts = ratedMedia
                .GroupBy(m => m!.mediaType)
                .Select(g => new { MediaType = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .FirstOrDefault();

            if (mediaTypeCounts != null)
            {
                profile.favoriteMediaType = mediaTypeCounts.MediaType.ToString();
            }
        }

        /// <summary>
        /// Recalculates all statistics for a user profile
        /// </summary>
        public void RecalculateStatistics(Guid userId)
        {
            var profile = _profileRepository.GetByOwnerId(userId);
            if (profile == null) return;

            // Count ratings given
            var userRatings = _ratingRepository.GetByCreator(userId);
            profile.numberOfRatingsGiven = userRatings?.Count ?? 0;

            // Count reviews written (ratings with comments)
            profile.numberOfReviewsWritten = userRatings?
                .Count(r => !string.IsNullOrWhiteSpace(r.comment)) ?? 0;

            // Count media added
            var userMedia = _mediaRepository.GetMediaByCreator(userId);
            profile.numberOfMediaAdded = userMedia?.Count ?? 0;

            // Update favorites
            UpdateFavorites(userId);
        }
    }
}
