namespace Vakantiepark.Shared.Constants
{
    // Role names used in tokens and in [Authorize(Roles = ...)] on both the API and the frontend.
    public static class Roles
    {
        public const string Medewerker = "Medewerker";
        public const string Beheerder = "Beheerder";
        public const string Gast = "Gast";

        public const string Staff = Medewerker + "," + Beheerder;
    }
}
