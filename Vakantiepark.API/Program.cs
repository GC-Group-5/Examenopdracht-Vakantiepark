using Vakantiepark.Infrastructure.Persistence;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Vakantiepark.API.Middlewares;
using Vakantiepark.Infrastructure;
using Vakantiepark.Application;
using System.Globalization;

namespace Vakantiepark.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.RegisterAPI();
            builder.Services.RegisterApplication();
            builder.Services.RegisterInfrastructure(
                builder.Configuration.GetConnectionString("vakantiepark")
                    ?? throw new InvalidOperationException("The connection string 'vakantiepark' was not found. Start the application through Vakantiepark.AppHost."));

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
            app.UseRateLimiter();
            app.MapControllers();

            app.Run();
        }
    }
}
