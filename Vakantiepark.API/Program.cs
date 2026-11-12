using Vakantiepark.Infrastructure.Persistence;
using Vakantiepark.Application.Settings;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Vakantiepark.API.Middlewares;
using Vakantiepark.Infrastructure;
using Vakantiepark.Infrastructure.Persistence.Seeders;
using Vakantiepark.Application;
using System.Globalization;

namespace Vakantiepark.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            JwtSettings jwtSettings = builder.Configuration.GetSection("JWT").Get<JwtSettings>()
                ?? throw new InvalidOperationException("The 'JWT' settings were not found.");

            if (jwtSettings.Key.Length < JwtSettings.MinimumKeyLength)
                throw new InvalidOperationException("The setting 'JWT:Key' is missing or too short. Start the application through Vakantiepark.AppHost.");

            builder.Services.RegisterAPI(jwtSettings);
            builder.Services.RegisterApplication(jwtSettings);
            builder.Services.RegisterInfrastructure(
                builder.Configuration.GetConnectionString("vakantiepark")
                    ?? throw new InvalidOperationException("The connection string 'vakantiepark' was not found. Start the application through Vakantiepark.AppHost."),
                seedTestData: builder.Environment.IsDevelopment());

            builder.Services.AddCors(options =>
            {
                string frontendUrl = builder.Configuration["Frontend:BaseUrl"]
                    ?? throw new InvalidOperationException("The setting 'Frontend:BaseUrl' was not found.");

                options.AddDefaultPolicy(policy => policy
                    .WithOrigins(frontendUrl)
                    .AllowAnyHeader()
                    .AllowAnyMethod());
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                ApplicationDBContext context = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                context.Database.Migrate();

                // Seeddata first (units + types), then park settings, seasons and prices.
                await context.SeedAccommodationsAsync();
                await context.SeedParkConfigurationAsync();
            }

            app.UseExceptionHandler("/error");

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger(options => options.RouteTemplate = "{documentName}/specifications.json");
                app.UseSwaggerUI(options =>
                {
                    options.RoutePrefix = "api";
                    options.SwaggerEndpoint("specifications.json", "API");
                });

                app.MapGet("/", context =>
                {
                    context.Response.Redirect("/api");
                    return Task.CompletedTask;
                });

                app.UseMiddleware<LogEndpointMiddleware>();
            }

            app.UseRequestLocalization(new RequestLocalizationOptions()
            {
                DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture)
            });

            app.UseCors();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRateLimiter();
            app.MapControllers();

            app.Run();
        }
    }
}
