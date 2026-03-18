using Microsoft.EntityFrameworkCore;
using SatellitePortfolio.Application;
using SatellitePortfolio.Domain;
using SatellitePortfolio.Infrastructure;
using SatellitePortfolio.RiskWorker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<SatellitePortfolioDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("PortfolioDb")
                           ?? "Host=localhost;Port=5432;Database=satellite_portfolio;Username=satellite;Password=satellite";

    options.UseNpgsql(
        connectionString,
        npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(SatellitePortfolioDbContext).Assembly.FullName);
        });
});

builder.Services.AddScoped<IInstrumentRepository, InstrumentRepository>();
builder.Services.AddScoped<ITradeRepository, TradeRepository>();
builder.Services.AddScoped<ICashLedgerRepository, CashLedgerRepository>();
builder.Services.AddScoped<IPriceSnapshotRepository, PriceSnapshotRepository>();
builder.Services.AddScoped<IAlertEventRepository, AlertEventRepository>();
builder.Services.AddScoped<IPortfolioRuleRepository, PortfolioRuleRepository>();
builder.Services.AddScoped<IPortfolioUnitOfWork, PortfolioUnitOfWork>();
builder.Services.AddScoped<IHoldingsCalculator, HoldingsCalculator>();
builder.Services.AddScoped<IPortfolioRuleEvaluator, PortfolioRuleEvaluator>();
builder.Services.AddScoped<RiskEvaluationService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
