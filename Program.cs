using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace McNativeMirrorServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
            .UseIISIntegration()
            .UseUrls("https://0.0.0.0:443")
            .UseSentry("https://243559f4ee7f47f2b6c8514e97b6f551@o428820.ingest.sentry.io/5449626")
            .UseStartup<Startup>();
    }
}
