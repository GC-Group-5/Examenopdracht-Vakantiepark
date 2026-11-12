using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Domain.Interfaces.Repositories
{
    public interface IParkSettingsRepository : IRepository<ParkSettings>
    {
        /// <summary>
        /// Returns the single row with the park-wide settings.
        /// </summary>
        Task<ParkSettings?> GetAsync();
    }
}
