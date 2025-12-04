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

        public MediaService(MediaRepository _media, RatingRepository _ratings, ProfileRepository _profiles)
        {
            media = _media;
            ratings = _ratings;
            profiles = _profiles;
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
                    profiles.UpdateProfile(profile);
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
    }
}