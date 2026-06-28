using FluentValidation;
using Jomla.Application.Common.Behaviors;
using Jomla.Application.Jobs.Expiry;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Jomla.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            services.AddMediatR(opt => opt.RegisterServicesFromAssembly(assembly));
            services.AddValidatorsFromAssembly(assembly);
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            //services.AddScoped<IGroupRequestOfferExpiryJob, GroupRequestOfferExpiryJob>();
            services.AddAutoMapper(cfg => { }, assembly);
            return services;
        }
    }
}
