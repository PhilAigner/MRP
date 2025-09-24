using System;

namespace MRP;

public class Profile
{
	public Guid uuid { get; }

    public Guid user { get; }       // the uuid of the user this profile belongs to


    public int someStatistic {  get; set; }

	public Profile(Guid owner) 
	{
		uuid = Guid.NewGuid();
        
        user = owner;

        someStatistic = 1;
    }
}