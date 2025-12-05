using System;
using System.Runtime;

namespace MRP;

public interface IMediaRepository
{
    public List<MediaEntry> GetAll();

    public MediaEntry? GetMediaById(Guid id);

    public List<MediaEntry>? GetMediaByTitle(string title);

    public List<MediaEntry>? GetMediaByCreator(Guid userid);

    public Guid AddMedia(MediaEntry mediaEntry);

    public bool UpdateMedia(MediaEntry mediaEntry);

    public bool DeleteMedia(Guid id);
}