using Vakantiepark.Shared.Constants;
using Vakantiepark.Domain.Enums;
using Xunit;

namespace Vakantiepark.UnitTests
{
    public class RoleTests
    {
        // The enum name ends up as role claim, so it must match the constant used in [Authorize(Roles = ...)].
        [Theory]
        [InlineData(UserRole.Medewerker, Roles.Medewerker)]
        [InlineData(UserRole.Beheerder, Roles.Beheerder)]
        [InlineData(UserRole.Schoonmaker, Roles.Schoonmaker)]
        public void UserRole_Name_MatchesRoleConstant(UserRole role, string expected)
        {
            Assert.Equal(expected, role.ToString());
        }

        [Fact]
        public void Schoonmaker_IsNotIncludedInStaffPermissions()
        {
            string[] staffRoles = Roles.Staff.Split(',');

            Assert.DoesNotContain(Roles.Schoonmaker, staffRoles);
        }
    }
}
