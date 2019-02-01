using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DisposeHttpClient
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            //await HttpClientMain();
            await HttpClientFactoryMainAsync();
        }

        public static async Task HttpClientMain()
        {
            Console.WriteLine("Creating and disposing HttpClient");
            for (var i = 0; i < 10; i++)
            {
                // This is bad. 
                using (var client = new HttpClient())
                {
                    var result = await client.GetAsync("https://asp.net");
                    if (result.StatusCode != HttpStatusCode.OK)
                    {
                        throw new ApplicationException();
                    }
                }
            }
            // Try running
            // netstat -n | findstr "TIME_WAIT"
            // Socket is closed on our end, but Windows will wait 240 seconds before closing the connection

            // Create too HttpClients and you can exhaust the number of available ephemeral ports
        }

        private static async Task HttpClientFactoryMainAsync()
        {
            var serviceProvider = new ServiceCollection()
                                    .AddHttpClient()
                                    .BuildServiceProvider();

            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            Console.WriteLine("Creating HttpClient from HttpClientFactory");
            for (var i = 0; i < 10; i++)
            {
                var client = httpClientFactory.CreateClient();
                var result = await client.GetAsync("https://asp.net");
                if (result.StatusCode != HttpStatusCode.OK)
                {
                    throw new ApplicationException();
                }
            }
            serviceProvider.Dispose();

            // HttpClientFactory pools the underlying HttpClientHandler and manages their lifetimes
            // This is superior to using a static HttpClient instance because it also solves the DNS update problem
        }
    }
}
