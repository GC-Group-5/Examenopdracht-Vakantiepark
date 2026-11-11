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
        public void UserRole_Name_MatchesRoleConstant(UserRole role, string expected)
        {
            Assert.Equal(expected, role.ToString());
        }
    }
}
