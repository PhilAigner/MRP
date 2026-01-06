namespace MRP;

public class User
{

	public Guid uuid { get; }
	public string username { get; }
	private string password;
    public DateTime created { get; }

    public Guid profileUuid { get; }

	public User(string _username, string _password, Guid _profileUuid)
	{
        uuid = Guid.NewGuid();
		
		username = _username;
        password = _password;

		created = DateTime.Now;

		profileUuid = _profileUuid;
    }

	public User(Guid _uuid, string _username, string _password, DateTime _created, Guid _profileUuid)
	{
		uuid = _uuid;
		username = _username;
		password = _password;
		created = _created;
		profileUuid = _profileUuid;
	}

	public string getPassword()
	{
		return password;
	}	
}