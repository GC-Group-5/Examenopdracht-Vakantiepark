using System;
using System.Collections.Generic;
using System.Text;

namespace Vakantiepark.Shared.DTOs.Accommodation
{
    public class AccommodationTypeResponse
    {
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int MaxPersons { get; set; }                 
        public int UnitCount { get; set; }                    
        public List<TypeFeatureResponse> Features { get; set; } = [];
    }

    public class TypeFeatureResponse
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ValueType { get; set; } = string.Empty;  
        public int UnitCount { get; set; }                 
        public decimal? MinNumber { get; set; }             
        public decimal? MaxNumber { get; set; }            
        public List<string> OptionNames { get; set; } = [];  
    }
}
