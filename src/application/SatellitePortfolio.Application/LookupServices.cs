using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Application;

public sealed record LookupListRequest(string? Search, bool? IsActive, int Skip = 0, int Take = 100);
public sealed record UpsertLookupRequest(string Code, string Name, bool IsActive = true);
public sealed record LookupView(Guid Id, string Code, string Name, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt);

public sealed class SectorLookupService(
    ISectorLookupRepository sectors,
    IInstrumentRepository instruments,
    IPortfolioUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyCollection<LookupView>> ListAsync(LookupListRequest request, CancellationToken cancellationToken)
        => (await sectors.ListAsync(request.Search, request.IsActive, request.Skip, request.Take, cancellationToken))
            .Select(Map)
            .ToList();

    public async Task<LookupView?> GetByIdAsync(SectorLookupId id, CancellationToken cancellationToken)
        => MapOrNull(await sectors.GetByIdAsync(id, cancellationToken));

    public async Task<LookupView> CreateAsync(UpsertLookupRequest request, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(request.Code);
        var name = NormalizeName(request.Name);
        var existing = await sectors.GetByCodeAsync(code, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Sector code '{code}' already exists.");
        }

        var now = DateTime.UtcNow;
        var sector = new SectorLookup
        {
            Id = new SectorLookupId(Guid.NewGuid()),
            Code = code,
            Name = name,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        await sectors.AddAsync(sector, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(sector);
    }

    public async Task<LookupView?> UpdateAsync(SectorLookupId id, UpsertLookupRequest request, CancellationToken cancellationToken)
    {
        var existing = await sectors.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var code = NormalizeCode(request.Code);
        var duplicate = await sectors.GetByCodeAsync(code, cancellationToken);
        if (duplicate is not null && duplicate.Id != id)
        {
            throw new InvalidOperationException($"Sector code '{code}' already exists.");
        }

        var updated = new SectorLookup
        {
            Id = existing.Id,
            Code = code,
            Name = NormalizeName(request.Name),
            IsActive = request.IsActive,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        await sectors.UpdateAsync(updated, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(updated);
    }

    public async Task<LookupView?> DeactivateAsync(SectorLookupId id, CancellationToken cancellationToken)
    {
        var existing = await sectors.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var updated = new SectorLookup
        {
            Id = existing.Id,
            Code = existing.Code,
            Name = existing.Name,
            IsActive = false,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
        await sectors.UpdateAsync(updated, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(updated);
    }

    public async Task<bool> DeleteAsync(SectorLookupId id, CancellationToken cancellationToken)
    {
        var existing = await sectors.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        var hasReferences = (await instruments.ListAsync(cancellationToken)).Any(x => x.SectorLookupId == id);
        if (hasReferences)
        {
            throw new InvalidOperationException("Cannot delete sector lookup because it is referenced by instruments.");
        }

        await sectors.DeleteAsync(existing, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static LookupView Map(SectorLookup entity) => new(entity.Id.Value, entity.Code, entity.Name, entity.IsActive, entity.CreatedAt, entity.UpdatedAt);
    private static LookupView? MapOrNull(SectorLookup? entity) => entity is null ? null : Map(entity);
    private static string NormalizeCode(string value) => string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException("Lookup code is required.") : value.Trim().ToUpperInvariant();
    private static string NormalizeName(string value) => string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException("Lookup name is required.") : value.Trim();
}

public sealed class PriceSourceLookupService(
    IPriceSourceLookupRepository sources,
    IPriceSnapshotRepository snapshots,
    IPortfolioUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyCollection<LookupView>> ListAsync(LookupListRequest request, CancellationToken cancellationToken)
        => (await sources.ListAsync(request.Search, request.IsActive, request.Skip, request.Take, cancellationToken))
            .Select(Map)
            .ToList();

    public async Task<LookupView?> GetByIdAsync(PriceSourceLookupId id, CancellationToken cancellationToken)
        => MapOrNull(await sources.GetByIdAsync(id, cancellationToken));

    public async Task<LookupView> CreateAsync(UpsertLookupRequest request, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(request.Code);
        var existing = await sources.GetByCodeAsync(code, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Price source code '{code}' already exists.");
        }

        var now = DateTime.UtcNow;
        var source = new PriceSourceLookup
        {
            Id = new PriceSourceLookupId(Guid.NewGuid()),
            Code = code,
            Name = NormalizeName(request.Name),
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        await sources.AddAsync(source, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(source);
    }

    public async Task<LookupView?> UpdateAsync(PriceSourceLookupId id, UpsertLookupRequest request, CancellationToken cancellationToken)
    {
        var existing = await sources.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var code = NormalizeCode(request.Code);
        var duplicate = await sources.GetByCodeAsync(code, cancellationToken);
        if (duplicate is not null && duplicate.Id != id)
        {
            throw new InvalidOperationException($"Price source code '{code}' already exists.");
        }

        var updated = new PriceSourceLookup
        {
            Id = existing.Id,
            Code = code,
            Name = NormalizeName(request.Name),
            IsActive = request.IsActive,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        await sources.UpdateAsync(updated, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(updated);
    }

    public async Task<LookupView?> DeactivateAsync(PriceSourceLookupId id, CancellationToken cancellationToken)
    {
        var existing = await sources.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var updated = new PriceSourceLookup
        {
            Id = existing.Id,
            Code = existing.Code,
            Name = existing.Name,
            IsActive = false,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
        await sources.UpdateAsync(updated, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(updated);
    }

    public async Task<bool> DeleteAsync(PriceSourceLookupId id, CancellationToken cancellationToken)
    {
        var existing = await sources.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        var hasReferences = (await snapshots.ListAllAsync(cancellationToken)).Any(x => x.PriceSourceLookupId == id);
        if (hasReferences)
        {
            throw new InvalidOperationException("Cannot delete price source lookup because it is referenced by price snapshots.");
        }

        await sources.DeleteAsync(existing, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static LookupView Map(PriceSourceLookup entity) => new(entity.Id.Value, entity.Code, entity.Name, entity.IsActive, entity.CreatedAt, entity.UpdatedAt);
    private static LookupView? MapOrNull(PriceSourceLookup? entity) => entity is null ? null : Map(entity);
    private static string NormalizeCode(string value) => string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException("Lookup code is required.") : value.Trim().ToUpperInvariant();
    private static string NormalizeName(string value) => string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException("Lookup name is required.") : value.Trim();
}

public sealed class CorrectionReasonLookupService(
    ICorrectionReasonLookupRepository reasons,
    ITradeRepository trades,
    IPortfolioUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyCollection<LookupView>> ListAsync(LookupListRequest request, CancellationToken cancellationToken)
        => (await reasons.ListAsync(request.Search, request.IsActive, request.Skip, request.Take, cancellationToken))
            .Select(Map)
            .ToList();

    public async Task<LookupView?> GetByIdAsync(CorrectionReasonLookupId id, CancellationToken cancellationToken)
        => MapOrNull(await reasons.GetByIdAsync(id, cancellationToken));

    public async Task<LookupView> CreateAsync(UpsertLookupRequest request, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(request.Code);
        var existing = await reasons.GetByCodeAsync(code, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Correction reason code '{code}' already exists.");
        }

        var now = DateTime.UtcNow;
        var reason = new CorrectionReasonLookup
        {
            Id = new CorrectionReasonLookupId(Guid.NewGuid()),
            Code = code,
            Name = NormalizeName(request.Name),
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        await reasons.AddAsync(reason, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(reason);
    }

    public async Task<LookupView?> UpdateAsync(CorrectionReasonLookupId id, UpsertLookupRequest request, CancellationToken cancellationToken)
    {
        var existing = await reasons.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var code = NormalizeCode(request.Code);
        var duplicate = await reasons.GetByCodeAsync(code, cancellationToken);
        if (duplicate is not null && duplicate.Id != id)
        {
            throw new InvalidOperationException($"Correction reason code '{code}' already exists.");
        }

        var updated = new CorrectionReasonLookup
        {
            Id = existing.Id,
            Code = code,
            Name = NormalizeName(request.Name),
            IsActive = request.IsActive,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        await reasons.UpdateAsync(updated, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(updated);
    }

    public async Task<LookupView?> DeactivateAsync(CorrectionReasonLookupId id, CancellationToken cancellationToken)
    {
        var existing = await reasons.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var updated = new CorrectionReasonLookup
        {
            Id = existing.Id,
            Code = existing.Code,
            Name = existing.Name,
            IsActive = false,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
        await reasons.UpdateAsync(updated, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(updated);
    }

    public async Task<bool> DeleteAsync(CorrectionReasonLookupId id, CancellationToken cancellationToken)
    {
        var existing = await reasons.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        var hasReferences = (await trades.ListAllAsync(cancellationToken)).Any(x => x.CorrectionReasonLookupId == id);
        if (hasReferences)
        {
            throw new InvalidOperationException("Cannot delete correction reason lookup because it is referenced by trades.");
        }

        await reasons.DeleteAsync(existing, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static LookupView Map(CorrectionReasonLookup entity) => new(entity.Id.Value, entity.Code, entity.Name, entity.IsActive, entity.CreatedAt, entity.UpdatedAt);
    private static LookupView? MapOrNull(CorrectionReasonLookup? entity) => entity is null ? null : Map(entity);
    private static string NormalizeCode(string value) => string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException("Lookup code is required.") : value.Trim().ToUpperInvariant();
    private static string NormalizeName(string value) => string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException("Lookup name is required.") : value.Trim();
}
