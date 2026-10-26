using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Rxintel.Generic.Entities.Entities;
namespace Rxintel.AI
{
    public class RxintelDbContextFactory : IDesignTimeDbContextFactory<RxintelDbContext>
    {
        public RxintelDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<RxintelDbContext>();

            // Use the same connection string as in appsettings.json
            optionsBuilder.UseSqlServer("Server=192.168.1.2;Database=RxintelLocal;User Id=sa;Password=plexoc123;TrustServerCertificate=True;");

            return new RxintelDbContext(optionsBuilder.Options);
        }
    }
}