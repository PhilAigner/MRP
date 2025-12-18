
namespace MRP
{
    internal class MediaService
    {

        private IMediaRepository media;
        private IRatingsRepository ratings;
        private IProfileRepository profiles;

        public MediaService(IMediaRepository _media, IRatingsRepository _ratings, IProfileRepository _profiles)
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
            
            // Update in database
            return media.UpdateMedia(entry);
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
                profiles.UpdateProfile(profile);
            }
            
            // Delete from database
            bool deleted = media.DeleteMedia(id);
            if (!deleted) return false;
            
            // Also remove all ratings for this media entry
            var ratingsToRemove = ratings.GetAll().Where(r => r.mediaEntry == id).ToList();
            foreach (var rating in ratingsToRemove)
            {
                // Update profile statistics for each removed rating
                var ratingUserProfile = profiles.GetByOwnerId(rating.user);
                if (ratingUserProfile != null)
                {
                    if (ratingUserProfile.numberOfRatingsGiven > 0)
                    {
                        ratingUserProfile.numberOfRatingsGiven--;
                    }
                    if (!string.IsNullOrWhiteSpace(rating.comment) && ratingUserProfile.numberOfReviewsWritten > 0)
                    {
                        ratingUserProfile.numberOfReviewsWritten--;
                    }
                    profiles.UpdateProfile(ratingUserProfile);
                }
            }
            
            return true;
        }
    }
}