using System;
using System.Net.Http.Headers;

namespace MRP;

public class MediaEntry
{
	public Guid uuid { get; }
	public string title { get; set; }
    public string description { get; set; }
    public EMediaType mediaType { get; set; }
    public int releaseYear { get; set; }
    public EFSK ageRestriction { get; set; }
    public string genre { get; set; }
    public DateTime createdAt { get; }

    public float averageScore { get; }

    public User createdBy { get; }

    public List<Rating> ratings { get; }



	public MediaEntry(string _title, EMediaType _mediaType, int _releaseYear, EFSK _ageRestriction, string _genre, User creator, string _description = "") 
	{
        uuid = Guid.NewGuid();
        title = _title;
        description = _description;
        mediaType = _mediaType;
        releaseYear = _releaseYear;
        ageRestriction = _ageRestriction;
        genre = _genre;
        createdAt = DateTime.Now;
        createdBy = creator;
        
        ratings =  new List<Rating>();
        averageScore = (float) 0;
    }

    public MediaEntry(Guid _uuid, string _title, string _description, EMediaType _mediaType, 
                      int _releaseYear, EFSK _ageRestriction, string _genre, DateTime _createdAt, 
                      User _createdBy)
    {
        uuid = _uuid;
        title = _title;
        description = _description;
        mediaType = _mediaType;
        releaseYear = _releaseYear;
        ageRestriction = _ageRestriction;
        genre = _genre;
        createdAt = _createdAt;
        createdBy = _createdBy;
        ratings = new List<Rating>();
        averageScore = (float) 0;
    }
}