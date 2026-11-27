using Vakantiepark.Domain.Interfaces;

namespace Vakantiepark.Domain.Entities
{
    // One swim slot in the pool (US-22). Slots are stored the first time a day is asked for, so every slot
    // keeps its own capacity and later park settings only apply to days that have no slots yet.
    public class SwimSlot : IEntity
    {
        public Guid Id { get; set; }

        // Park time (Europe/Amsterdam).
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        public int Capacity { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
