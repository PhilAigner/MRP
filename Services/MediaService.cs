using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRP
{
    internal class MediaService
    {

        private MediaRepository media;
        private RatingRepository ratings;
        private ProfileRepository profiles;
        private ProfileStatisticsService? statisticsService;

        public MediaService(MediaRepository _media, RatingRepository _ratings, ProfileRepository _profiles)
        {
            media = _media;
            ratings = _ratings;
            profiles = _profiles;
            statisticsService = new ProfileStatisticsService(_profiles, _ratings, _media);
        }


        public Guid createMediaEntry(MediaEntry entry)
        {
            var result = media.AddMedia(entry);
            
            // Update profile statistics if media was successfully added
            if (result != Guid.Empty)
            {
                var profile = profiles.GetByOwnerId(entry.createdBy.uuid);
                if (profile != null)
                {
                    profile.numberOfMediaAdded++;
                }
            }
            
            return result;
        }

        public bool updateMediaEntry(MediaEntry entry)
        {
            var existingEntry = media.GetMediaById(entry.uuid);
            if (existingEntry == null) return false;
            media.GetAll().Remove(existingEntry);
            media.GetAll().Add(entry);
            return true;
        }

        public bool deleteMediaEntry(Guid id)
        {
            var existingEntry = media.GetMediaById(id);
            if (existingEntry == null) return false;
            
            // Update profile statistics before deletion
            var profile = profiles.GetByOwnerId(existingEntry.createdBy.uuid);
            if (profile != null && profile.numberOfMediaAdded > 0)
            {
                profile.numberOfMediaAdded--;
            }
            
            media.GetAll().Remove(existingEntry);
            //also remove all ratings for this media entry
            var ratingsToRemove = ratings.GetAll().Where(r => r.mediaEntry == id).ToList();
            if (ratingsToRemove == null) return true;
            foreach (var rating in ratingsToRemove)
            {
                // Update profile statistics for each removed rating
                var ratingUserProfile = profiles.GetByOwnerId(rating.user);
                if (ratingUserProfile != null && ratingUserProfile.numberOfRatingsGiven > 0)
                {
                    ratingUserProfile.numberOfRatingsGiven--;
                }
                if (!string.IsNullOrWhiteSpace(rating.comment) && ratingUserProfile != null && ratingUserProfile.numberOfReviewsWritten > 0)
                {
                    ratingUserProfile.numberOfReviewsWritten--;
                }
                ratings.GetAll().Remove(rating);
            }
            return true;
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
            statisticsService?.UpdateFavorites(userId);
            
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
            statisticsService?.UpdateFavorites(userId);
            
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
            statisticsService?.UpdateFavorites(userId);
            
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
            statisticsService?.UpdateFavorites(userId);
            
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