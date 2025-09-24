using System;
using System.Runtime;

namespace MRP;

public class User
{

	public Guid uuid { get; }
	public string username { get; }
	private string password;
    public DateTime created { get; }

    public Guid profileUuid { get; }

	public User(string _username, string _password, ProfileRepository profileRepository)
	{
        uuid = Guid.NewGuid();
		
		username = _username;
        password = _password;

		created = DateTime.Now;

		profileUuid = profileRepository.AddProfile(new Profile(uuid));
    }


	public string getPassword()
	{
		return password;
	}
	
}