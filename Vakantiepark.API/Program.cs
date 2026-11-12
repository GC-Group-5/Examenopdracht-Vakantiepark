using Vakantiepark.Infrastructure.Persistence;
using Vakantiepark.Infrastructure.Settings;
using Vakantiepark.Application.Settings;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Vakantiepark.API.Middlewares;
using Vakantiepark.Infrastructure;
using Vakantiepark.Application;
using System.Globalization;
using System.Data.Common;

namespace Vakantiepark.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            JwtSettings jwtSettings = builder.Configuration.GetSection("JWT").Get<JwtSettings>()
                ?? throw new InvalidOperationException("The 'JWT' settings were not found.");

            if (jwtSettings.Key.Length < JwtSettings.MinimumKeyLength)
                throw new InvalidOperationException("The setting 'JWT:Key' is missing or too short. Start the application through Vakantiepark.AppHost.");

            EmailSettings emailSettings = CreateEmailSettings(builder.Configuration);

            builder.Services.RegisterAPI(jwtSettings);
            builder.Services.RegisterApplication(jwtSettings);
            builder.Services.RegisterInfrastructure(
                builder.Configuration.GetConnectionString("vakantiepark")
                    ?? throw new InvalidOperationException("The connection string 'vakantiepark' was not found. Start the application through Vakantiepark.AppHost."),
                emailSettings,
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

        // Default: Mailpit, supplied by Vakantiepark.AppHost as "Endpoint=smtp://host:port".
        // Optional: an external test SMTP server (e.g. Mailtrap) set in this project's user-secrets under "Email:Smtp".
        private static EmailSettings CreateEmailSettings(IConfiguration configuration)
        {
            string fromAddress = configuration["Email:FromAddress"]
                ?? throw new InvalidOperationException("The setting 'Email:FromAddress' was not found.");

            IConfigurationSection smtp = configuration.GetSection("Email:Smtp");
            if (!string.IsNullOrWhiteSpace(smtp["Host"]))
            {
                return new EmailSettings()
                {
                    Host = smtp["Host"]!,
                    Port = smtp.GetValue<int?>("Port")
                        ?? throw new InvalidOperationException("The setting 'Email:Smtp:Port' was not found."),
                    Username = smtp["Username"] ?? string.Empty,
                    Password = smtp["Password"] ?? string.Empty,
                    UseTls = true,
                    FromAddress = fromAddress
                };
            }

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
