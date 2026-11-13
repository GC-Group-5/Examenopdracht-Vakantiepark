using Vakantiepark.Infrastructure.Persistence;
using Vakantiepark.Infrastructure.Settings;
using Vakantiepark.Application.Settings;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Vakantiepark.API.Middlewares;
using Vakantiepark.Infrastructure;
using Vakantiepark.Infrastructure.Persistence.Seeders;
using Vakantiepark.Application;
using System.Globalization;
using System.Data.Common;

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

            EmailSettings emailSettings = CreateEmailSettings(builder.Configuration);
            FrontendSettings frontendSettings = new FrontendSettings
            {
                BaseUrl = builder.Configuration["Frontend:BaseUrl"]
                    ?? throw new InvalidOperationException("The setting 'Frontend:BaseUrl' was not found.")
            };
            builder.Services.AddSingleton(frontendSettings);

            builder.Services.RegisterAPI(jwtSettings);
            builder.Services.RegisterApplication(jwtSettings);
            builder.Services.RegisterInfrastructure(
                builder.Configuration.GetConnectionString("vakantiepark")
                    ?? throw new InvalidOperationException("The connection string 'vakantiepark' was not found. Start the application through Vakantiepark.AppHost."),
                emailSettings,
                seedTestData: builder.Environment.IsDevelopment());

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy => policy
                    .WithOrigins(frontendSettings.BaseUrl)
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

        // The API always sends to Mailpit, supplied by Vakantiepark.AppHost as "Endpoint=smtp://host:port".
        // Mailpit can relay to a real inbox; that is configured in the AppHost, so the API needs no secrets.
        private static EmailSettings CreateEmailSettings(IConfiguration configuration)
        {
            string fromAddress = configuration["Email:FromAddress"]
                ?? throw new InvalidOperationException("The setting 'Email:FromAddress' was not found.");

            string connectionString = configuration.GetConnectionString("mailpit")
                ?? throw new InvalidOperationException("The connection string 'mailpit' was not found. Start the application through Vakantiepark.AppHost.");

            DbConnectionStringBuilder parts = new DbConnectionStringBuilder() { ConnectionString = connectionString };
            Uri endpoint = new Uri((string)parts["Endpoint"]);

            return new EmailSettings()
            {
                Host = endpoint.Host,
                Port = endpoint.Port,
                FromAddress = fromAddress
            };
        }
    }
}
