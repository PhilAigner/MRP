using System;
using System.Runtime;

namespace MRT;

public class User
{

	public Guid uuid { get; }
	public string username { get; }
	private string password;
    public DateTime created { get; }

    public Profile profile { get; }

	public User(string _username, string _password)
	{
        uuid = Guid.NewGuid();
		
		username = _username;
        password = _password;

		created = DateTime.Now;

		profile = new Profile(this, uuid);
	}


	public string getPassword()
	{
		return password;
	}
	
}
