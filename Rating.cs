using System;
using System.Collections.Generic;
using System.Runtime;

namespace MRP;

public class Rating
{

    public Guid uuid { get; }

    public Guid mediaEntry { get; }
    public Guid user { get; }

    public int stars { get; set; }
    public String comment { get; set; }
    public DateTime createdAt { get; }

    public List<User> likedBy { get; set; }

    public Boolean publicVisible { get; set; }


    public Rating(Guid _mediaEntry, Guid _user, int _stars)
	{
        uuid = Guid.NewGuid();

		mediaEntry = _mediaEntry;
		user = _user;

		stars = _stars;

        createdAt = DateTime.Now;

        publicVisible = false;
    }

    public Rating(Guid _mediaEntry, Guid _user, int _stars, string _comment)
    {
        uuid = Guid.NewGuid();

        mediaEntry = _mediaEntry;
        user = _user;

        stars = _stars;
        comment = _comment;

        createdAt = DateTime.Now;

        publicVisible = false;
    }

}
