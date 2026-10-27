using Microsoft.EntityFrameworkCore;
namespace CasaDoCodigo;
public class Program
{
    public static async Task Main(string[] args)
    {
        var commands = args.Where(x => x is "--migrate" or "--seed").ToArray();
        using var host = CreateHostBuilder(args.Except(commands).ToArray()).Build();
        if (commands.Length > 0)
        {
            using var scope = host.Services.CreateScope();
            if (commands.Contains("--migrate")) await scope.ServiceProvider.GetRequiredService<ApplicationContext>().Database.MigrateAsync();
            if (commands.Contains("--seed")) await scope.ServiceProvider.GetRequiredService<IDataService>().SeedAsync();
            return;
        }
        await host.RunAsync();
    }
    public static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(web => web.UseStartup<Startup>());
}
