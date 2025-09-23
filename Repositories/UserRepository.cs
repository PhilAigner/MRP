namespace MRP;

public class UserRepository : IUserRepository {

    private List<User> users = new List<User>();


    public List<User> GetAll()
    {
        return users;
    }

    public User? GetUserById(Guid id)
    {
        User? user = users.FirstOrDefault(u => u.uuid == id);
        return user;
    }

    public bool AddUser(User user)
    {
        //test if user already exists
        if (users.Any(u => u.uuid == user.uuid)) return false;

        users.Add(user);

        return true;
    }

    internal User? GetUserByUsername(string username)
    {
        User? user = users.FirstOrDefault(u => u.username == username);
        return user;
    }
}