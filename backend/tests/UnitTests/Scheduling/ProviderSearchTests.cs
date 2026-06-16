using Microsoft.EntityFrameworkCore;
using Moq;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Queries.GetProviderSlots;
using Scheduling.Application.Queries.SearchProviders;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;
using SharedKernel.Caching;
using SharedKernel.Domain;

namespace UnitTests.Scheduling;

public class SearchProvidersQueryHandlerTests : IDisposable
{
    private readonly TestSchedulingDbContext _dbContext;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly SearchProvidersQueryHandler _handler;

    public SearchProvidersQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestSchedulingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestSchedulingDbContext(options);
        _cacheMock = new Mock<ICacheService>();
        _handler = new SearchProvidersQueryHandler(_dbContext, _cacheMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ReturnsProviders_WhenMatchingByName()
    {
        SeedProviderWithSlots("Dr. Smith", "Cardiology", "Downtown");

        var query = new SearchProvidersQuery("Smith", null, null, DateTime.UtcNow.Date, 1, 20, null);
        var result = await _handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Providers);
        Assert.Equal("Dr. Smith", result.Value.Providers[0].FullName);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmpty_WhenNoMatchingProviders()
    {
        SeedProviderWithSlots("Dr. Smith", "Cardiology", "Downtown");

        var query = new SearchProvidersQuery("Jones", null, null, DateTime.UtcNow.Date, 1, 20, null);
        var result = await _handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Providers);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_FiltersBy_Specialty()
    {
        SeedProviderWithSlots("Dr. A", "Cardiology", "Downtown");
        SeedProviderWithSlots("Dr. B", "Dermatology", "Downtown");

        var query = new SearchProvidersQuery(null, "Cardiology", null, DateTime.UtcNow.Date, 1, 20, null);
        var result = await _handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Providers);
        Assert.Equal("Dr. A", result.Value.Providers[0].FullName);
    }

