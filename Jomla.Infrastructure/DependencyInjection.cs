using Hangfire;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Common.Settings;
using Jomla.Application.Jobs.Agents;
using Jomla.Application.Jobs.Closing;
using Jomla.Application.Jobs.Expiry;
using Jomla.Application.Jobs.Fulfillment;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Application.Jobs.Matching;
using Jomla.Application.Jobs.Sync;
using Jomla.Domain.Entities;
using Jomla.Infrastructure.AI;
using Jomla.Infrastructure.Auth;
using Jomla.Infrastructure.Jobs.Agents;
using Jomla.Infrastructure.Jobs.Closing;
using Jomla.Infrastructure.Jobs.Expiry;
using Jomla.Infrastructure.Jobs.Fulfillment;
using Jomla.Infrastructure.Jobs.JobDispatcher;
using Jomla.Infrastructure.Jobs.Matching;
using Jomla.Infrastructure.Jobs.Sync;
using Jomla.Infrastructure.Persistance;
using Jomla.Infrastructure.Persistance.Qdrant;
using Jomla.Infrastructure.Persistance.Seeders;
using Jomla.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using OpenAI;
using Qdrant.Client;
using System.ClientModel;
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
            services.AddScoped<ISupplierOfferExpiryJob, SupplierOfferExpiryJob>();
            services.AddScoped<IModerateSupplierOfferJob, ModerateSupplierOfferJob>();
            services.AddScoped<IModerateGroupRequestJob, ModerateGroupRequestJob>();
            services.AddScoped<IBatchCompletionJob, BatchCompletionJob>();
            services.AddScoped<IBackgroundJobDispatcher, HangfireJobDispatcher>();
            services.AddScoped<ISupplierMatchingJob, SupplierMatchingJob>();
            services.AddScoped<IGroupRequestAutoCloseJob, GroupRequestAutoCloseJob>();
            services.AddScoped<IGroupRequestOfferExpiryJob, GroupRequestOfferExpiryJob>();
            services.AddScoped<INegotiationRoundIndexJob, NegotiationRoundIndexJob>();
            services.AddScoped<INegotiationRoundSyncJob, NegotiationRoundSyncJob>();
            #endregion

            #region Identity
            var jwtSettings = config.GetSection("Jwt").Get<JwtSettings>()!;
            services.Configure<JwtSettings>(config.GetSection("Jwt"));

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

            services.AddAuthorization();
            #endregion

            #region AI
            var token = config["AI:Token"]
                ?? throw new InvalidOperationException("AI:Token is not configured.");

            services.AddOpenAIChatCompletion(
                modelId: config["AI:ModelId"]!,
                endpoint: new Uri(config["AI:Endpoint"]!),
                apiKey: token);

            #pragma warning disable CS0618
            services.AddOpenAITextEmbeddingGeneration(
                modelId: config["AI:EmbeddingModelId"]!,
                openAIClient: new OpenAIClient(
                    new ApiKeyCredential(token),
                    new OpenAIClientOptions { Endpoint = new Uri(config["AI:Endpoint"]!) }
                ));
            #pragma warning restore CS0618

            services.AddScoped<IModerationAgent, ModerationAgent>();
            services.AddScoped<ICategoryAgent, CategoryAgent>();
            services.AddScoped<INegotiationRoundIndexer, NegotiationRoundIndexer>();
            services.AddScoped<INegotiationAgent, NegotiationAgent>();
            #endregion


            // MOW : Cloudinary dependancy ###
            #region Cloudinary

            services.Configure<CloudinarySettings>(config.GetSection("CloudinarySettings"));

            services.AddScoped<IImageService,
                CloudinaryImageService>();

            #endregion

            #region Qdrant
            // Qdrant
            var qdrantOptions = config.GetSection("Qdrant").Get<QdrantSettings>()!;
            services.AddSingleton(new QdrantClient(
                host: qdrantOptions.Url,
                https:true,
                apiKey: qdrantOptions.ApiKey));

            services.AddScoped<NegotiationRoundsCollectionInitializer>();
            #endregion

            services.AddScoped<DataSeeder>();

            // Infrastructure implementations
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IIdentityService, IdentityService>();
            return services;
        }
    }
}
