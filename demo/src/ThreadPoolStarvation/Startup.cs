using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ThreadPoolStarvation
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Map("/syncoverasyncwait", (application) =>
            {
                application.Run(async (context) =>
                {
                    Task.Delay(TimeSpan.FromSeconds(30)).Wait();
                    ThreadPool.GetAvailableThreads(out var workerThreads, out _);
                    await context.Response.WriteAsync($"Available worker threads: {workerThreads}");
                });
            });

            app.Map("/asyncwait", (application) =>
            {
                application.Run(async (context) =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(30));
                    ThreadPool.GetAvailableThreads(out var workerThreads, out _);
                    await context.Response.WriteAsync($"Available worker threads: {workerThreads}");
                });
            });

            app.Map("/blockthread", (application) =>
            {
                application.Run(async (context) =>
                {
                    ThreadPool.QueueUserWorkItem((_) =>
                    {
                        Thread.Sleep(TimeSpan.FromMinutes(1));
                    });
                    ThreadPool.GetAvailableThreads(out var workerThreads, out _);
                    await context.Response.WriteAsync($"Available worker threads: {workerThreads}");
                });
            });

            app.Run(async (context) =>
            {
                ThreadPool.GetAvailableThreads(out var workerThreads, out _);
                await context.Response.WriteAsync($"Available worker threads: {workerThreads}");
            });
        }
    }
}
