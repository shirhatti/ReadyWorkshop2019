using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ThreadPoolStarvation
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ThreadPool.GetAvailableThreads(out var workerThreads, out var completionPortThreads);
            workerThreads = 16;
            ThreadPool.SetMaxThreads(workerThreads, completionPortThreads);
            CreateWebHostBuilder(args).Build().Run();
        }

        private static void TestMethod(object obj)
        {
            throw new NotImplementedException();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
