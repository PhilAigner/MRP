using System;
using System.Runtime;

namespace MRP;

public class RatingRepository :  IRatingsRepository {

    private List<Rating> ratingEntries = new List<Rating>();


    public List<Rating> GetAll() {
        return ratingEntries;
    }

    public Rating? GetById(Guid id) {
        Rating? res = ratingEntries.FirstOrDefault(r => r.uuid == id);
        return res;
    }

    public List<Rating>? GetByCreator(Guid userid) {
        List<Rating>? searchResult = ratingEntries.Where(r => r.user == userid).ToList();
        return searchResult;
    }

    public Guid AddRating(Rating rating) {
        //test if mediaEntry already exists
        if (ratingEntries.Any(r => r.uuid == rating.uuid)) return Guid.Empty;

        ratingEntries.Add(rating);
        return rating.uuid;
    }

    public List<Rating>? GetByStarsGreaterEqlThan(int stars) {
        List<Rating>? searchResult = ratingEntries.Where(r => r.stars >= stars).ToList();
        return searchResult;
    }

    public List<Rating>? GetByStarsLowerEqlThan(int stars) {
        List<Rating>? searchResult = ratingEntries.Where(r => r.stars <= stars).ToList();
        return searchResult;
    }
}