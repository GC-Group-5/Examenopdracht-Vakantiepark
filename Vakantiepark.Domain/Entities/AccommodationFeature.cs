namespace Vakantiepark.Domain.Entities
{
    // The value of one feature for one accommodation (table accommodation_features).
    // Yes/no feature: the row exists only when the answer is yes; both values stay null.
    // Number feature: NumberValue, e.g. 2 bedrooms or 7.5 metres.
    // Option feature: the chosen FeatureOption, e.g. 'Versterkt gras'.
    public class AccommodationFeature
    {
        public Guid AccommodationId { get; set; }
        public Guid FeatureId { get; set; }

        public decimal? NumberValue { get; set; }
        public Guid? FeatureOptionId { get; set; }

        // Navigation Properties
        public Accommodation Accommodation { get; set; } = null!;
        public Feature Feature { get; set; } = null!;
        public FeatureOption? Option { get; set; }
    }
}
