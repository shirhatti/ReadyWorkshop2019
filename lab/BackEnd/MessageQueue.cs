using BackEnd.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

namespace BackEnd
{
    public partial class MessageQueue
    {
        private BlockingCollection<ILogContext> _messageQueue = new BlockingCollection<ILogContext>(100);
        private ConcurrentDictionary<string, int> _hitCount = new ConcurrentDictionary<string, int>();
        private readonly Thread _outputThread;

        public MessageQueue()
        {
            _outputThread = new Thread(ProcessMessageQueue)
            {
                IsBackground = true,
                Name = "Message queue processing thread"
            };
            _outputThread.Start();
        }

        private void ProcessMessageQueue()
        {
            try
            {
                foreach (var message in _messageQueue.GetConsumingEnumerable())
                {
                    _hitCount.AddOrUpdate(WebUtility.UrlEncode(message.Path), 1, (id, count) => count + 1);
                }
            }
            catch
            {
                try
                {
                    _messageQueue.CompleteAdding();
                }
                catch { }
            }
        }

        public IEnumerable<PathCount> GetPathCount()
        {
            var pathCounts = new List<PathCount>();
            foreach (var item in _hitCount)
            {
                pathCounts.Add(new PathCount {Path = item.Key, Count = item.Value});
            }
            return pathCounts;
        }
        public PathCount GetPathCount(string path)
        {
            if (_hitCount.TryGetValue(path, out var value))
            {
                return new PathCount
                {
                    Path = path,
                    Count = value
                };
            }
            else
            {
                return new PathCount
                {
                    Path = path,
                    Count = 0
                };
            }
        }

        public void EnqueueMessage(HttpContext context)
        {
            if (!_messageQueue.IsAddingCompleted)
            {
                try
                {
                    _messageQueue.Add(new LogContext(context));
                    return;
                }
                catch (InvalidOperationException) { }
            }
        }
    }
}
