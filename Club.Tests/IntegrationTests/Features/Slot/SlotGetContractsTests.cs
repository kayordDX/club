using Club.Data;
using Club.Entities;
using Club.Features.Slot.GetContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Features.Slot;

[Collection("AppFixture collection")]
public class SlotGetContractsTests(AppFixture app)
{
    [Fact, Priority(1)]
    public async Task GetContracts_WithNoUserContract_ReturnsOnlyPublicContracts()
    {
        // Arrange
        var (slotId, publicSlotContractId, memberSlotContractId, _) = await SeedSlotWithContracts();

        // Act
        var (rsp, result) = await app.Client.GETAsync<Endpoint, SlotGetContractsRequest, List<SlotGetContractsResponse>>(
            new SlotGetContractsRequest { Id = slotId }
        );

        // Assert
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.Select(sc => sc.Id).ShouldNotContain(memberSlotContractId);
        result.Select(sc => sc.Id).ShouldContain(publicSlotContractId);
    }

    [Fact, Priority(2)]
    public async Task GetContracts_WithUserContractValidOnSlotDate_ReturnsPublicAndMemberContracts()
    {
        // Arrange
        var (slotId, publicSlotContractId, memberSlotContractId, memberContractId) = await SeedSlotWithContracts();
        await CreateUserContract(memberContractId, startOffsetDays: 0, endOffsetDays: 7);

        // Act
        var (rsp, result) = await app.Client.GETAsync<Endpoint, SlotGetContractsRequest, List<SlotGetContractsResponse>>(
            new SlotGetContractsRequest { Id = slotId }
        );

        // Assert
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.Select(sc => sc.Id).ShouldContain(publicSlotContractId);
        result.Select(sc => sc.Id).ShouldContain(memberSlotContractId);
    }

    [Fact, Priority(3)]
    public async Task GetContracts_WithUserContractExpiredBeforeSlotDate_ReturnsOnlyPublicContracts()
    {
        // Arrange
        var (slotId, publicSlotContractId, memberSlotContractId, memberContractId) = await SeedSlotWithContracts();
        await CreateUserContract(memberContractId, startOffsetDays: -7, endOffsetDays: -1);

        // Act
        var (rsp, result) = await app.Client.GETAsync<Endpoint, SlotGetContractsRequest, List<SlotGetContractsResponse>>(
            new SlotGetContractsRequest { Id = slotId }
        );

        // Assert
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.Select(sc => sc.Id).ShouldNotContain(memberSlotContractId);
        result.Select(sc => sc.Id).ShouldContain(publicSlotContractId);
    }

    [Fact, Priority(4)]
    public async Task GetContracts_WithUserContractStartingAfterSlotDate_ReturnsOnlyPublicContracts()
    {
        // Arrange
        var (slotId, publicSlotContractId, memberSlotContractId, memberContractId) = await SeedSlotWithContracts();
        await CreateUserContract(memberContractId, startOffsetDays: 2, endOffsetDays: 30);

        // Act
        var (rsp, result) = await app.Client.GETAsync<Endpoint, SlotGetContractsRequest, List<SlotGetContractsResponse>>(
            new SlotGetContractsRequest { Id = slotId }
        );

        // Assert
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.Select(sc => sc.Id).ShouldNotContain(memberSlotContractId);
        result.Select(sc => sc.Id).ShouldContain(publicSlotContractId);
    }

    [Fact, Priority(5)]
    public async Task GetContracts_WithInactiveUserContract_ReturnsOnlyPublicContracts()
    {
        // Arrange
        var (slotId, publicSlotContractId, memberSlotContractId, memberContractId) = await SeedSlotWithContracts();
        var userContract = await CreateUserContract(memberContractId, startOffsetDays: 0, endOffsetDays: 7);
        userContract.IsActive = false;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.UserContract.Update(userContract);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        // Act
        var (rsp, result) = await app.Client.GETAsync<Endpoint, SlotGetContractsRequest, List<SlotGetContractsResponse>>(
            new SlotGetContractsRequest { Id = slotId }
        );

        // Assert
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.Select(sc => sc.Id).ShouldNotContain(memberSlotContractId);
        result.Select(sc => sc.Id).ShouldContain(publicSlotContractId);
    }

