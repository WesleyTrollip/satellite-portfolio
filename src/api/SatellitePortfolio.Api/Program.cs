using SatellitePortfolio.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SatellitePortfolio.Application;
using SatellitePortfolio.Api;
using SatellitePortfolio.Domain;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
    };
});

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetailsFactory = context.HttpContext.RequestServices
                .GetRequiredService<ProblemDetailsFactory>();

            var problemDetails = problemDetailsFactory.CreateValidationProblemDetails(
                context.HttpContext,
                context.ModelState);

            problemDetails.Status ??= StatusCodes.Status400BadRequest;

            return new ObjectResult(problemDetails)
            {
                StatusCode = problemDetails.Status
            };
        };
    });

builder.Services.AddScoped<IUserContext, LocalUserContext>();
builder.Services.AddScoped<IInstrumentRepository, InstrumentRepository>();
builder.Services.AddScoped<ITradeRepository, TradeRepository>();
builder.Services.AddScoped<ICashLedgerRepository, CashLedgerRepository>();
builder.Services.AddScoped<IPriceSnapshotRepository, PriceSnapshotRepository>();
builder.Services.AddScoped<IPortfolioUnitOfWork, PortfolioUnitOfWork>();
builder.Services.AddScoped<IHoldingsCalculator, HoldingsCalculator>();
builder.Services.AddScoped<InstrumentService>();
builder.Services.AddScoped<TradeService>();
builder.Services.AddScoped<CashLedgerService>();

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

app.UseExceptionHandler();
app.UseStatusCodePages();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SatellitePortfolioDbContext>();
    dbContext.Database.Migrate();
}

app.MapControllers();

app.Run();

