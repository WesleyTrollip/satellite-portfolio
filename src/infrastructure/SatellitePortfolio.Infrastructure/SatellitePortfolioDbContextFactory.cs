using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SatellitePortfolio.Infrastructure;

public sealed class SatellitePortfolioDbContextFactory : IDesignTimeDbContextFactory<SatellitePortfolioDbContext>
{
    public SatellitePortfolioDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SatellitePortfolioDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=satellite_portfolio;Username=satellite;Password=satellite",
            npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(SatellitePortfolioDbContext).Assembly.FullName));
        return new SatellitePortfolioDbContext(optionsBuilder.Options);
    }
}

