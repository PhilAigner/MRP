namespace MRP;

public class Profile
{

	private Guid uuid;

	private int someStatistic {  get; set; }

	public Profile(Guid _uuid) 
	{
		uuid = _uuid;
	}
}
