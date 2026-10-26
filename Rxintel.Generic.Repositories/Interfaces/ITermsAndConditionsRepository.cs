using Rxintel.Generic.Entities.Entities;

namespace Rxintel.Generic.Repositories.Interfaces
{
    public interface ITermsAndConditionsRepository : IGenericRepository<TermsAndConditions>
    {
        Task<TermsAndConditions?> GetCurrentAsync(CancellationToken cancellationToken = default);
    }
}
