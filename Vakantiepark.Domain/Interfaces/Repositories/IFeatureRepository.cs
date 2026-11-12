using Vakantiepark.Domain.Entities;

namespace Vakantiepark.Domain.Interfaces.Repositories
{
    public interface IFeatureRepository : IRepository<Feature>
    {
        /// <summary>
        /// Returns every feature with its options, ordered by name.
        /// </summary>
        Task<List<Feature>> GetAllWithOptionsAsync();
    }
}
