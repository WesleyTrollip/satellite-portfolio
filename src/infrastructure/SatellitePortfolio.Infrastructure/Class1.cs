using Microsoft.EntityFrameworkCore;

namespace SatellitePortfolio.Infrastructure;

public class SatellitePortfolioDbContext(DbContextOptions<SatellitePortfolioDbContext> options)
    : DbContext(options)
{
    // DbSets will be added in T010–T012 when the domain model is defined.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity configurations will be added alongside the EF mappings task (T020).
    }
}

