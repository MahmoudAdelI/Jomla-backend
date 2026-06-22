using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Jobs.JobDispatcher
{
    public interface IBackgroundJobDispatcher
    {
        void Enqueue<TJob>(Expression<Func<TJob, Task>> methodCall);
        string Schedule<TJob>(Expression<Func<TJob, Task>> methodCall, TimeSpan delay);
        string Schedule<TJob>(Expression<Func<TJob, Task>> methodCall, DateTimeOffset enqueueAt);
    }
}
