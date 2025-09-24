using System;
using System.Runtime;

namespace MRP;

public class MediaRepository :  IMediaRepository {

    private List<MediaEntry> mediaEntries = new List<MediaEntry>();


    public List<MediaEntry> GetAll() {
        return mediaEntries;
    }

    public MediaEntry? GetMediaById(Guid id) {
        MediaEntry? mediaEntry = mediaEntries.FirstOrDefault(m => m.uuid == id);
        return mediaEntry;
    }

    public List<MediaEntry>? GetMediaByTitle(String title) {
        List<MediaEntry>? searchResult = mediaEntries.Where(m => m.title == title).ToList();
        return searchResult;
    }

    public List<MediaEntry>? GetMediaByCreator(Guid userid) {
        List<MediaEntry>? searchResult = mediaEntries.Where(m => m.createdBy.uuid == userid).ToList();
        return searchResult;
    }

    public Guid AddMedia(MediaEntry mediaEntry) {
        //test if mediaEntry already exists
        if (mediaEntries.Any(m => m.uuid == mediaEntry.uuid)) return Guid.Empty;

        mediaEntries.Add(mediaEntry);
        return mediaEntry.uuid;
    }
}