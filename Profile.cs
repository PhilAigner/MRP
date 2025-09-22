using System;

namespace MRT;

public class Profile
{

	private User user { get; }
	private Guid uuid;

	private int someStatistic {  get; set; }

	public Profile(User _user, Guid _uuid) 
	{
		user = _user;
		uuid = _uuid;
	}
}
