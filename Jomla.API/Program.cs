
using Jomla.API.Middleware;
using Jomla.Application;
using Jomla.Infrastructure;
using Jomla.Infrastructure.Persistance.Seeders;
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
            builder.Services.AddSwaggerGen();

            var app = builder.Build();
            app.UseExceptionHandler();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();

                using (var scope = app.Services.CreateScope())
                {
                    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
                    await seeder.SeedAsync();
                }
            }

            app.UseHttpsRedirection();

            app.UseCors("Angular");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
