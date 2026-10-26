using Microsoft.EntityFrameworkCore.Storage;

namespace Rxintel.Generic.Repositories.Interfaces
{
    /// <summary>
    /// Coordinates transactions and SaveChanges across repositories that share a scoped DbContext.
    /// </summary>
    public interface IUnitOfWork
    {
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task CommitAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default);

        Task RollbackAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default);
    }
}
