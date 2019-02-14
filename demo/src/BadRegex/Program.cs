// Credits to https://github.com/zzbennett/RegexPerfTest

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BadRegex
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<RegexBenchmark>();
        }
    }

    [SimpleJob(launchCount: 1, warmupCount: 0, targetCount: 1)]
    public class RegexBenchmark
    {
        private Regex _badRegex;
        private Regex _goodRegex;

        [ParamsSource(nameof(LogLineSource))]
        public string LogLine { get; set; }

        public static IEnumerable<string> LogLineSource => new[]
        {
            "2014-08-26 app[web.1]: 50.0.134.125 - - [26/Aug/2014 00:27:41] \"GET / HTTP/1.1\" 200 14 0.0005",
            "50.0.134.125 - - [26/Aug/2014 00:27:41] \\\"GET / HTTP/1.1\\\" 200 14 0.0005"
        };

        [GlobalSetup]
        public void GlobalSetup()
        {
            var badPattern = ".* (.*)\\[(.*)\\]:.*";
            _badRegex = new Regex(badPattern);

            var goodPattern = "[12]\\d{3}-[01]\\d-[0-3]\\d ([^ \\[]*?)\\[([^\\]]*?)\\]:.*";
            _goodRegex = new Regex(goodPattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));
        }

        [Benchmark]
        public bool BadRegex()
        {
            return _badRegex.IsMatch(LogLine);
        }

        [Benchmark]
        public bool GoodRegex()
        {
            return _goodRegex.IsMatch(LogLine);
        }

    }
}
