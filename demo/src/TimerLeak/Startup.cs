using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TimerLeak
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<HttpClient>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Map("/badtimer", (application) =>
            {
                application.Run(async (context) =>
                {
                    var client = context.RequestServices.GetRequiredService<HttpClient>();
                    var httpTask = client.GetAsync("https://pokeapi.co/api/v2/pokemon/ditto");
                    var delayTask = Task.Delay(TimeSpan.FromSeconds(10));
                    var resultTask = await Task.WhenAny(httpTask, delayTask);
                    
                    if (resultTask == delayTask)
                    {
                        throw new OperationCanceledException();
                    }
                    await context.Response.WriteAsync(httpTask.Result.StatusCode.ToString());

                });
            });

            app.Map("/goodtimer", (application) =>
            {
                application.Run(async (context) =>
                {
                    using (var cts = new CancellationTokenSource())
                    {
                        var client = context.RequestServices.GetRequiredService<HttpClient>();
                        var httpTask = client.GetAsync("https://pokeapi.co/api/v2/pokemon/ditto");
                        var delayTask = Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
                        var resultTask = await Task.WhenAny(httpTask, delayTask);
                        if (resultTask == delayTask)
                        {
                            // Operation cancelled
                            throw new OperationCanceledException();
                        }
                        else
                        {
                            // Cancel the timer task so that it does not fire
                            cts.Cancel();
                            await context.Response.WriteAsync(httpTask.Result.StatusCode.ToString());
                        }
                    }

                });
            });

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
