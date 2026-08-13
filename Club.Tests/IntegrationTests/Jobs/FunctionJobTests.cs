using Club.Common.Enums;
using Club.Data;
using Club.Entities;
using Club.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Jobs;

[Collection("AppFixture collection")]
public class FunctionJobTests(AppFixture app)
{
    [Fact]
    public async Task ClearExpiredBookings_ShouldExpirePendingBookingsOnly_AndReleaseTheirSlots()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var business = new Business { Name = $"Business_{Guid.NewGuid()}" };
        db.Business.Add(business);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        var outletType = new OutletType { Name = $"OutletType_{Guid.NewGuid()}" };
        db.OutletType.Add(outletType);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        var facilityTypeId = await CreateFacilityType(db);

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

        var facility = new Facility
        {
            Name = "Test Facility",
            Outlet = outlet,
            OutletId = outlet.Id,
            FacilityTypeId = facilityTypeId,
            IsActive = true,
        };
        db.Facility.Add(facility);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        var slot = new Slot
        {
            Id = Guid.NewGuid(),
            FacilityId = facility.Id,
            StartDatetime = DateTime.UtcNow.AddHours(1),
            EndDatetime = DateTime.UtcNow.AddHours(2),
            MaxBookings = 2,
        };
        db.Slot.Add(slot);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        var contract = new Contract { Name = $"Contract_{Guid.NewGuid()}" };
        db.Contract.Add(contract);
        db.ContractFacility.Add(new ContractFacility { Contract = contract, Facility = facility });
        await db.SaveChangesAsync(app.Context.CancellationToken);

        var slotContract = new SlotContract
        {
            SlotId = slot.Id,
            ContractId = contract.Id,
            Price = 100,
        };
        db.SlotContract.Add(slotContract);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        var expiredPendingBooking = new Booking
        {
            BookingStatusId = (int)BookingStatusEnum.Pending,
            BookingStatusDate = DateTime.UtcNow.AddMinutes(-20),
            IsPaid = false,
            AmountOutstanding = 100,
            AmountPaid = 0,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10),
        };
        var confirmedBooking = new Booking
        {
            BookingStatusId = (int)BookingStatusEnum.Confirmed,
            BookingStatusDate = DateTime.UtcNow.AddMinutes(-20),
            IsPaid = true,
            AmountOutstanding = 0,
            AmountPaid = 100,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10),
        };
        db.Booking.AddRange(expiredPendingBooking, confirmedBooking);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        db.SlotContractBooking.AddRange(
            new SlotContractBooking
            {
                SlotContractId = slotContract.Id,
                BookingId = expiredPendingBooking.Id,
                Name = "Expired Pending Player",
            },
            new SlotContractBooking
            {
                SlotContractId = slotContract.Id,
                BookingId = confirmedBooking.Id,
                Name = "Confirmed Player",
            }
        );
        await db.SaveChangesAsync(app.Context.CancellationToken);

        var job = new FunctionJob(db);

        await job.ClearExpiredBookings(app.Context.CancellationToken);

        var updatedPendingBooking = await db.Booking.AsNoTracking().SingleAsync(x => x.Id == expiredPendingBooking.Id, app.Context.CancellationToken);
        var updatedConfirmedBooking = await db.Booking.AsNoTracking().SingleAsync(x => x.Id == confirmedBooking.Id, app.Context.CancellationToken);
        var remainingSlotBookings = await db
            .SlotContractBooking.AsNoTracking()
            .Where(x => x.BookingId == expiredPendingBooking.Id || x.BookingId == confirmedBooking.Id)
            .OrderBy(x => x.BookingId)
            .ToListAsync(app.Context.CancellationToken);

        updatedPendingBooking.BookingStatusId.ShouldBe((int)BookingStatusEnum.Expired);
        updatedConfirmedBooking.BookingStatusId.ShouldBe((int)BookingStatusEnum.Confirmed);
        remainingSlotBookings.Count.ShouldBe(1);
        remainingSlotBookings[0].BookingId.ShouldBe(confirmedBooking.Id);
    }

    private static async Task<int> CreateFacilityType(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("INSERT INTO facility_type (name) VALUES ({0}) ON CONFLICT DO NOTHING", $"FacilityType_{Guid.NewGuid()}");

        var facilityType = await db.Database.SqlQueryRaw<FacilityType>("SELECT id, name FROM facility_type ORDER BY id DESC LIMIT 1").FirstOrDefaultAsync();

        return facilityType?.Id ?? 1;
    }

    private class FacilityType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
