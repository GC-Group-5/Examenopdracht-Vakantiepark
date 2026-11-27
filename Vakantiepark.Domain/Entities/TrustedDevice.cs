using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    // US-44: a browser where a staff member ticked "Onthoud dit apparaat", so the login code is skipped there for a while.
    public class TrustedDevice : IEntity
    {
        public Guid Id { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation Keys
        public Guid UserId { get; set; }

        // Navigation Properties
        public User User { get; set; } = null!;
    }
}
