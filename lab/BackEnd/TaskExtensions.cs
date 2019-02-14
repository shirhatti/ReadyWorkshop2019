using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.Threading.Tasks
{
    public static class TaskExtensions
    {
        public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout)
        {
            var delayTask = Task.Delay(timeout);

            var resultTask = await Task.WhenAny(task, delayTask);
            if (resultTask == delayTask)
            {
                // Operation cancelled
                throw new OperationCanceledException();
            }

            return await task;
        }
    }
}
