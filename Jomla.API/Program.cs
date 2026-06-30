using Hangfire;
using Jomla.API.Filters;
using Jomla.API.Hubs;
using Jomla.API.Middleware;
using Jomla.API.Services;
using Jomla.Application;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Jobs.Sync;
using Jomla.Infrastructure;
using Jomla.Infrastructure.Payments;
using Jomla.Infrastructure.Persistance.Qdrant;
using Jomla.Infrastructure.Persistance.Seeders;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;


namespace Jomla.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            // Application Layer Dependencies
            builder.Services.AddApplication();

            // Infrastructure Layer Dependencies
            builder.Services.AddInfrastructure(builder.Configuration);

            // SignalR
            builder.Services.AddSignalR();
            builder.Services.AddScoped<IRealtimeService, RealtimeService>();

            // Global exception handling
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            builder.Services.AddCors(opt =>
                 opt.AddPolicy("Angular", p =>
                 p.WithOrigins("http://localhost:4200")
                 .AllowAnyHeader()
                 .AllowAnyMethod()
                 .AllowCredentials()));

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())); // This will serialize enums as strings in JSON responses
            builder.Services.AddOpenApi();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(opt =>
            {
                opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT access token."
                });

                opt.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });
            });

            //stripe services
            builder.Services.AddScoped<IStripePaymentService>(provider =>
             new StripePaymentService(
                 builder.Configuration["Stripe:SecretKey"]!
             )
             );
        

            var app = builder.Build();
            app.UseExceptionHandler();
            // Configure the HTTP request pipeline.
            bool didSeed = false;
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();

                using (var scope = app.Services.CreateScope())
                {
                    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
                    didSeed = await seeder.SeedAsync();
                    await seeder.SeedAdminAsync();
                }
            }

            // Hangfire dashboard
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new HangfireDashboardAuthFilter()]
            });
            app.MapHangfireDashboard();

            // Schedule recurring sync job daily at midnight
            using (var scope = app.Services.CreateScope())
            {
                var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
                recurringJobManager.AddOrUpdate<INegotiationRoundSyncJob>(
                    "negotiation-round-sync",
                    job => job.ExcuteAsync(),
                    Cron.Daily(0) // Runs every day at 12:00 AM (midnight)
                );
            }


            // Initialize Qdrant collections
            using (var scope = app.Services.CreateScope())
            {
                var initializer = scope.ServiceProvider
                    .GetRequiredService<NegotiationRoundsCollectionInitializer>();
                await initializer.InitializeAsync();

                if (app.Environment.IsDevelopment() && didSeed)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var bgScope = app.Services.CreateScope();
                            var syncJob = bgScope.ServiceProvider.GetRequiredService<INegotiationRoundSyncJob>();
                            await syncJob.ExcuteAsync();
                        }
                        catch (Exception ex)
                        {
                            using var bgScope = app.Services.CreateScope();
                            var logger = bgScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                            logger.LogWarning(ex, "Background task failed to sync negotiation rounds to Qdrant.");
                        }
                    });
                }
            }

            app.UseHttpsRedirection();

            app.UseCors("Angular");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<JomlaHub>("/hubs/jomla");
            app.Run();
          
        }
    }
}
