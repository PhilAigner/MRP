using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRP
{
    internal class RatingService
    {
        private RatingRepository ratings;
        private ProfileRepository profiles;
        private MediaRepository media;
        private ProfileStatisticsService statisticsService;

        public RatingService(RatingRepository _ratings, ProfileRepository _profiles, MediaRepository _media)
        {
            ratings = _ratings;
            profiles = _profiles;
            media = _media;
            statisticsService = new ProfileStatisticsService(_profiles, _ratings, _media);
        }

        public bool rateMediaEntry(Guid mediaId, Guid userId, int stars)
        {
            var existingEntry = media.GetMediaById(mediaId);
            if (existingEntry == null) return false;
            
            //check if user has already rated this media entry
            var existingRating = ratings.GetAll().FirstOrDefault(r => r.mediaEntry == mediaId && r.user == userId);
            if (existingRating != null)
            {
                //update existing rating
                ratings.GetAll().Remove(existingRating);
            }
            else
            {
                // Only increment if it's a new rating
                var profile = profiles.GetByOwnerId(userId);
                if (profile != null)
                {
                    profile.numberOfRatingsGiven++;
                }
            }
            
            //add new rating
            ratings.AddRating(new Rating(mediaId, userId, stars));
            
            // Update favorite genre and media type
            statisticsService.UpdateFavorites(userId);
            
            return true;
        }

        public bool rateMediaEntryWithComment(Guid mediaId, Guid userId, int stars, string comment)
        {
            var existingEntry = media.GetMediaById(mediaId);
            if (existingEntry == null) return false;
            
            //check if user has already rated this media entry
            var existingRating = ratings.GetAll().FirstOrDefault(r => r.mediaEntry == mediaId && r.user == userId);
            bool hadComment = false;
            
            if (existingRating != null)
            {
                hadComment = !string.IsNullOrWhiteSpace(existingRating.comment);
                //update existing rating
                ratings.GetAll().Remove(existingRating);
            }
            else
            {
                // New rating
                var profile = profiles.GetByOwnerId(userId);
                if (profile != null)
                {
                    profile.numberOfRatingsGiven++;
                }
            }
            
            // Update review count if comment was added or changed
            if (!string.IsNullOrWhiteSpace(comment) && !hadComment)
            {
                var profile = profiles.GetByOwnerId(userId);
                if (profile != null)
                {
                    profile.numberOfReviewsWritten++;
                }
            }
            
            //add new rating
            ratings.AddRating(new Rating(mediaId, userId, stars, comment));
            
            // Update favorite genre and media type
            statisticsService.UpdateFavorites(userId);
            
            return true;
        }

        public bool removeRating(Guid mediaId, Guid userId)
        {
            var existingEntry = media.GetMediaById(mediaId);
            if (existingEntry == null) return false;
            
            //check if user has already rated this media entry
            var existingRating = ratings.GetAll().FirstOrDefault(r => r.mediaEntry == mediaId && r.user == userId);
            if (existingRating == null) return false;
            
            // Update profile statistics
            var profile = profiles.GetByOwnerId(userId);
            if (profile != null)
            {
                if (profile.numberOfRatingsGiven > 0)
                {
                    profile.numberOfRatingsGiven--;
                }
                
                if (!string.IsNullOrWhiteSpace(existingRating.comment) && profile.numberOfReviewsWritten > 0)
                {
                    profile.numberOfReviewsWritten--;
                }
            }
            
            //remove existing rating
            ratings.GetAll().Remove(existingRating);
            
            // Update favorite genre and media type
            statisticsService.UpdateFavorites(userId);
            
            return true;
        }

        public bool editRating(Guid mediaId, Guid userId, int stars, string comment="")
        {
            var existingEntry = media.GetMediaById(mediaId);
            if (existingEntry == null) return false;
            
            //check if user has already rated this media entry
            var existingRating = ratings.GetAll().FirstOrDefault(r => r.mediaEntry == mediaId && r.user == userId);
            if (existingRating == null) return false;
            
            bool hadComment = !string.IsNullOrWhiteSpace(existingRating.comment);
            bool hasComment = !string.IsNullOrWhiteSpace(comment);
            
            //remove existing rating
            ratings.GetAll().Remove(existingRating);
            
            // Update review count if comment status changed
            var profile = profiles.GetByOwnerId(userId);
            if (profile != null)
            {
                if (!hadComment && hasComment)
                {
                    // Comment was added
                    profile.numberOfReviewsWritten++;
                }
                else if (hadComment && !hasComment)
                {
                    // Comment was removed
                    if (profile.numberOfReviewsWritten > 0)
                    {
                        profile.numberOfReviewsWritten--;
                    }
                }
            }
            
            //add new rating
            ratings.AddRating(new Rating(mediaId, userId, stars, comment));
            
            // Update favorite genre and media type
            statisticsService.UpdateFavorites(userId);
            
            return true;
        }

        public bool likeRating(Guid ratingId, Guid userId)
        {
            var existingRating = ratings.GetById(ratingId);
            if (existingRating == null) return false;
            if (existingRating.likedBy.Contains(userId)) return false;      //user has already liked this rating
            existingRating.likedBy.Add(userId);
            return true;
        }

        public bool removeLikeFromRating(Guid ratingId, Guid userId)
        {
            var existingRating = ratings.GetById(ratingId);
            if (existingRating == null) return false;
            if (!existingRating.likedBy.Contains(userId)) return false;     //user has not liked this rating
            existingRating.likedBy.Remove(userId);
            return true;
        }

        public bool approveRating(Guid ratingId, Guid approverId)
        {
            var existingRating = ratings.GetById(ratingId);
            if (existingRating == null) return false;
            
            var mediaEntry = media.GetMediaById(existingRating.mediaEntry);
            if (mediaEntry == null) return false;
            
            // Check if the approver is the owner of the media entry
            if (mediaEntry.createdBy.uuid != approverId) return false;
            
            // Make the rating publicly visible
            existingRating.publicVisible = true;

            // Add rating to media entry's ratings list if not already present
            // ( so that it can be shown directly in the media entry details )
            if (!mediaEntry.ratings.Any(r => r.uuid == ratingId))
            {
                mediaEntry.ratings.Add(existingRating);
            }
            
            return true;
        }
    }
}
