using System;
using System.Runtime;

namespace MRP;

public interface IRatingsRepository
{
    public List<Rating> GetAll();

    public Rating? GetById(Guid id);

    public List<Rating>? GetByCreator(Guid userid);

    public Guid AddRating(Rating rating);

    public List<Rating>? GetByStarsGreaterEqlThan(int stars);

    public List<Rating>? GetByStarsLowerEqlThan(int stars);
}