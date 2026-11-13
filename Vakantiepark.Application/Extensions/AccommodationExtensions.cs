using Vakantiepark.Application.Helpers;
using Vakantiepark.Shared.DTOs.Accommodation;
using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Application.Extensions
{
    public static class AccommodationExtensions
    {
        public static AccommodationResponse ToResponse(this Accommodation accommodation)
            => new AccommodationResponse()
            {
                Code = accommodation.Code,
                Name = accommodation.Name,
                TypeName = accommodation.Type.Name,
                TypeSlug = accommodation.Type.Slug,
                TypeDescription = accommodation.Type.Description,
                Category = accommodation.Type.Category.ToString(),
                Zone = accommodation.Zone.Name,
                MaxPersons = accommodation.MaxPersons,
                Features = accommodation.Features
                    .OrderBy(value => value.Feature.Name)
                    .Select(value => new AccommodationFeatureResponse()
                    {
                        Key = value.Feature.Key,
                        Name = value.Feature.Name,
                        ValueType = value.Feature.ValueType.ToString(),
                        NumberValue = value.NumberValue,
                        OptionValue = value.Option?.Value,
                        OptionName = value.Option?.Name,
                    })
                    .ToList(),
            };

        public static FeatureResponse ToResponse(this Feature feature)
            => new FeatureResponse()
            {
                Key = feature.Key,
                Name = feature.Name,
                ValueType = feature.ValueType.ToString(),
                Category = feature.Category?.ToString(),
                Options = feature.Options
                    .OrderBy(option => option.SortOrder)
                    .Select(option => new FeatureOptionResponse() { Value = option.Value, Name = option.Name })
                    .ToList(),
            };

        public static AccommodationTypeResponse ToTypeResponse(this IGrouping<string, Accommodation> units)
        {
            List<Accommodation> unitList = units.ToList();
            AccommodationType type = unitList.First().Type;

            return new AccommodationTypeResponse()
            {
                Slug = type.Slug,
                Name = type.Name,
                Category = type.Category.ToString(),
                Description = type.Description,
                MaxPersons = unitList.Max(unit => unit.MaxPersons),
                UnitCount = unitList.Count,
                Zones = unitList
                    .Select(unit => unit.Zone.Name)
                    .Distinct()
                    .OrderBy(zone => zone)
                    .ToList(),
                Highlights = AccommodationTypeHighlights.For(unitList),
                Features = unitList
                    .SelectMany(unit => unit.Features)
                    .GroupBy(value => value.Feature.Key)
                    .Select(values => new TypeFeatureResponse()
                    {
                        Key = values.Key,
                        Name = values.First().Feature.Name,
                        ValueType = values.First().Feature.ValueType.ToString(),
                        UnitCount = values.Count(),
                        MinNumber = values.Min(value => value.NumberValue),
                        MaxNumber = values.Max(value => value.NumberValue),
                        OptionNames = values
                            .Where(value => value.Option is not null)
                            .Select(value => value.Option!.Name)
                            .Distinct()
                            .ToList(),
                    })
                    .OrderBy(feature => feature.Name)
                    .ToList(),
            };
        }
    }
}
