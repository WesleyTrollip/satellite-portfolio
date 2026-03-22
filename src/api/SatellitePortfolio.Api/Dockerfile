FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/api/SatellitePortfolio.Api/SatellitePortfolio.Api.csproj", "src/api/SatellitePortfolio.Api/"]
COPY ["src/application/SatellitePortfolio.Application/SatellitePortfolio.Application.csproj", "src/application/SatellitePortfolio.Application/"]
COPY ["src/infrastructure/SatellitePortfolio.Infrastructure/SatellitePortfolio.Infrastructure.csproj", "src/infrastructure/SatellitePortfolio.Infrastructure/"]
COPY ["src/domain/SatellitePortfolio.Domain/SatellitePortfolio.Domain.csproj", "src/domain/SatellitePortfolio.Domain/"]

RUN dotnet restore "src/api/SatellitePortfolio.Api/SatellitePortfolio.Api.csproj"

COPY . .
RUN dotnet publish "src/api/SatellitePortfolio.Api/SatellitePortfolio.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SatellitePortfolio.Api.dll"]
