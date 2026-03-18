using Microsoft.EntityFrameworkCore;
using SatellitePortfolio.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

builder.Services.AddDbContext<SatellitePortfolioDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("PortfolioDb")
                           ?? throw new InvalidOperationException("Connection string 'PortfolioDb' is not configured.");

    options.UseNpgsql(
        connectionString,
        npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(SatellitePortfolioDbContext).Assembly.FullName);
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();

