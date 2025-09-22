namespace MRP;

public class UserRepository : IUserRepository {

    private List<User> users = new List<User>();


    List<User> IUserRepository.GetAll()
    {
        return users;
    }

    User? IUserRepository.GetUserById(Guid id)
    {
        User? user = users.FirstOrDefault(u => u.uuid == id);
        return user;
    }

    bool IUserRepository.AddUser(User user)
    {
        //TODO check if user already exists

        users.Add(user);

        return true;
    }
}