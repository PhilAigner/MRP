namespace MRP
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Handler handler = new Handler();

            await handler.StartAsync();
        }
    }
}