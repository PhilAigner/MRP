using System;

namespace MRT;

public class Profile
{

    public Guid uuid { get; }      // of user

	public int someStatistic {  get; set; }

	public Profile(Guid _uuid) 
	{
		uuid = _uuid;
	}
}
