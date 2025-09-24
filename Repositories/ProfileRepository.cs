using System;
using System.Runtime;

namespace MRP;

public class ProfileRepository :  IProfileRepository {

    private List<Profile> profiles = new List<Profile>();

    public List<Profile> GetAll() { 
        return profiles;
    }

    public Profile? GetById(Guid id)
    {
        Profile? res = profiles.FirstOrDefault(p => p.uuid == id);
        return res;
    }


    public Profile? GetByOwnerId(Guid userid)
    {
        Profile? res = profiles.FirstOrDefault(p => p.user == userid);
        return res;
    }
}