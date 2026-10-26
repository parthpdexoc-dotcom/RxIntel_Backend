using Microsoft.EntityFrameworkCore.Storage;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;

namespace Rxintel.Generic.Repositories.Repositories
{
    /// <summary>
    /// Unit of Work over the shared <see cref="RxintelDbContext"/>.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly RxintelDbContext _dbContext;

        public UnitOfWork(RxintelDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            _dbContext.Database.BeginTransactionAsync(cancellationToken);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _dbContext.SaveChangesAsync(cancellationToken);

        public async Task CommitAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(transaction);
            await transaction.CommitAsync(cancellationToken);
        }

        public async Task RollbackAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(transaction);
            await transaction.RollbackAsync(cancellationToken);
        }
    }
}
