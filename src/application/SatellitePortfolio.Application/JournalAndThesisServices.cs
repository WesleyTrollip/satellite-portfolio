using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Application;

public sealed record CreateJournalEntryRequest(
    DateTime OccurredAt,
    string Title,
    string Body,
    string? Tags,
    IReadOnlyCollection<Guid>? ThesisIds,
    IReadOnlyCollection<Guid>? InstrumentIds);

public sealed record UpdateJournalEntryRequest(
    DateTime OccurredAt,
    string Title,
    string Body,
    string? Tags,
    IReadOnlyCollection<Guid>? ThesisIds,
    IReadOnlyCollection<Guid>? InstrumentIds);

public sealed record CreateThesisRequest(
    string Title,
    string Body,
    ThesisStatus Status,
    Guid? InstrumentId);

public sealed record UpdateThesisRequest(
    string Title,
    string Body,
    ThesisStatus Status,
    Guid? InstrumentId);

public sealed record JournalEntryWithLinks(
    JournalEntry Entry,
    IReadOnlyCollection<Guid> ThesisIds,
    IReadOnlyCollection<Guid> InstrumentIds);

public sealed class JournalService(
    IJournalRepository journalRepository,
    IJournalLinkRepository journalLinkRepository,
    IThesisRepository thesisRepository,
    IInstrumentRepository instrumentRepository,
    IPortfolioUnitOfWork unitOfWork)
{
    private static readonly PortfolioId LocalPortfolioId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    public async Task<IReadOnlyCollection<JournalEntryWithLinks>> ListAsync(CancellationToken cancellationToken)
    {
        var entries = await journalRepository.ListAsync(cancellationToken);
        var result = new List<JournalEntryWithLinks>(entries.Count);

        foreach (var entry in entries.OrderByDescending(x => x.OccurredAt))
        {
            var thesisLinks = await journalLinkRepository.ListThesisLinksAsync(entry.Id, cancellationToken);
            var instrumentLinks = await journalLinkRepository.ListInstrumentLinksAsync(entry.Id, cancellationToken);
            result.Add(new JournalEntryWithLinks(
                entry,
                thesisLinks.Select(x => x.ThesisId.Value).ToList(),
                instrumentLinks.Select(x => x.InstrumentId.Value).ToList()));
        }

        return result;
    }

    public async Task<JournalEntryWithLinks?> GetByIdAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken)
    {
        var entry = await journalRepository.GetByIdAsync(journalEntryId, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        var thesisLinks = await journalLinkRepository.ListThesisLinksAsync(entry.Id, cancellationToken);
        var instrumentLinks = await journalLinkRepository.ListInstrumentLinksAsync(entry.Id, cancellationToken);
        return new JournalEntryWithLinks(
            entry,
            thesisLinks.Select(x => x.ThesisId.Value).ToList(),
            instrumentLinks.Select(x => x.InstrumentId.Value).ToList());
    }

    public async Task<JournalEntryWithLinks> CreateAsync(CreateJournalEntryRequest request, CancellationToken cancellationToken)
    {
        ValidateEntryRequest(request.Title, request.Body);
        await ValidateLinkTargets(request.ThesisIds, request.InstrumentIds, cancellationToken);

        var entry = new JournalEntry
        {
            Id = new JournalEntryId(Guid.NewGuid()),
            PortfolioId = LocalPortfolioId,
            CreatedAt = DateTime.UtcNow,
            OccurredAt = request.OccurredAt,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            Tags = request.Tags?.Trim()
        };

        await journalRepository.AddAsync(entry, cancellationToken);
        await ReplaceLinksAsync(entry.Id, request.ThesisIds, request.InstrumentIds, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(entry.Id, cancellationToken))!;
    }

    public async Task<JournalEntryWithLinks?> UpdateAsync(JournalEntryId journalEntryId, UpdateJournalEntryRequest request, CancellationToken cancellationToken)
    {
        ValidateEntryRequest(request.Title, request.Body);
        await ValidateLinkTargets(request.ThesisIds, request.InstrumentIds, cancellationToken);

        var existing = await journalRepository.GetByIdAsync(journalEntryId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var updated = new JournalEntry
        {
            Id = existing.Id,
            PortfolioId = existing.PortfolioId,
            CreatedAt = existing.CreatedAt,
            OccurredAt = request.OccurredAt,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            Tags = request.Tags?.Trim()
        };

        await journalRepository.UpdateAsync(updated, cancellationToken);
        await ReplaceLinksAsync(updated.Id, request.ThesisIds, request.InstrumentIds, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(updated.Id, cancellationToken);
    }

    private async Task ReplaceLinksAsync(
        JournalEntryId journalEntryId,
        IReadOnlyCollection<Guid>? thesisIds,
        IReadOnlyCollection<Guid>? instrumentIds,
        CancellationToken cancellationToken)
    {
        await journalLinkRepository.RemoveThesisLinksAsync(journalEntryId, cancellationToken);
        await journalLinkRepository.RemoveInstrumentLinksAsync(journalEntryId, cancellationToken);

        var thesisLinks = (thesisIds ?? [])
            .Distinct()
            .Select(id => new JournalEntryThesisLink
            {
                JournalEntryId = journalEntryId,
                ThesisId = new ThesisId(id)
            })
            .ToList();

        var instrumentLinks = (instrumentIds ?? [])
            .Distinct()
            .Select(id => new JournalEntryInstrumentLink
            {
                JournalEntryId = journalEntryId,
                InstrumentId = new InstrumentId(id)
            })
            .ToList();

        if (thesisLinks.Count > 0)
        {
            await journalLinkRepository.AddThesisLinksAsync(thesisLinks, cancellationToken);
        }

        if (instrumentLinks.Count > 0)
        {
            await journalLinkRepository.AddInstrumentLinksAsync(instrumentLinks, cancellationToken);
        }
    }

    private async Task ValidateLinkTargets(
        IReadOnlyCollection<Guid>? thesisIds,
        IReadOnlyCollection<Guid>? instrumentIds,
        CancellationToken cancellationToken)
    {
        if (thesisIds is not null)
        {
            foreach (var thesisId in thesisIds.Distinct())
            {
                var thesis = await thesisRepository.GetByIdAsync(new ThesisId(thesisId), cancellationToken)
                             ?? throw new InvalidOperationException($"Thesis '{thesisId}' was not found.");

                if (thesis.PortfolioId != LocalPortfolioId)
                {
                    throw new InvalidOperationException("Thesis must belong to the active portfolio.");
                }
            }
        }

        if (instrumentIds is not null)
        {
            foreach (var instrumentId in instrumentIds.Distinct())
            {
                var instrument = await instrumentRepository.GetByIdAsync(new InstrumentId(instrumentId), cancellationToken);
                if (instrument is null)
                {
                    throw new InvalidOperationException($"Instrument '{instrumentId}' was not found.");
                }
            }
        }
    }

    private static void ValidateEntryRequest(string title, string body)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Journal title is required.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException("Journal body is required.");
        }
    }
}

