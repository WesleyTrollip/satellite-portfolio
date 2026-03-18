using SatellitePortfolio.Application;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Application.Tests;

public class JournalAndThesisServiceTests
{
    [Fact]
    public async Task ThesisLinking_RequiresValidInstrument()
    {
        var thesisService = new ThesisService(
            new InMemoryThesisRepository(),
            new InMemoryInstrumentRepository(),
            new InMemoryUnitOfWork());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            thesisService.CreateAsync(
                new CreateThesisRequest("Title", "Body", ThesisStatus.Active, Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task JournalLinking_RejectsThesisFromOtherPortfolio()
    {
        var thesisRepository = new InMemoryThesisRepository();
        thesisRepository.Items.Add(new InvestmentThesis
        {
            Id = new ThesisId(Guid.NewGuid()),
            PortfolioId = new PortfolioId(Guid.NewGuid()),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Title = "Foreign thesis",
            Body = "Body",
            Status = ThesisStatus.Active
        });

        var service = new JournalService(
            new InMemoryJournalRepository(),
            new InMemoryJournalLinkRepository(),
            thesisRepository,
            new InMemoryInstrumentRepository(),
            new InMemoryUnitOfWork());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(
                new CreateJournalEntryRequest(
                    DateTime.UtcNow,
                    "Entry",
                    "Body",
                    null,
                    [thesisRepository.Items[0].Id.Value],
                    []),
                CancellationToken.None));
    }

}

internal sealed class InMemoryJournalRepository : IJournalRepository
{
    public List<JournalEntry> Items { get; } = [];

    public Task<IReadOnlyCollection<JournalEntry>> ListAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<JournalEntry>>(Items.ToList());

    public Task<JournalEntry?> GetByIdAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken)
        => Task.FromResult(Items.SingleOrDefault(x => x.Id == journalEntryId));

    public Task AddAsync(JournalEntry journalEntry, CancellationToken cancellationToken)
    {
        Items.Add(journalEntry);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(JournalEntry journalEntry, CancellationToken cancellationToken)
    {
        var index = Items.FindIndex(x => x.Id == journalEntry.Id);
        if (index >= 0)
        {
            Items[index] = journalEntry;
        }

        return Task.CompletedTask;
    }
}

internal sealed class InMemoryThesisRepository : IThesisRepository
{
    public List<InvestmentThesis> Items { get; } = [];

    public Task<IReadOnlyCollection<InvestmentThesis>> ListAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<InvestmentThesis>>(Items.ToList());

    public Task<InvestmentThesis?> GetByIdAsync(ThesisId thesisId, CancellationToken cancellationToken)
        => Task.FromResult(Items.SingleOrDefault(x => x.Id == thesisId));

    public Task AddAsync(InvestmentThesis thesis, CancellationToken cancellationToken)
    {
        Items.Add(thesis);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(InvestmentThesis thesis, CancellationToken cancellationToken)
    {
        var index = Items.FindIndex(x => x.Id == thesis.Id);
        if (index >= 0)
        {
            Items[index] = thesis;
        }

        return Task.CompletedTask;
    }
}

internal sealed class InMemoryJournalLinkRepository : IJournalLinkRepository
{
    private readonly List<JournalEntryThesisLink> _thesisLinks = [];
    private readonly List<JournalEntryInstrumentLink> _instrumentLinks = [];

    public Task<IReadOnlyCollection<JournalEntryThesisLink>> ListThesisLinksAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<JournalEntryThesisLink>>(_thesisLinks.Where(x => x.JournalEntryId == journalEntryId).ToList());

    public Task<IReadOnlyCollection<JournalEntryInstrumentLink>> ListInstrumentLinksAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<JournalEntryInstrumentLink>>(_instrumentLinks.Where(x => x.JournalEntryId == journalEntryId).ToList());

    public Task AddThesisLinksAsync(IEnumerable<JournalEntryThesisLink> links, CancellationToken cancellationToken)
    {
        _thesisLinks.AddRange(links);
        return Task.CompletedTask;
    }

    public Task AddInstrumentLinksAsync(IEnumerable<JournalEntryInstrumentLink> links, CancellationToken cancellationToken)
    {
        _instrumentLinks.AddRange(links);
        return Task.CompletedTask;
    }

    public Task RemoveThesisLinksAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken)
    {
        _thesisLinks.RemoveAll(x => x.JournalEntryId == journalEntryId);
        return Task.CompletedTask;
    }

    public Task RemoveInstrumentLinksAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken)
    {
        _instrumentLinks.RemoveAll(x => x.JournalEntryId == journalEntryId);
        return Task.CompletedTask;
    }
}

