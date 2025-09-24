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

        public MediaService(MediaRepository _media, RatingRepository _ratings)
        {
            media = _media;
            ratings = _ratings;
        }


        public Guid createMediaEntry(MediaEntry entry)
        {
            return media.AddMedia(entry);
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
            media.GetAll().Remove(existingEntry);
            //also remove all ratings for this media entry
            var ratingsToRemove = ratings.GetAll().Where(r => r.mediaEntry == id).ToList();
            if (ratingsToRemove == null) return true;
            foreach (var rating in ratingsToRemove)
            {
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
            //add new rating
            ratings.AddRating(new Rating(mediaId, userId, stars));
            return true;
        }

        public bool rateMediaEntryWithComment(Guid mediaId, Guid userId, int stars, string comment)
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
            //add new rating
            ratings.AddRating(new Rating(mediaId, userId, stars, comment));
            return true;
        }

        public bool removeRating(Guid mediaId, Guid userId)
        {
            var existingEntry = media.GetMediaById(mediaId);
            if (existingEntry == null) return false;
            //check if user has already rated this media entry
            var existingRating = ratings.GetAll().FirstOrDefault(r => r.mediaEntry == mediaId && r.user == userId);
            if (existingRating == null) return false;
            //remove existing rating
            ratings.GetAll().Remove(existingRating);
            return true;
        }

        public bool editRating(Guid mediaId, Guid userId, int stars, string comment="")
        {
            var existingEntry = media.GetMediaById(mediaId);
            if (existingEntry == null) return false;
            //check if user has already rated this media entry
            var existingRating = ratings.GetAll().FirstOrDefault(r => r.mediaEntry == mediaId && r.user == userId);
            if (existingRating == null) return false;
            //remove existing rating
            ratings.GetAll().Remove(existingRating);
            //add new rating
            ratings.AddRating(new Rating(mediaId, userId, stars, comment));
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

        public bool validateRating(Guid ratingId, Guid userId)
        {
            var existingRating = ratings.GetById(ratingId);
            if (existingRating == null) return false;
            if (existingRating.user != userId) return false;                //user is not the creator of this rating
            existingRating.publicVisible = true;
            return true;
        }
    }
}