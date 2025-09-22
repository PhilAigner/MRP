using System;
using System.Runtime;

namespace MRP;

public interface IUserRepository
{
    List<User> GetAll();

    User? GetUserById(Guid id);

    bool AddUser(User user);
}