namespace MRP;

public class User
{
    public Guid uuid { get; }
    public string username { get; }
    private string password;
    public DateTime created { get; }
    public Profile profile { get; }

    public User(string username, string password)
    {
        this.uuid = Guid.NewGuid();
        this.username = username;
        this.password = password;
        this.created = DateTime.Now;
        this.profile = new Profile(uuid);
    }

    public User(Guid uuid, string username, string password, DateTime created)
    {
        this.uuid = uuid;
        this.username = username;
        this.password = password;
        this.created = created;
    }

    public string getPassword()
    {
        return password;
    }
}
