using Hangfire;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Common.Settings;
using Jomla.Domain.Entities;
using Jomla.Infrastructure.Auth;
using Jomla.Infrastructure.Persistance;
using Jomla.Infrastructure.Persistance.Seeders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Jomla.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            // Register your infrastructure services here

            #region EF Core
            // AddDbContextFactory registers both the factory (for Hangfire jobs that need
            // to own their own DbContext lifetime) and a scoped AppDbContext (for normal
            // request-scoped usage). IAppDbContext is wired manually because the factory
            // registration does not auto-register custom interfaces.
            services.AddDbContextFactory<AppDbContext>(opt =>
                opt.UseSqlServer(config.GetConnectionString("Default"))
            );
            services.AddScoped<IAppDbContext>(provider =>
                provider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());
            #endregion

            #region Hangfire
            services.AddHangfire(hangfire => hangfire
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(config.GetConnectionString("Default"))
            );

            services.AddHangfireServer();
            // Job registrations

            #endregion

            #region Identity
            var jwtSettings = config.GetSection("Jwt").Get<JwtSettings>()!;
            services.AddSingleton(jwtSettings);

            services.AddIdentityCore<AppUser>(opt =>
            {
                opt.Password.RequireNonAlphanumeric = false;
                opt.Password.RequireUppercase = false;
                opt.Password.RequireLowercase = false;
                opt.Password.RequireDigit = true;
                opt.Password.RequiredLength = 8;
            })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddSignInManager<SignInManager<AppUser>>();

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(opt =>
                {
                    opt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        ClockSkew = TimeSpan.Zero
                    };

                    // Allow SignalR to receive JWT from query string
                    opt.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = ctx =>
                        {
                            var token = ctx.Request.Query["access_token"];
                            var path = ctx.HttpContext.Request.Path;

                            if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/hubs/jomla"))
                                ctx.Token = token;

                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthentication();
            services.AddAuthorization();
            #endregion

            services.AddScoped<DataSeeder>();

            // Infrastructure implementations
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IIdentityService, IdentityService>();
            return services;
        }
    }
}
