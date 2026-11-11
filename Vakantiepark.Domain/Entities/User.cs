using Vakantiepark.Domain.Interfaces;
using Vakantiepark.Domain.Enums;

namespace Vakantiepark.Domain.Entities
{
    public class User : IEntity
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;

        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
