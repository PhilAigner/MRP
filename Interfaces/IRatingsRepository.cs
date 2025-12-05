using System;
using System.Runtime;

namespace MRP;

public interface IRatingsRepository
{
    public List<Rating> GetAll();

    public Rating? GetById(Guid id);

    public List<Rating>? GetByCreator(Guid userid);
    
    public Rating? GetByMediaAndUser(Guid mediaId, Guid userId);
    
    public List<Rating>? GetByMedia(Guid mediaId);

    public Guid AddRating(Rating rating);

    public List<Rating>? GetByStarsGreaterEqlThan(int stars);

    public List<Rating>? GetByStarsLowerEqlThan(int stars);

    public bool UpdateRating(Rating rating);

    public bool DeleteRating(Guid id);

    public bool AddLike(Guid ratingId, Guid userId);

    public bool RemoveLike(Guid ratingId, Guid userId);
}