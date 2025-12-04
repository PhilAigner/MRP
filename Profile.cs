using System;

namespace MRP;

public class Profile
{
	public Guid uuid { get; }

    public Guid user { get; }       // the uuid of the user this profile belongs to


    public int numberOfLogins { get; set; }
    public int numberOfRatingsGiven { get; set; }
    public int numberOfMediaAdded { get; set; }
    public int numberOfReviewsWritten { get; set; }
    public string favoriteGenre { get; set; }
    public string favoriteMediaType { get; set; }
    public string sobriquet { get; set; }
    public string aboutMe { get; set; }

    public Profile(Guid owner) 
	{
		uuid = Guid.NewGuid();
        
        user = owner;

        numberOfLogins = 0;
        numberOfRatingsGiven = 0;
        numberOfMediaAdded = 0;
        numberOfReviewsWritten = 0;
        favoriteGenre = string.Empty;
        favoriteMediaType = string.Empty;
        sobriquet = string.Empty;
        aboutMe = string.Empty;
    }

    public Profile(Guid _uuid, Guid _user, int _numberOfLogins, int _numberOfRatingsGiven, 
                   int _numberOfMediaAdded, int _numberOfReviewsWritten, string _favoriteGenre, 
                   string _favoriteMediaType, string _sobriquet, string _aboutMe)
    {
        uuid = _uuid;
        user = _user;
        numberOfLogins = _numberOfLogins;
        numberOfRatingsGiven = _numberOfRatingsGiven;
        numberOfMediaAdded = _numberOfMediaAdded;
        numberOfReviewsWritten = _numberOfReviewsWritten;
        favoriteGenre = _favoriteGenre;
        favoriteMediaType = _favoriteMediaType;
        sobriquet = _sobriquet;
        aboutMe = _aboutMe;
    }
}