using System;
using System.Runtime;

namespace MRP;

public interface IProfileRepository
{
    public List<Profile> GetAll();

    public Guid AddProfile(Profile profile);

    public Profile? GetById(Guid id);

    public Profile? GetByOwnerId(Guid userid);

    public bool UpdateProfile(Profile profile);
}