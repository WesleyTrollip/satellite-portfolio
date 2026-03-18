using SatellitePortfolio.Application;

namespace SatellitePortfolio.Api;

public sealed class LocalUserContext : IUserContext
{
    // For the MVP we use a fixed local user identifier.
    public string UserId => "local-user";
}

