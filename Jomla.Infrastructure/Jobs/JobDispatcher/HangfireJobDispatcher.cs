using Hangfire;
using Jomla.Application.Jobs.JobDispatcher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Infrastructure.Jobs.JobDispatcher
{
    public class HangfireJobDispatcher : IBackgroundJobDispatcher
    {
        public void Enqueue<TJob>(Expression<Func<TJob, Task>> methodCall)
        {
            BackgroundJob.Enqueue(methodCall);
        }

        public string Schedule<TJob>(Expression<Func<TJob, Task>> methodCall, TimeSpan delay)
            => BackgroundJob.Schedule(methodCall, delay);

        public string Schedule<TJob>(Expression<Func<TJob, Task>> methodCall, DateTimeOffset enqueueAt)
            => BackgroundJob.Schedule(methodCall, enqueueAt);
    }
}
