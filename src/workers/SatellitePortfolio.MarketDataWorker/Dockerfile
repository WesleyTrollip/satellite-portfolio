FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/workers/SatellitePortfolio.MarketDataWorker/SatellitePortfolio.MarketDataWorker.csproj", "src/workers/SatellitePortfolio.MarketDataWorker/"]

RUN dotnet restore "src/workers/SatellitePortfolio.MarketDataWorker/SatellitePortfolio.MarketDataWorker.csproj"

COPY . .
RUN dotnet publish "src/workers/SatellitePortfolio.MarketDataWorker/SatellitePortfolio.MarketDataWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SatellitePortfolio.MarketDataWorker.dll"]
