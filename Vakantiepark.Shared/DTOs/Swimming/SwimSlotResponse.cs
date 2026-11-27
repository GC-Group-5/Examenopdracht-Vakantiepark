namespace Vakantiepark.Shared.DTOs.Swimming
{
    public class SwimSlotResponse
    {
        public Guid Id { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int Capacity { get; set; }
        public int RemainingPlaces { get; set; }
        public bool IsFull { get; set; }
    }
}
