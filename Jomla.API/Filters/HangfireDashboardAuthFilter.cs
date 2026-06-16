using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace Jomla.API.Filters
{
    public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context)
        {
            var env = context.GetHttpContext().RequestServices
            .GetRequiredService<IWebHostEnvironment>();

            return env.IsDevelopment();
        }
    }
}
