using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace FormatLogMessages
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<LogBenchmark>();
        }
    }

    [ShortRunJob]
    public class LogBenchmark
    {
        private ServiceProvider serviceProvider;
        private static Action<ILogger, int, Exception> logIteration;
        private ILogger logger;

        [GlobalSetup]
        public void GlobalSetup()
        {
            serviceProvider = new ServiceCollection()
                .AddLogging(logBuilder =>
                {
                    logBuilder.AddConsole();
                })
                .BuildServiceProvider();
            //logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<LogBenchmark>();
            logger = NullLogger.Instance;
            logIteration = LoggerMessage.Define<int>(LogLevel.Information,
                                                        eventId: 1,
                                                        formatString: "Message '{i}'");
        }

        [Benchmark]
        public void LogStringInterpolation()
        {
            logger.LogInformation($"Message '{0}'");
        }

        [Benchmark]
        public void LogString()
        {
            logger.LogInformation("Message '{i}'", 0);  
        }

        [Benchmark]
        public void LogFormat()
        {
            logIteration(logger, 0, null);
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            serviceProvider.Dispose();
        }
    }
}
