using System;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace DoubleContainerBuild
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureServices(s => s.AddSingleton<SingleService>())
                .UseStartup<Startup>();
    }

    public class SingleService
    {
        public Guid Id { get; set; }
        public SingleService()
        {
            Id = Guid.NewGuid();
        }
    }

    public class Startup
    {
        public Startup(SingleService service)
        {
            Console.WriteLine(service.Id);
        }

        public void Configure(IApplicationBuilder app, SingleService service)
        {
            Console.WriteLine(service.Id);
            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}