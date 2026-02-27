using Online.Features.Slot.Get;
using Online.Entities;
using Online.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Features.Slot;

[Collection("AppFixture collection")]
public class GetSlotsByFacilityAndDateTests(AppFixture app)
{
    [Fact, Priority(1)]
    public async Task GetSlots_WithNoSlotsForFacility_ShouldReturnEmptyList()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var request = new Request
        {
            FacilityId = 999, // Non-existent facility
            Date = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var (rsp, result) = await app.Client.GETAsync<Endpoint, Request, List<Response>>(request);

        // Assert
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        rsp.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact, Priority(2)]
    public async Task GetSlots_WithValidFacilityAndDate_ShouldReturnSlots()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create test data
        var facilityTypeId = await CreateFacilityType(db);
        var outlet = await CreateOutlet(db);

        var facility = new Facility
        {
            Name = "Test Facility",
            Outlet = outlet,
            OutletId = outlet.Id,
            FacilityTypeId = facilityTypeId,
            IsActive = true
        };
        db.Facility.Add(facility);
        await db.SaveChangesAsync();

        var today = DateTime.UtcNow.Date;
        var slotStartTime = new DateTime(today.Year, today.Month, today.Day, 10, 0, 0, DateTimeKind.Utc);
        var slotEndTime = slotStartTime.AddHours(1);

