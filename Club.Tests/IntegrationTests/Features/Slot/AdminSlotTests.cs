using Club.Common.Enums;
using Club.Data;
using Club.Entities;
using Club.Features.Admin.Slot.GetAll;
using Club.Features.Booking.Create;
using IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AdminSlotGetAllEndpoint = Club.Features.Admin.Slot.GetAll.Endpoint;
using BookingCreateEndpoint = Club.Features.Booking.Create.Endpoint;

namespace IntegrationTests.Features.Slot;

[Collection("AppFixture collection")]
public class AdminSlotTests(AppFixture app)
{
    [Fact]
    public async Task AdminSlotGetAll_WhenManager_ReturnsSlotsWithBookingDetails()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (slot, slotContract, facilityId) = await CreateSlotSetup(db);
        await AssignManagerRole(db, facilityId);

        var (createResponse, createdBooking) = await app.Client.POSTAsync<BookingCreateEndpoint, BookingCreateRequest, BookingCreateResponse>(
            new BookingCreateRequest
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
            }
        );
        createResponse.IsSuccessStatusCode.ShouldBeTrue();

        // Act
        var (getResponse, result) = await app.Client.GETAsync<AdminSlotGetAllEndpoint, AdminSlotGetAllRequest, List<AdminSlotGetAllResponse>>(
            new AdminSlotGetAllRequest { FacilityId = facilityId, Date = slot.StartDatetime.Date }
        );

        // Assert
        getResponse.IsSuccessStatusCode.ShouldBeTrue();
        result.ShouldHaveSingleItem();
        var returnedSlot = result.First();
        returnedSlot.Id.ShouldBe(slot.Id);
        returnedSlot.Total.ShouldBe(slot.MaxBookings);
        returnedSlot.Booked.ShouldBe(1);
        returnedSlot.Bookings.ShouldHaveSingleItem();

        var booking = returnedSlot.Bookings.Single();
        booking.BookingId.ShouldBe(createdBooking.Id);
        booking.PlayerName.ShouldBe("Jaco Taute");
        booking.BookingStatusId.ShouldBe((int)BookingStatusEnum.Pending);
        booking.BookingStatusName.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task AdminSlotGetAll_WhenNotManager_ReturnsForbidden()
    {
        // Arrange - no manager role for this facility
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (slot, _, facilityId) = await CreateSlotSetup(db);

        // Act
        var (getResponse, _) = await app.Client.GETAsync<AdminSlotGetAllEndpoint, AdminSlotGetAllRequest, List<AdminSlotGetAllResponse>>(
            new AdminSlotGetAllRequest { FacilityId = facilityId, Date = slot.StartDatetime.Date }
        );

        // Assert
        getResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminSlotGetAll_ExcludesCancelledAndExpiredPendingBookings()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (slot, slotContract, facilityId) = await CreateSlotSetup(db);
        await AssignManagerRole(db, facilityId);

        var (createActiveResponse, activeBooking) = await app.Client.POSTAsync<BookingCreateEndpoint, BookingCreateRequest, BookingCreateResponse>(
            new BookingCreateRequest
            {
                Bookings =
                [
                    new BookingRequest
                    {
                        SlotId = slot.Id,
                        SlotContractId = slotContract.Id,
                        Name = "Active Player",
                        Email = "active@example.com",
                        Cellphone = "0842502311",
                    },
                ],
            }
        );
        createActiveResponse.IsSuccessStatusCode.ShouldBeTrue();

        var (createCancelledResponse, cancelledBooking) = await app.Client.POSTAsync<BookingCreateEndpoint, BookingCreateRequest, BookingCreateResponse>(
            new BookingCreateRequest
            {
                Bookings =
                [
                    new BookingRequest
                    {
                        SlotId = slot.Id,
                        SlotContractId = slotContract.Id,
                        Name = "Cancelled Player",
                        Email = "cancelled@example.com",
                        Cellphone = "0842502311",
                    },
                ],
            }
        );
        createCancelledResponse.IsSuccessStatusCode.ShouldBeTrue();

        var cancelled = await db.Booking.FirstAsync(b => b.Id == cancelledBooking.Id, app.Context.CancellationToken);
        cancelled.BookingStatusId = (int)BookingStatusEnum.Cancelled;
        await db.SaveChangesAsync(app.Context.CancellationToken);

        // Act
        var (getResponse, result) = await app.Client.GETAsync<AdminSlotGetAllEndpoint, AdminSlotGetAllRequest, List<AdminSlotGetAllResponse>>(
            new AdminSlotGetAllRequest { FacilityId = facilityId, Date = slot.StartDatetime.Date }
        );

        // Assert - cancelled booking no longer counts toward or lists under the slot
        getResponse.IsSuccessStatusCode.ShouldBeTrue();
        var returnedSlot = result.Single(s => s.Id == slot.Id);
        returnedSlot.Booked.ShouldBe(1);
        returnedSlot.Bookings.ShouldHaveSingleItem();
        returnedSlot.Bookings.Single().BookingId.ShouldBe(activeBooking.Id);

        // The cancelled booking is still visible on the admin bookings list for management.
        var active = await db.Booking.FirstAsync(b => b.Id == activeBooking.Id, app.Context.CancellationToken);
        active.BookingStatusId.ShouldBe((int)BookingStatusEnum.Pending);
    }

    private static async Task AssignManagerRole(AppDbContext db, int facilityId)
    {
        const string normalizedName = "MANAGER";
        var role = await db.Roles.FirstOrDefaultAsync(r => r.NormalizedName == normalizedName);
        if (role is null)
        {
            role = new Role { Name = Club.Constants.Policy.Manager, NormalizedName = normalizedName };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
        }

        var alreadyAssigned = await db.UserRoles.AnyAsync(ur => ur.UserId == TestClaims.UserIdGuid && ur.RoleId == role.Id && ur.FacilityId == facilityId);
        if (!alreadyAssigned)
        {
            db.UserRoles.Add(
                new UserRole
                {
                    UserId = TestClaims.UserIdGuid,
                    RoleId = role.Id,
                    FacilityId = facilityId,
                }
            );
            await db.SaveChangesAsync();
        }
    }

    private static async Task<(Club.Entities.Slot Slot, SlotContract SlotContract, int FacilityId)> CreateSlotSetup(AppDbContext db)
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
            IsActive = true,
        };
        db.Outlet.Add(outlet);
        await db.SaveChangesAsync();

        await db.Database.ExecuteSqlRawAsync("INSERT INTO facility_type (name) VALUES ({0}) ON CONFLICT DO NOTHING", $"FacilityType_{Guid.NewGuid()}");
        var facilityType = await db.Database.SqlQueryRaw<FacilityType>("SELECT id, name FROM facility_type ORDER BY id DESC LIMIT 1").FirstOrDefaultAsync();

        var facility = new Facility
        {
            Name = "Admin Slot Facility",
            Outlet = outlet,
            OutletId = outlet.Id,
            FacilityTypeId = facilityType?.Id ?? 1,
            IsActive = true,
        };
        db.Facility.Add(facility);
        await db.SaveChangesAsync();

        var slot = new Club.Entities.Slot
        {
            Id = Guid.NewGuid(),
            FacilityId = facility.Id,
            StartDatetime = DateTime.UtcNow.AddDays(1),
            EndDatetime = DateTime.UtcNow.AddDays(1).AddHours(1),
            MaxBookings = 4,
        };
        db.Slot.Add(slot);

        var contract = new Contract { Name = $"Contract_{Guid.NewGuid()}" };
        db.Contract.Add(contract);
        db.ContractFacility.Add(new ContractFacility { Contract = contract, Facility = facility });
        await db.SaveChangesAsync();

        var slotContract = new SlotContract
        {
            SlotId = slot.Id,
            Slot = slot,
            ContractId = contract.Id,
            Contract = contract,
            Price = 100m,
            CanPayLater = false,
        };
        db.SlotContract.Add(slotContract);
        await db.SaveChangesAsync();

        return (slot, slotContract, facility.Id);
    }
}