public sealed class ThesisService(
    IThesisRepository thesisRepository,
    IInstrumentRepository instrumentRepository,
    IPortfolioUnitOfWork unitOfWork)
{
    private static readonly PortfolioId LocalPortfolioId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    public Task<IReadOnlyCollection<InvestmentThesis>> ListAsync(CancellationToken cancellationToken)
        => thesisRepository.ListAsync(cancellationToken);

    public Task<InvestmentThesis?> GetByIdAsync(ThesisId thesisId, CancellationToken cancellationToken)
        => thesisRepository.GetByIdAsync(thesisId, cancellationToken);

    public async Task<InvestmentThesis> CreateAsync(CreateThesisRequest request, CancellationToken cancellationToken)
    {
        await ValidateInstrumentLink(request.InstrumentId, cancellationToken);
        ValidateThesisRequest(request.Title, request.Body);

        var thesis = new InvestmentThesis
        {
            Id = new ThesisId(Guid.NewGuid()),
            PortfolioId = LocalPortfolioId,
            InstrumentId = request.InstrumentId.HasValue ? new InstrumentId(request.InstrumentId.Value) : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            Status = request.Status
        };

        await thesisRepository.AddAsync(thesis, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return thesis;
    }

    public async Task<InvestmentThesis?> UpdateAsync(ThesisId thesisId, UpdateThesisRequest request, CancellationToken cancellationToken)
    {
        await ValidateInstrumentLink(request.InstrumentId, cancellationToken);
        ValidateThesisRequest(request.Title, request.Body);

        var existing = await thesisRepository.GetByIdAsync(thesisId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        if (existing.PortfolioId != LocalPortfolioId)
        {
            throw new InvalidOperationException("Thesis must belong to the active portfolio.");
        }

        var updated = new InvestmentThesis
        {
            Id = existing.Id,
            PortfolioId = existing.PortfolioId,
            InstrumentId = request.InstrumentId.HasValue ? new InstrumentId(request.InstrumentId.Value) : null,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            Status = request.Status
        };

        await thesisRepository.UpdateAsync(updated, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return updated;
    }

    private async Task ValidateInstrumentLink(Guid? instrumentId, CancellationToken cancellationToken)
    {
        if (!instrumentId.HasValue)
        {
            return;
        }

        var instrument = await instrumentRepository.GetByIdAsync(new InstrumentId(instrumentId.Value), cancellationToken);
        if (instrument is null)
        {
            throw new InvalidOperationException($"Instrument '{instrumentId.Value}' was not found.");
        }
    }

    private static void ValidateThesisRequest(string title, string body)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Thesis title is required.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException("Thesis body is required.");
        }
    }
}

