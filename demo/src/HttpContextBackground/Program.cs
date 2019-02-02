using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HttpContextBackground
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            using (var factory = new CustomWebApplicationFactory<Startup>())
            {
                var client = factory.CreateDefaultClient();
                for (var i = 0; i < 100; i++)
                {
                    _ = await client.GetAsync("/");
                    //await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
        }
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }

    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup: class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSolutionRelativeContentRoot(@"src\HttpContextBackground");
            builder.ConfigureLogging(logBuilder => logBuilder.ClearProviders());
        }
    }
}
