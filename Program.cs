
namespace MRP
{
    internal class Program
    {
        static async Task Main()
        {
            var cs = "Host=192.177.1.69;Port=5333;Username=mrp;Password=Philipp!!;Database=mrp;Pooling=true";
            var dbFactory = new DbConnectionFactory(cs);


            try
            {
                using var repo = new UserRepository(dbFactory);

                await repo.InitAsync();


                var userId = await repo.InsertUserAsync("testuser", "testpassword");
                Console.WriteLine(userId);


                var all = await repo.GetAllAsync();
                foreach (var u in all)
                    Console.WriteLine($"{u.uuid}: {u.username}");

            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Fehler bei der Verbindung:");
                Console.WriteLine(ex.Message);
            }

            /*
            Handler handler = new Handler();

            handler.Start();
            */
        }
    }
}