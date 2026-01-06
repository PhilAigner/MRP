namespace MRP;

public interface IUserRepository
{
    public List<User> GetAll();

    public User? GetUserById(Guid id);

    public Guid AddUser(User user);

    public User? GetUserByUsername(string username);
}