    private async Task<(Guid SlotId, int PublicSlotContractId, int MemberSlotContractId, int MemberContractId)> SeedSlotWithContracts()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var outlet = await CreateOutlet(db);
        var facilityTypeId = await CreateFacilityType(db);

        var facility = new Facility
        {
            Name = $"Contracts Facility_{Guid.NewGuid()}",
            Outlet = outlet,
            OutletId = outlet.Id,
            FacilityTypeId = facilityTypeId,
            IsActive = true,
        };
        db.Facility.Add(facility);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        var slot = new Club.Entities.Slot
        {
            Id = Guid.NewGuid(),
            FacilityId = facility.Id,
            StartDatetime = DateTime.UtcNow.AddDays(1),
            EndDatetime = DateTime.UtcNow.AddDays(1).AddHours(1),
            MaxBookings = 4,
        };
        db.Slot.Add(slot);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        var publicContract = new Contract { Name = $"Public_{Guid.NewGuid()}", IsPublic = true };
        var memberContract = new Contract { Name = $"Member_{Guid.NewGuid()}", IsPublic = false };
        db.Contract.AddRange(publicContract, memberContract);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        var publicSlotContract = new SlotContract
        {
            SlotId = slot.Id,
            Slot = slot,
            ContractId = publicContract.Id,
            Contract = publicContract,
            Price = 150m,
            CanPayLater = false,
            Description = "Public 18 Holes",
        };
        var memberSlotContract = new SlotContract
        {
            SlotId = slot.Id,
            Slot = slot,
            ContractId = memberContract.Id,
            Contract = memberContract,
            Price = 100m,
            CanPayLater = false,
            Description = "Member 18 Holes",
        };
        db.SlotContract.AddRange(publicSlotContract, memberSlotContract);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        return (slot.Id, publicSlotContract.Id, memberSlotContract.Id, memberContract.Id);
    }

    private async Task<UserContract> CreateUserContract(int contractId, int startOffsetDays, int endOffsetDays)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.FirstAsync(u => u.Id == TestClaims.UserIdGuid, app.Context.CancellationToken);

        var userContract = new UserContract
        {
            ContractId = contractId,
            Contract = db.Contract.First(c => c.Id == contractId),
            StartDate = DateTime.UtcNow.AddDays(startOffsetDays),
            EndDate = DateTime.UtcNow.AddDays(endOffsetDays),
            Price = 100m,
            IsActive = true,
            UserId = TestClaims.UserIdGuid,
            User = user,
        };
        db.UserContract.Add(userContract);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        return userContract;
    }

    private async Task<int> CreateFacilityType(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("INSERT INTO facility_type (name) VALUES ({0}) ON CONFLICT DO NOTHING", $"FacilityType_{Guid.NewGuid()}");

        var facilityType = await db.Database.SqlQueryRaw<FacilityType>("SELECT id, name FROM facility_type ORDER BY id DESC LIMIT 1").FirstOrDefaultAsync();

        return facilityType?.Id ?? 1;
    }

    private async Task<Outlet> CreateOutlet(AppDbContext db)
    {
        var business = new Business { Name = $"Business_{Guid.NewGuid()}" };
        db.Business.Add(business);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        var outletType = new OutletType { Name = $"OutletType_{Guid.NewGuid()}" };
        db.OutletType.Add(outletType);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        var outlet = new Outlet
        {
            Name = $"Outlet_{Guid.NewGuid()}",
            Slug = $"outlet-{Guid.NewGuid()}",
            Business = business,
            BusinessId = business.Id,
            VatNumber = "00000000",
            DisplayName = "Test Outlet",
            OutletType = outletType,
            OutletTypeId = outletType.Id,
            IsActive = true,
        };
        db.Outlet.Add(outlet);
        await db.SaveChangesAsync(app.Context.CancellationToken);
        return outlet;
    }
}
