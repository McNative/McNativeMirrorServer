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
            .UseUrls("http://0.0.0.0:80")
            .UseStartup<Startup>();
    }
}