        var request = new Request
        {
            FacilityId = facility.Id,
            Date = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, DateTimeKind.Utc)
        };

        var slot = new Online.Entities.Slot
        {
            Id = Guid.NewGuid(),
            FacilityId = facility.Id,
            StartDatetime = slotStartTime,
            EndDatetime = slotEndTime,
            GroupId = Guid.NewGuid()
        };
        db.Slot.Add(slot);
        await db.SaveChangesAsync();



        // Act
        var (rsp, result) = await app.Client.GETAsync<Endpoint, Request, List<Response>>(request);

        // Assert
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.ShouldHaveSingleItem();
        var returnedSlot = result.First();
        returnedSlot.Id.ShouldBe(slot.Id);
        returnedSlot.FacilityId.ShouldBe(facility.Id);
        returnedSlot.StartDatetime.ShouldBe(slotStartTime);
        returnedSlot.EndDatetime.ShouldBe(slotEndTime);
    }

    [Fact, Priority(3)]
    public async Task GetSlots_WithDifferentDateRange_ShouldFilterCorrectly()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var facilityTypeId = await CreateFacilityType(db);
        var outlet = await CreateOutlet(db);

        var facility = new Facility
        {
            Name = "Filter Test Facility",
            Outlet = outlet,
            OutletId = outlet.Id,
            FacilityTypeId = facilityTypeId,
            IsActive = true
        };
        db.Facility.Add(facility);
        await db.SaveChangesAsync();

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        // Create slots for different dates
        var slotToday = new Online.Entities.Slot
        {
            Id = Guid.NewGuid(),
            FacilityId = facility.Id,
            StartDatetime = new DateTime(today.Year, today.Month, today.Day, 10, 0, 0, DateTimeKind.Utc),
            EndDatetime = new DateTime(today.Year, today.Month, today.Day, 11, 0, 0, DateTimeKind.Utc),
            GroupId = Guid.NewGuid()
        };

        var slotTomorrow = new Online.Entities.Slot
        {
            Id = Guid.NewGuid(),
            FacilityId = facility.Id,
            StartDatetime = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 10, 0, 0, DateTimeKind.Utc),
            EndDatetime = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 11, 0, 0, DateTimeKind.Utc),
            GroupId = Guid.NewGuid()
        };

        db.Slot.AddRange(slotToday, slotTomorrow);
        await db.SaveChangesAsync();

        var requestToday = new Request
        {
            FacilityId = facility.Id,
            Date = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act - Query for today's slots
        var (rspToday, resultToday) = await app.Client.GETAsync<Endpoint, Request, List<Response>>(requestToday);

        // Assert
        rspToday.IsSuccessStatusCode.ShouldBeTrue();
        resultToday.ShouldHaveSingleItem();
        resultToday.First().Id.ShouldBe(slotToday.Id);

        // Act - Query for tomorrow's slots
        var requestTomorrow = new Request
        {
            FacilityId = facility.Id,
            Date = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 0, 0, 0, DateTimeKind.Utc)
        };
        var (rspTomorrow, resultTomorrow) = await app.Client.GETAsync<Endpoint, Request, List<Response>>(requestTomorrow);

        // Assert
        rspTomorrow.IsSuccessStatusCode.ShouldBeTrue();
        resultTomorrow.ShouldHaveSingleItem();
        resultTomorrow.First().Id.ShouldBe(slotTomorrow.Id);
    }

    [Fact, Priority(4)]
    public async Task GetSlots_WithMultipleSlotsForFacility_ShouldReturnAllOrderedByStartTime()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var facilityTypeId = await CreateFacilityType(db);
        var outlet = await CreateOutlet(db);

        var facility = new Facility
        {
            Name = "Multiple Slots Facility",
            Outlet = outlet,
            OutletId = outlet.Id,
            FacilityTypeId = facilityTypeId,
            IsActive = true
        };
        db.Facility.Add(facility);
        await db.SaveChangesAsync();

        var today = DateTime.UtcNow.Date;

        // Create multiple slots for the same day
        var slot1 = new Online.Entities.Slot
        {
            Id = Guid.NewGuid(),
            FacilityId = facility.Id,
            StartDatetime = new DateTime(today.Year, today.Month, today.Day, 10, 0, 0, DateTimeKind.Utc),
            EndDatetime = new DateTime(today.Year, today.Month, today.Day, 11, 0, 0, DateTimeKind.Utc),
            GroupId = Guid.NewGuid()
        };

        var slot2 = new Online.Entities.Slot
        {
            Id = Guid.NewGuid(),
            FacilityId = facility.Id,
            StartDatetime = new DateTime(today.Year, today.Month, today.Day, 14, 0, 0, DateTimeKind.Utc),
            EndDatetime = new DateTime(today.Year, today.Month, today.Day, 15, 0, 0, DateTimeKind.Utc),
            GroupId = Guid.NewGuid()
        };

        var slot3 = new Online.Entities.Slot
        {
            Id = Guid.NewGuid(),
            FacilityId = facility.Id,
            StartDatetime = new DateTime(today.Year, today.Month, today.Day, 12, 0, 0, DateTimeKind.Utc),
            EndDatetime = new DateTime(today.Year, today.Month, today.Day, 13, 0, 0, DateTimeKind.Utc),
            GroupId = Guid.NewGuid()
        };

        db.Slot.AddRange(slot1, slot2, slot3);
        await db.SaveChangesAsync();

        var request = new Request
        {
            FacilityId = facility.Id,
            Date = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var (rsp, result) = await app.Client.GETAsync<Endpoint, Request, List<Response>>(request);

        // Assert
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        result.Count.ShouldBe(3);
        result[0].StartDatetime.Hour.ShouldBe(10);
        result[1].StartDatetime.Hour.ShouldBe(12);
        result[2].StartDatetime.Hour.ShouldBe(14);
    }

    [Fact, Priority(5)]
    public async Task GetSlots_WithResourceAssociation_ShouldIncludeResourceInfo()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var facilityTypeId = await CreateFacilityType(db);
        var outlet = await CreateOutlet(db);

        var facility = new Facility
        {
            Name = "Resource Test Facility",
            Outlet = outlet,
            OutletId = outlet.Id,
            FacilityTypeId = facilityTypeId,
            IsActive = true
        };
        db.Facility.Add(facility);
        await db.SaveChangesAsync();

        var resource = new Resource
        {
            Name = "Test Resource",
            Facility = facility,
            FacilityId = facility.Id,
            IsActive = true
        };
        db.Resource.Add(resource);
        await db.SaveChangesAsync();

        var today = DateTime.UtcNow.Date;
        var slot = new Online.Entities.Slot
        {
            Id = Guid.NewGuid(),
            FacilityId = facility.Id,
            ResourceId = resource.Id,
            StartDatetime = new DateTime(today.Year, today.Month, today.Day, 10, 0, 0, DateTimeKind.Utc),
            EndDatetime = new DateTime(today.Year, today.Month, today.Day, 11, 0, 0, DateTimeKind.Utc),
            GroupId = Guid.NewGuid()
        };
        db.Slot.Add(slot);
        await db.SaveChangesAsync();

        var request = new Request
        {
            FacilityId = facility.Id,
            Date = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var (rsp, result) = await app.Client.GETAsync<Endpoint, Request, List<Response>>(request);

        // Assert
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        result.ShouldHaveSingleItem();
        var returnedSlot = result.First();
        returnedSlot.ResourceId.ShouldBe(resource.Id);
        returnedSlot.ResourceName.ShouldBe("Test Resource");
    }

    [Fact, Priority(6)]
    public async Task GetSlots_WithInvalidFacilityId_ShouldReturnEmptyList()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var request = new Request
        {
            FacilityId = 999999, // Non-existent facility ID
            Date = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var (rsp, result) = await app.Client.GETAsync<Endpoint, Request, List<Response>>(request);

        // Assert
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact, Priority(7)]
    public async Task GetSlots_WithSlotsFromDifferentFacilities_ShouldOnlyReturnRequestedFacility()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var facilityTypeId = await CreateFacilityType(db);
        var outlet = await CreateOutlet(db);

        var facility1 = new Facility
        {
            Name = "Facility 1",
            Outlet = outlet,
            OutletId = outlet.Id,
            FacilityTypeId = facilityTypeId,
            IsActive = true
        };

        var facility2 = new Facility
        {
            Name = "Facility 2",
            Outlet = outlet,
            OutletId = outlet.Id,
            FacilityTypeId = facilityTypeId,
            IsActive = true
        };

        db.Facility.AddRange(facility1, facility2);
        await db.SaveChangesAsync();

        var today = DateTime.UtcNow.Date;

        var slot1 = new Online.Entities.Slot
        {
            Id = Guid.NewGuid(),
            FacilityId = facility1.Id,
            StartDatetime = new DateTime(today.Year, today.Month, today.Day, 10, 0, 0, DateTimeKind.Utc),
            EndDatetime = new DateTime(today.Year, today.Month, today.Day, 11, 0, 0, DateTimeKind.Utc),
            GroupId = Guid.NewGuid()
        };

        var slot2 = new Online.Entities.Slot
        {
            Id = Guid.NewGuid(),
            FacilityId = facility2.Id,
            StartDatetime = new DateTime(today.Year, today.Month, today.Day, 10, 0, 0, DateTimeKind.Utc),
            EndDatetime = new DateTime(today.Year, today.Month, today.Day, 11, 0, 0, DateTimeKind.Utc),
            GroupId = Guid.NewGuid()
        };

        db.Slot.AddRange(slot1, slot2);
        await db.SaveChangesAsync();

        var request = new Request
        {
            FacilityId = facility1.Id,
            Date = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var (rsp, result) = await app.Client.GETAsync<Endpoint, Request, List<Response>>(request);

        // Assert
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        result.ShouldHaveSingleItem();
        result.First().FacilityId.ShouldBe(facility1.Id);
        result.First().Id.ShouldBe(slot1.Id);
    }

    // Helper methods
    private async Task<int> CreateFacilityType(AppDbContext db)
    {
        // Use raw SQL to insert facility type since it's not exposed as DbSet
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO facility_type (name) VALUES ({0}) ON CONFLICT DO NOTHING",
            $"FacilityType_{Guid.NewGuid()}"
        );

        var facilityType = await db.Database.SqlQueryRaw<FacilityType>(
            "SELECT id, name FROM facility_type ORDER BY id DESC LIMIT 1"
        ).FirstOrDefaultAsync();

        return facilityType?.Id ?? 1;
    }

    private async Task<Outlet> CreateOutlet(AppDbContext db)
    {
        var business = new Business { Name = $"Business_{Guid.NewGuid()}" };
        db.Business.Add(business);
        await db.SaveChangesAsync();

        var outletType = new OutletType { Name = $"OutletType_{Guid.NewGuid()}" };
        db.OutletType.Add(outletType);
        await db.SaveChangesAsync();

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
            IsActive = true
        };
        db.Outlet.Add(outlet);
        await db.SaveChangesAsync();
        return outlet;
    }
}
