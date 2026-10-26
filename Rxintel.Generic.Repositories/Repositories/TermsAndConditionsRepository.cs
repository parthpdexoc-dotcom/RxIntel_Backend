using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;

namespace Rxintel.Generic.Repositories.Repositories
{
    public class TermsAndConditionsRepository
        : GenericRepository<TermsAndConditions, RxintelDbContext>, ITermsAndConditionsRepository
    {
        private readonly RxintelDbContext _context;

        public TermsAndConditionsRepository(RxintelDbContext context, ILogger<TermsAndConditionsRepository> logger)
            : base(context, logger)
        {
            _context = context;
        }

        public async Task<TermsAndConditions?> GetCurrentAsync(CancellationToken cancellationToken = default)
        {
            return await _context.TermsAndConditions
                .AsNoTracking()
                .OrderByDescending(t => t.ModifiedDate ?? t.CreatedDate)
                .ThenByDescending(t => t.CreatedDate)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
