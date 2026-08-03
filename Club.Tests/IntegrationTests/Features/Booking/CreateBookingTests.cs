using Club.Data;
using Club.DTO;
using Club.Entities;
using Club.Features.Booking.Create;
using Club.Features.Booking.Get;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BookingCreateEndpoint = Club.Features.Booking.Create.Endpoint;
using BookingGetEndpoint = Club.Features.Booking.Get.Endpoint;

namespace IntegrationTests.Features.Booking;

[Collection("AppFixture collection")]
public class CreateBookingTests(AppFixture app)
{
    [Fact]
    public async Task CreateBooking_WithOnlineExtras_AddsExtrasToOutstandingAmountAndPaymentSummary()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var outlet = await CreateOutlet(db);
        var facilityTypeId = await CreateFacilityType(db);

        var facility = new Facility
        {
            Name = "Booking Facility",
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

        var contract = new Contract
        {
            Name = $"Contract_{Guid.NewGuid()}",
            BusinessId = outlet.BusinessId,
            Business = outlet.Business,
        };
        db.Contract.Add(contract);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        var slotContract = new SlotContract
        {
            SlotId = slot.Id,
            Slot = slot,
            ContractId = contract.Id,
            Contract = contract,
            Price = 100m,
            CanPayLater = false,
            Description = "Guest 18 Holes",
        };
        db.SlotContract.Add(slotContract);

        var extra = new Extra
        {
            FacilityId = facility.Id,
            Facility = facility,
            OutletId = outlet.Id,
            Name = "Golf Cart",
            Code = $"EXTRA_{Guid.NewGuid():N}",
            Price = 300m,
            IsAvailable = true,
            IsOnline = true,
        };
        db.Extra.Add(extra);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        var request = new BookingCreateRequest
        {
            Bookings =
            [
                new BookingRequest
                {
                    SlotId = slot.Id,
                    SlotContractId = slotContract.Id,
                    Name = "Jaco Taute",
                    Email = "jaco@example.com",
                    Cellphone = "0842502311",
                },
            ],
            Extras = [new BookingExtraRequest { ExtraId = extra.Id, Amount = 2 }],
        };

        // Act
        var (createResponse, createdBooking) = await app.Client.POSTAsync<BookingCreateEndpoint, BookingCreateRequest, BookingCreateResponse>(request);

        // Assert
        createResponse.IsSuccessStatusCode.ShouldBeTrue();
        createdBooking.Id.ShouldBeGreaterThan(0);

        var persistedBooking = await db
            .Booking.Include(booking => booking.ExtraBookings)
            .FirstAsync(booking => booking.Id == createdBooking.Id, app.Context.CancellationToken);

        persistedBooking.AmountOutstanding.ShouldBe(700m);
        persistedBooking.UserId.ShouldBe(TestClaims.UserIdGuid);
        persistedBooking.ExtraBookings.ShouldHaveSingleItem();
        persistedBooking.ExtraBookings.Single().Amount.ShouldBe(2);

        var (getResponse, bookingDto) = await app.Client.GETAsync<BookingGetEndpoint, BookingGetRequest, BookingDTO>(
            new BookingGetRequest { Id = createdBooking.Id }
        );

        getResponse.IsSuccessStatusCode.ShouldBeTrue();
        bookingDto.AmountOutstanding.ShouldBe(700m);
        bookingDto.ExtraBookings.ShouldHaveSingleItem();
        bookingDto.ExtraBookings.Single().ExtraId.ShouldBe(extra.Id);
        bookingDto.ExtraBookings.Single().Amount.ShouldBe(2);
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
