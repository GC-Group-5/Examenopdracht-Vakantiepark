namespace Vakantiepark.Shared.DTOs.Reservations
{
    // US-51: what staff can move a booking to for the new dates, before saving.
    public class ChangeOptionsResponse
    {
        // True when the booking's own type has no free unit that fits; Types then also holds the other types that do.
        public bool CurrentTypeFull { get; set; }
        public List<ChangeOptionTypeResponse> Types { get; set; } = [];
    }

    public class ChangeOptionTypeResponse
    {
        public string TypeSlug { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;

        // For the new stay with the travellers already on the booking; saving calculates it again.
        public decimal TotalPrice { get; set; }
        public List<ChangeOptionUnitResponse> Units { get; set; } = [];
    }

    public class ChangeOptionUnitResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }

        // One of UnitAvailability; only a Vrij unit can be picked.
        public string Availability { get; set; } = string.Empty;
    }
}
