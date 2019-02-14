using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace HttpContextBackground
{
    public class MessageQueue
    {
        public BlockingCollection<ILogContext> Collection { get; } = new BlockingCollection<ILogContext>(100);

        public virtual void EnqueueMessage(HttpContext context)
        {
            if (!Collection.IsAddingCompleted)
            {
                try
                {
                    Collection.Add(new CopyLogContext(context));
                    return;
                }
                catch (InvalidOperationException) { }
            }
        }
    }

    public class CustomHostedService : HostedService
    {
        private readonly MessageQueue _messageQueue;
        public CustomHostedService(MessageQueue messageQueue)
        {
            _messageQueue = messageQueue;
        }
        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                await Task.Yield();
                try
                {
                    foreach (var logContext in _messageQueue.Collection.GetConsumingEnumerable(cancellationToken))
                    {
                        Console.WriteLine($"Request path: {logContext.Path}\tRequest ID: {logContext.TraceIdentifier}\n");
                        
                    }
                }
                catch
                {
                    try
                    {
                        _messageQueue.Collection.CompleteAdding();
                    }
                    catch { }
                }
            }
        }
    }

    public abstract class HostedService : IHostedService
    {
        private Task _executingTask;
        private CancellationTokenSource _cts;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Create a linked token so we can trigger cancellation outside of this token's cancellation
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Store the task we're executing
            _executingTask = ExecuteAsync(_cts.Token);

            // If the task is completed then return it, otherwise it's running
            return _executingTask.IsCompleted ? _executingTask : Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (_executingTask == null)
            {
                return;
            }

            // Signal cancellation to the executing method
            _cts.Cancel();

            // Wait until the task completes or the stop token triggers
            await Task.WhenAny(_executingTask, Task.Delay(-1, cancellationToken));

            // Throw if cancellation triggered
            cancellationToken.ThrowIfCancellationRequested();
        }

        // Derived classes should override this and execute a long running method until 
        // cancellation is requested
        protected abstract Task ExecuteAsync(CancellationToken cancellationToken);
    }
}
