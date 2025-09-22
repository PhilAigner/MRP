using System;
using System.Runtime;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MRT;

public class User
{

	public Guid uuid { get; }
	public string username { get; }
    public string password { get; }
    public DateTime created { get; }

    [JsonIgnore]
    public Profile profile { get; }

    public User(string _username, string _password)
    {
        uuid = Guid.NewGuid();

        username = _username;
        password = _password;

        created = DateTime.Now;

        profile = new Profile(uuid);
    }

    [JsonConstructor]
    public User(Guid _uuid, string _username, string _password, DateTime _created)
    {
        uuid = _uuid;

        username = _username;
        password = _password;

        created = _created;

        profile = new Profile(_uuid); //neu erstellen beim laden
    }


    public string getPassword()
	{
		return password;
	}
	
}