    [Fact]
    public async Task HandleAsync_FiltersBy_Location()
    {
        SeedProviderWithSlots("Dr. A", "Cardiology", "Downtown");
        SeedProviderWithSlots("Dr. B", "Cardiology", "North");

        var query = new SearchProvidersQuery(null, null, "North", DateTime.UtcNow.Date, 1, 20, null);
        var result = await _handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Providers);
        Assert.Equal("Dr. B", result.Value.Providers[0].FullName);
    }

    [Fact]
    public async Task HandleAsync_ExcludesInactiveProviders()
    {
        var provider = SeedProviderWithSlots("Dr. Inactive", "Cardiology", "Downtown");
        provider.IsActive = false;
        await _dbContext.SaveChangesAsync();

        var query = new SearchProvidersQuery(null, null, null, DateTime.UtcNow.Date, 1, 20, null);
        var result = await _handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Providers);
    }

    [Fact]
    public async Task HandleAsync_ExcludesProviders_WithNoAvailableSlots()
    {
        var provider = new Provider { Name = "Dr. Busy", Specialty = "Cardiology", Location = "Downtown" };
        _dbContext.Set<Provider>().Add(provider);
        _dbContext.Set<AppointmentSlot>().Add(new AppointmentSlot
        {
            ProviderId = provider.Id,
            StartTime = DateTime.UtcNow.Date.AddHours(10),
            Status = SlotStatus.Booked
        });
        await _dbContext.SaveChangesAsync();

        var query = new SearchProvidersQuery(null, null, null, DateTime.UtcNow.Date, 1, 20, null);
        var result = await _handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Providers);
    }

    [Fact]
    public async Task HandleAsync_ReturnsCachedResult_WhenAvailable()
    {
        var cached = new SearchProvidersResult(
            [new ProviderDto(Guid.NewGuid(), "Cached Dr.", "Cached", "Here", 5.0, true, [], null)],
            1, 1, 20);

        _cacheMock.Setup(c => c.GetAsync<SearchProvidersResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var query = new SearchProvidersQuery(null, null, null, DateTime.UtcNow.Date, 1, 20, null);
        var result = await _handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Equal("Cached Dr.", result.Value!.Providers[0].FullName);
    }

    [Fact]
    public async Task HandleAsync_CachesResult_OnCacheMiss()
    {
        SeedProviderWithSlots("Dr. Fresh", "Cardiology", "Downtown");

        _cacheMock.Setup(c => c.GetAsync<SearchProvidersResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SearchProvidersResult?)null);

        var query = new SearchProvidersQuery(null, null, null, DateTime.UtcNow.Date, 1, 20, null);
        await _handler.HandleAsync(query);

        _cacheMock.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<SearchProvidersResult>(),
            CacheTier.L1,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_PaginatesResults_Correctly()
    {
        for (var i = 0; i < 5; i++)
        {
            SeedProviderWithSlots($"Dr. Provider{i}", "Cardiology", "Downtown");
        }

        var query = new SearchProvidersQuery(null, null, null, DateTime.UtcNow.Date, 2, 2, "name");
        var result = await _handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.TotalCount);
        Assert.Equal(2, result.Value.Providers.Count);
        Assert.Equal(2, result.Value.Page);
    }

    [Fact]
    public async Task HandleAsync_ClampsPageSize_ToMax100()
    {
        SeedProviderWithSlots("Dr. One", "Cardiology", "Downtown");

        var query = new SearchProvidersQuery(null, null, null, DateTime.UtcNow.Date, 1, 500, null);
        var result = await _handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value!.PageSize);
    }

    [Fact]
    public async Task HandleAsync_ReturnsSlots_ForMatchingProviders()
    {
        SeedProviderWithSlots("Dr. Slotted", "Cardiology", "Downtown");

        var query = new SearchProvidersQuery("Slotted", null, null, DateTime.UtcNow.Date, 1, 20, null);
        var result = await _handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!.Providers[0].AvailableSlots);
        Assert.True(result.Value.Providers[0].AvailableSlots[0].IsAvailable);
    }

    private Provider SeedProviderWithSlots(string name, string specialty, string location)
    {
        var provider = new Provider { Name = name, Specialty = specialty, Location = location };
        _dbContext.Set<Provider>().Add(provider);
        _dbContext.Set<AppointmentSlot>().Add(new AppointmentSlot
        {
            ProviderId = provider.Id,
            StartTime = DateTime.UtcNow.Date.AddHours(10),
            Status = SlotStatus.Available
        });
        _dbContext.Set<AppointmentSlot>().Add(new AppointmentSlot
        {
            ProviderId = provider.Id,
            StartTime = DateTime.UtcNow.Date.AddHours(14),
            Status = SlotStatus.Available
        });
        _dbContext.SaveChanges();
        return provider;
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

public class GetProviderSlotsQueryHandlerTests : IDisposable
{
    private readonly TestSchedulingDbContext _dbContext;
    private readonly GetProviderSlotsQueryHandler _handler;

    public GetProviderSlotsQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestSchedulingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestSchedulingDbContext(options);
        _handler = new GetProviderSlotsQueryHandler(_dbContext);
    }

    [Fact]
    public async Task HandleAsync_ReturnsSlots_ForExistingProvider()
    {
        var provider = new Provider { Name = "Dr. Slots", Specialty = "Cardiology" };
        _dbContext.Set<Provider>().Add(provider);
        _dbContext.Set<AppointmentSlot>().AddRange(
            new AppointmentSlot { ProviderId = provider.Id, StartTime = DateTime.UtcNow.Date.AddHours(9), Status = SlotStatus.Available },
            new AppointmentSlot { ProviderId = provider.Id, StartTime = DateTime.UtcNow.Date.AddHours(10), Status = SlotStatus.Booked }
        );
        await _dbContext.SaveChangesAsync();

        var query = new GetProviderSlotsQuery(provider.Id, DateTime.UtcNow.Date);
        var result = await _handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Slots.Count);
        Assert.True(result.Value.Slots[0].IsAvailable);
        Assert.False(result.Value.Slots[1].IsAvailable);
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailure_ForNonexistentProvider()
    {
        var query = new GetProviderSlotsQuery(Guid.NewGuid(), DateTime.UtcNow.Date);
        var result = await _handler.HandleAsync(query);

        Assert.False(result.IsSuccess);
        Assert.Equal("Provider not found.", result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmpty_WhenNoSlotsForDate()
    {
        var provider = new Provider { Name = "Dr. Empty", Specialty = "Dermatology" };
        _dbContext.Set<Provider>().Add(provider);
        _dbContext.Set<AppointmentSlot>().Add(new AppointmentSlot
        {
            ProviderId = provider.Id,
            StartTime = DateTime.UtcNow.Date.AddDays(5).AddHours(9),
            Status = SlotStatus.Available
        });
        await _dbContext.SaveChangesAsync();

        var query = new GetProviderSlotsQuery(provider.Id, DateTime.UtcNow.Date);
        var result = await _handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Slots);
    }

    [Fact]
    public async Task HandleAsync_ExcludesInactiveProvider()
    {
        var provider = new Provider { Name = "Dr. Gone", Specialty = "Cardiology", IsActive = false };
        _dbContext.Set<Provider>().Add(provider);
        await _dbContext.SaveChangesAsync();

        var query = new GetProviderSlotsQuery(provider.Id, DateTime.UtcNow.Date);
        var result = await _handler.HandleAsync(query);

        Assert.False(result.IsSuccess);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

/// <summary>
/// In-memory test DbContext implementing ISchedulingDbContext for unit testing.
/// </summary>
internal sealed class TestSchedulingDbContext : DbContext, ISchedulingDbContext
{
    public TestSchedulingDbContext(DbContextOptions<TestSchedulingDbContext> options) : base(options) { }

    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<AppointmentSlot> AppointmentSlots => Set<AppointmentSlot>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<PreferredSlotSwap> PreferredSlotSwaps => Set<PreferredSlotSwap>();
    public DbSet<Waitlist> Waitlists => Set<Waitlist>();
    public DbSet<NoShowRiskScore> NoShowRiskScores => Set<NoShowRiskScore>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public void SetRowVersion(Appointment appointment, byte[] rowVersion)
    {
        Entry(appointment).Property(a => a.RowVersion).OriginalValue = rowVersion;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Provider>().HasKey(p => p.Id);
        modelBuilder.Entity<AppointmentSlot>().HasKey(s => s.Id);
        modelBuilder.Entity<Appointment>().HasKey(a => a.Id);
        modelBuilder.Entity<PreferredSlotSwap>().HasKey(s => s.Id);
        modelBuilder.Entity<Waitlist>().HasKey(w => w.Id);
        modelBuilder.Entity<NoShowRiskScore>().HasKey(r => r.Id);

        modelBuilder.Entity<Provider>()
            .HasMany(p => p.Slots)
            .WithOne(s => s.Provider)
            .HasForeignKey(s => s.ProviderId);

        modelBuilder.Entity<AppointmentSlot>()
            .Ignore(s => s.RowVersion);

        modelBuilder.Entity<PreferredSlotSwap>()
            .HasOne(s => s.OriginalAppointment)
            .WithMany()
            .HasForeignKey(s => s.OriginalAppointmentId);

        modelBuilder.Entity<PreferredSlotSwap>()
            .HasOne(s => s.DesiredSlot)
            .WithMany()
            .HasForeignKey(s => s.DesiredSlotId);

        modelBuilder.Entity<Waitlist>()
            .HasOne(w => w.Provider)
            .WithMany()
            .HasForeignKey(w => w.ProviderId);

        modelBuilder.Entity<AuditLog>().HasKey(a => a.Id);
    }
}
