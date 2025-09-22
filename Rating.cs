using System;
using System.Collections.Generic;
using System.Runtime;

namespace MRT;

public class Rating
{

	private Guid uuid { get; }

	private Guid mediaEntry { get; }
	private Guid user { get; }

	private int stars { get; set; }
	private String comment { get; set; }
	private DateTime createdAt { get; }

    private List<User> likedBy { get; set; }

    private Boolean publicVisible { get; set; }


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
