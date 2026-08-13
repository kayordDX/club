using System.Linq;
using System.Text.Json;
using Club.Common.Enums;
using Club.Common.Models;
using Club.Data;
using Club.DTO;
using Club.Entities;
using Club.Features.Admin.Booking.Get;
using Club.Features.Admin.Booking.GetAll;
using Club.Features.Admin.Booking.UpdateStatus;
using Club.Features.Booking.Create;
using IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AdminBookingGetAllEndpoint = Club.Features.Admin.Booking.GetAll.Endpoint;
using AdminBookingGetEndpoint = Club.Features.Admin.Booking.Get.Endpoint;
using AdminBookingUpdateStatusEndpoint = Club.Features.Admin.Booking.UpdateStatus.Endpoint;
using BookingCreateEndpoint = Club.Features.Booking.Create.Endpoint;

namespace IntegrationTests.Features.Booking;

[Collection("AppFixture collection")]
public class AdminBookingTests(AppFixture app)
{
    [Fact]
    public async Task AdminBookingGetAll_WhenManager_ReturnsFacilityBookings()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (slot, slotContract, facilityId) = await CreateBookingSetup(db);
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
        var (getResponse, _) = await app.Client.GETAsync<AdminBookingGetAllEndpoint, AdminBookingGetAllRequest, PaginatedList<AdminBookingDTO>>(
            new AdminBookingGetAllRequest { FacilityId = facilityId }
        );

        // Assert - parse the JSON directly because PaginatedList<T> has no parameterless
        // constructor, so FastEndpoints.Testing cannot deserialize it into the typed result.
        getResponse.IsSuccessStatusCode.ShouldBeTrue();
        var body = await getResponse.Content.ReadAsStringAsync(app.Context.CancellationToken);
        using var document = JsonDocument.Parse(body);
        var itemIds = document.RootElement.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetInt32()).ToArray();
        document.RootElement.GetProperty("totalCount").GetInt32().ShouldBeGreaterThan(0);
        itemIds.ShouldContain(createdBooking.Id);
    }

    [Fact]
    public async Task AdminBookingGetAll_WhenNotManager_ReturnsForbidden()
    {
        // Arrange - no manager role assigned for this facility
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (_, _, facilityId) = await CreateBookingSetup(db);

        // Act
        var (getResponse, _) = await app.Client.GETAsync<AdminBookingGetAllEndpoint, AdminBookingGetAllRequest, PaginatedList<AdminBookingDTO>>(
            new AdminBookingGetAllRequest { FacilityId = facilityId }
        );

        // Assert
        getResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminBookingGetAll_WithFiltersQuery_FiltersByDerivedExpiredStatus()
    {
        // Arrange - lowercase query params mirror what the generated frontend client sends
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (slot, slotContract, facilityId) = await CreateBookingSetup(db);
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

        // Push the pending booking past its expiry so it reads as Expired (derived status).
        var booking = await db.Booking.FirstAsync(b => b.Id == createdBooking.Id, app.Context.CancellationToken);
        booking.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        // Act - filter by Expired includes the derived-expired booking
        var expiredIds = await GetItemIdsAsync($"/admin/facility/{facilityId}/booking?filters={Uri.EscapeDataString("bookingStatusId == 4")}");
        // filtering by Pending must not surface it (it has effectively expired)
        var pendingIds = await GetItemIdsAsync($"/admin/facility/{facilityId}/booking?filters={Uri.EscapeDataString("bookingStatusId == 1")}");

        // Assert
        expiredIds.ShouldContain(createdBooking.Id);
        pendingIds.ShouldNotContain(createdBooking.Id);
    }

    [Fact]
    public async Task AdminBookingGetAll_WithFiltersQuery_FiltersByBookingId()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (slot, slotContract, facilityId) = await CreateBookingSetup(db);
        await AssignManagerRole(db, facilityId);

        var (_, createdBooking) = await app.Client.POSTAsync<BookingCreateEndpoint, BookingCreateRequest, BookingCreateResponse>(
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

        // Act
        var matched = await GetItemIdsAsync($"/admin/facility/{facilityId}/booking?filters={Uri.EscapeDataString($"id == {createdBooking.Id}")}");
        var missed = await GetItemIdsAsync($"/admin/facility/{facilityId}/booking?filters={Uri.EscapeDataString("id == 999999")}");

        // Assert
        matched.ShouldHaveSingleItem();
        matched.ShouldContain(createdBooking.Id);
        missed.ShouldBeEmpty();
    }

    [Fact]
    public async Task AdminBookingGetAll_IncludesFacilityAndSlotTimes()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (slot, slotContract, facilityId) = await CreateBookingSetup(db);
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
        var response = await app.Client.GetAsync(
            $"/admin/facility/{facilityId}/booking?filters={Uri.EscapeDataString($"id == {createdBooking.Id}")}",
            app.Context.CancellationToken
        );
        response.IsSuccessStatusCode.ShouldBeTrue();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(app.Context.CancellationToken));
        var item = document.RootElement.GetProperty("items").EnumerateArray().Single();

        // Assert - the new summary fields are populated
        item.GetProperty("facilityName").GetString().ShouldBe("Admin Facility");
        item.GetProperty("slotStartDatetime").GetString().ShouldNotBeNullOrEmpty();
        item.GetProperty("slotEndDatetime").GetString().ShouldNotBeNullOrEmpty();
        item.GetProperty("playerCount").GetInt32().ShouldBe(1);
    }

    private async Task<int[]> GetItemIdsAsync(string pathAndQuery)
    {
        var response = await app.Client.GetAsync(pathAndQuery, app.Context.CancellationToken);
        response.IsSuccessStatusCode.ShouldBeTrue();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(app.Context.CancellationToken));
        return document.RootElement.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32()).ToArray();
    }

    [Fact]
    public async Task AdminBookingUpdateStatus_WhenManager_ChangesStatusWithoutRemovingPlayers()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (slot, slotContract, facilityId) = await CreateBookingSetup(db);
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

        // Act - confirm the booking
        var statusResponse = await app.Client.PUTAsync<AdminBookingUpdateStatusEndpoint, AdminBookingUpdateStatusRequest>(
            new AdminBookingUpdateStatusRequest
            {
                FacilityId = facilityId,
                Id = createdBooking.Id,
                Status = BookingStatusEnum.Confirmed,
            }
        );

        // Assert
        statusResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var persisted = await db.Booking.Include(b => b.SlotContractBookings).FirstAsync(b => b.Id == createdBooking.Id, app.Context.CancellationToken);

        persisted.BookingStatusId.ShouldBe((int)BookingStatusEnum.Confirmed);
        // Manager status changes must stay reversible: players are not removed on confirm.
        persisted.SlotContractBookings.ShouldHaveSingleItem();

        // The detail endpoint (also manager-guarded) returns the updated status.
        var (getResponse, detail) = await app.Client.GETAsync<AdminBookingGetEndpoint, AdminBookingGetRequest, BookingDTO>(
            new AdminBookingGetRequest { FacilityId = facilityId, Id = createdBooking.Id }
        );
        getResponse.IsSuccessStatusCode.ShouldBeTrue();
        detail!.BookingStatus.Id.ShouldBe((int)BookingStatusEnum.Confirmed);
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

    private static async Task<(Club.Entities.Slot Slot, SlotContract SlotContract, int FacilityId)> CreateBookingSetup(AppDbContext db)
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
            Name = "Admin Facility",
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
