FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/workers/SatellitePortfolio.RiskWorker/SatellitePortfolio.RiskWorker.csproj", "src/workers/SatellitePortfolio.RiskWorker/"]
COPY ["src/application/SatellitePortfolio.Application/SatellitePortfolio.Application.csproj", "src/application/SatellitePortfolio.Application/"]
COPY ["src/infrastructure/SatellitePortfolio.Infrastructure/SatellitePortfolio.Infrastructure.csproj", "src/infrastructure/SatellitePortfolio.Infrastructure/"]
COPY ["src/domain/SatellitePortfolio.Domain/SatellitePortfolio.Domain.csproj", "src/domain/SatellitePortfolio.Domain/"]

RUN dotnet restore "src/workers/SatellitePortfolio.RiskWorker/SatellitePortfolio.RiskWorker.csproj"

COPY . .
RUN dotnet publish "src/workers/SatellitePortfolio.RiskWorker/SatellitePortfolio.RiskWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SatellitePortfolio.RiskWorker.dll"]
