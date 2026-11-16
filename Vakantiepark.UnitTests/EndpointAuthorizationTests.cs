using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vakantiepark.API.Controllers;
using Vakantiepark.Shared.Constants;
using Xunit;

namespace Vakantiepark.UnitTests
{
    // US-35: reads every [Authorize] in the API, so a new endpoint that lets cleaners or guests in fails here.
    public class EndpointAuthorizationTests
    {
        // A bare [Authorize] admits every logged-in user, guests and cleaners included; only these may use it.
        private static readonly string[] EndpointsForEveryLoggedInUser = [$"{nameof(AuthController)}.{nameof(AuthController.GetCurrentUser)}"];

        private static List<(string Endpoint, AuthorizeAttribute Attribute)> AuthorizeAttributes()
        {
            List<(string Endpoint, AuthorizeAttribute Attribute)> result = [];
            IEnumerable<Type> controllers = typeof(AuthController).Assembly.GetTypes()
                .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract);

            foreach (Type controller in controllers)
            {
                result.AddRange(controller.GetCustomAttributes<AuthorizeAttribute>().Select(attribute => (controller.Name, attribute)));

                foreach (MethodInfo action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    result.AddRange(action.GetCustomAttributes<AuthorizeAttribute>().Select(attribute => ($"{controller.Name}.{action.Name}", attribute)));
            }

            return result;
        }

        [Fact]
        public void RoleRestrictedEndpoints_NeverAdmitSchoonmaker()
        {
            List<string> admittingCleaners = AuthorizeAttributes()
                .Where(entry => entry.Attribute.Roles is not null && entry.Attribute.Roles.Split(',').Contains(Roles.Schoonmaker))
                .Select(entry => entry.Endpoint)
                .ToList();

            Assert.Empty(admittingCleaners);
        }

        [Fact]
        public void AuthorizeWithoutRoles_OnlyOnEndpointsForEveryLoggedInUser()
        {
            List<string> withoutRoles = AuthorizeAttributes()
                .Where(entry => string.IsNullOrWhiteSpace(entry.Attribute.Roles))
                .Select(entry => entry.Endpoint)
                .Where(endpoint => !EndpointsForEveryLoggedInUser.Contains(endpoint))
                .ToList();

            Assert.Empty(withoutRoles);
        }

        [Fact]
        public void MaintenancePeriods_OnlyForStaff()
        {
            AuthorizeAttribute? attribute = typeof(MaintenancePeriodsController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.Equal(Roles.Staff, attribute?.Roles);
        }
    }
}
