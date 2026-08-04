using Club.Common.Enums;
using Club.Data;
using Club.Entities;
using Club.Features.Booking.Create;
using Club.Features.Booking.GetPath;
using Club.Features.Booking.Update;
using Club.Features.Booking.UpdateStatus;
using IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BookingCreateEndpoint = Club.Features.Booking.Create.Endpoint;
using BookingGetPathEndpoint = Club.Features.Booking.GetPath.Endpoint;
using BookingUpdateEndpoint = Club.Features.Booking.Update.Endpoint;
using BookingUpdateStatusEndpoint = Club.Features.Booking.UpdateStatus.Endpoint;

namespace IntegrationTests.Features.Booking;

[Collection("AppFixture collection")]
public class UpdateBookingTests(AppFixture app)
{
    [Fact]
    public async Task UpdateBooking_WhenPending_ReplacesPlayersAndExtrasAndRecalculatesOutstanding()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (slot, slotContract, extra, _) = await CreateBookingSetup(db);
        var createRequest = new BookingCreateRequest
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

        var (createResponse, createdBooking) = await app.Client.POSTAsync<BookingCreateEndpoint, BookingCreateRequest, BookingCreateResponse>(createRequest);
        createResponse.IsSuccessStatusCode.ShouldBeTrue();

        // Act
        var updateRequest = new BookingUpdateRequest
        {
            Id = createdBooking.Id,
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
                new BookingRequest
                {
                    SlotId = slot.Id,
                    SlotContractId = slotContract.Id,
                    Name = "Jane Doe",
                    Email = "jane@example.com",
                    Cellphone = "0825551234",
                },
            ],
            Extras = [],
        };

        var updateResponse = await app.Client.PUTAsync<BookingUpdateEndpoint, BookingUpdateRequest>(updateRequest);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var persistedBooking = await db
            .Booking.Include(booking => booking.SlotContractBookings)
            .Include(booking => booking.ExtraBookings)
            .FirstAsync(booking => booking.Id == createdBooking.Id, app.Context.CancellationToken);

        persistedBooking.AmountOutstanding.ShouldBe(200m);
        persistedBooking.SlotContractBookings.Count.ShouldBe(2);
        persistedBooking.SlotContractBookings.ShouldContain(booking => booking.Name == "Jane Doe");
        persistedBooking.SlotContractBookings.ShouldContain(booking => booking.Name == "Jaco Taute");
        persistedBooking.ExtraBookings.ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateBooking_WhenPlayersExceedAvailability_ReturnsConflict()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (slot, slotContract, _, _) = await CreateBookingSetup(db);

        // Fill the slot (MaxBookings = 4) with two separate bookings of two players each.
        var createFirst = new BookingCreateRequest
        {
            Bookings =
            [
                new BookingRequest
                {
                    SlotId = slot.Id,
                    SlotContractId = slotContract.Id,
                    Name = "Player One",
                    Email = "one@example.com",
                    Cellphone = "0825551001",
                },
                new BookingRequest
                {
                    SlotId = slot.Id,
                    SlotContractId = slotContract.Id,
                    Name = "Player Two",
                    Email = "two@example.com",
                    Cellphone = "0825551002",
                },
            ],
        };
        var (firstResponse, firstBooking) = await app.Client.POSTAsync<BookingCreateEndpoint, BookingCreateRequest, BookingCreateResponse>(createFirst);
        firstResponse.IsSuccessStatusCode.ShouldBeTrue();

        var createSecond = new BookingCreateRequest
        {
            Bookings =
            [
                new BookingRequest
                {
                    SlotId = slot.Id,
                    SlotContractId = slotContract.Id,
                    Name = "Player Three",
                    Email = "three@example.com",
                    Cellphone = "0825551003",
                },
                new BookingRequest
                {
                    SlotId = slot.Id,
                    SlotContractId = slotContract.Id,
                    Name = "Player Four",
                    Email = "four@example.com",
                    Cellphone = "0825551004",
                },
            ],
        };
        var (secondResponse, _) = await app.Client.POSTAsync<BookingCreateEndpoint, BookingCreateRequest, BookingCreateResponse>(createSecond);
        secondResponse.IsSuccessStatusCode.ShouldBeTrue();

        // Act - try to grow the first booking from 2 to 3 players while the slot is full.
        var updateRequest = new BookingUpdateRequest
        {
            Id = firstBooking.Id,
            Bookings =
            [
                new BookingRequest
                {
                    SlotId = slot.Id,
                    SlotContractId = slotContract.Id,
                    Name = "Player One",
                    Email = "one@example.com",
                    Cellphone = "0825551001",
                },
                new BookingRequest
                {
                    SlotId = slot.Id,
                    SlotContractId = slotContract.Id,
                    Name = "Player Two",
                    Email = "two@example.com",
                    Cellphone = "0825551002",
                },
                new BookingRequest
                {
                    SlotId = slot.Id,
                    SlotContractId = slotContract.Id,
                    Name = "Player Five",
                    Email = "five@example.com",
                    Cellphone = "0825551005",
                },
            ],
        };

        var updateResponse = await app.Client.PUTAsync<BookingUpdateEndpoint, BookingUpdateRequest>(updateRequest);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateBooking_WhenNotPending_ReturnsBadRequest()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (slot, slotContract, _, _) = await CreateBookingSetup(db);
        var createRequest = new BookingCreateRequest
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
        };

        var (createResponse, createdBooking) = await app.Client.POSTAsync<BookingCreateEndpoint, BookingCreateRequest, BookingCreateResponse>(createRequest);
        createResponse.IsSuccessStatusCode.ShouldBeTrue();

        var confirmResponse = await app.Client.PUTAsync<BookingUpdateStatusEndpoint, BookingUpdateStatusRequest>(
            new BookingUpdateStatusRequest { BookingId = createdBooking.Id, Status = BookingStatusEnum.Confirmed }
        );
        confirmResponse.IsSuccessStatusCode.ShouldBeTrue();

        // Act
        var updateRequest = new BookingUpdateRequest
        {
            Id = createdBooking.Id,
            Bookings =
            [
                new BookingRequest
                {
                    SlotId = slot.Id,
                    SlotContractId = slotContract.Id,
                    Name = "Changed Name",
                    Email = "changed@example.com",
                    Cellphone = "0825551234",
                },
            ],
        };

        var updateResponse = await app.Client.PUTAsync<BookingUpdateEndpoint, BookingUpdateRequest>(updateRequest);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateBooking_WhenNotTheOwner_ReturnsForbidden()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (slot, slotContract, _, _) = await CreateBookingSetup(db);

        // Create a booking owned by a different user.
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var otherUserId = Guid.NewGuid();
        await userManager.CreateAsync(
            new User
            {
                Id = otherUserId,
                UserName = $"other-user-{Guid.NewGuid():N}",
                Email = "other@example.com",
                EmailConfirmed = true,
                FirstName = "Other",
                LastName = "User",
            }
        );

        var booking = new Club.Entities.Booking
        {
            BookingStatusId = (int)BookingStatusEnum.Pending,
            BookingStatusDate = DateTime.UtcNow,
            IsPaid = false,
            AmountOutstanding = 100m,
            AmountPaid = 0,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            UserId = otherUserId,
        };
        db.Booking.Add(booking);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        db.SlotContractBooking.Add(
            new SlotContractBooking
            {
                SlotContractId = slotContract.Id,
                BookingId = booking.Id,
                Name = "Other Player",
                Email = "player@example.com",
                Cellphone = "0825551234",
            }
        );
        await db.SaveChangesAsync(app.Context.CancellationToken);

        // Act
        var updateRequest = new BookingUpdateRequest
        {
            Id = booking.Id,
            Bookings =
            [
                new BookingRequest
                {
                    SlotId = slot.Id,
                    SlotContractId = slotContract.Id,
                    Name = "Hijacker",
                    Email = "hacker@example.com",
                    Cellphone = "0825551234",
                },
            ],
        };

        var updateResponse = await app.Client.PUTAsync<BookingUpdateEndpoint, BookingUpdateRequest>(updateRequest);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetBookingPath_ReturnsOutletAndFacilityForBreadcrumbs()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (slot, slotContract, _, _) = await CreateBookingSetup(db);
        var createRequest = new BookingCreateRequest
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
        };

        var (createResponse, createdBooking) = await app.Client.POSTAsync<BookingCreateEndpoint, BookingCreateRequest, BookingCreateResponse>(createRequest);
        createResponse.IsSuccessStatusCode.ShouldBeTrue();

        // Act
        var (getResponse, path) = await app.Client.GETAsync<BookingGetPathEndpoint, BookingGetPathRequest, BookingPathDTO>(
            new BookingGetPathRequest { Id = createdBooking.Id }
        );

        // Assert
        getResponse.IsSuccessStatusCode.ShouldBeTrue();
        path.ShouldNotBeNull();
        path.BookingId.ShouldBe(createdBooking.Id);
        path.FacilityId.ShouldBe(slot.FacilityId!.Value);
        path.SlotId.ShouldBe(slot.Id);
        path.OutletSlug.ShouldNotBeNullOrEmpty();
        path.OutletName.ShouldNotBeNullOrEmpty();
        path.FacilityName.ShouldNotBeNullOrEmpty();
    }

    private static async Task<(Club.Entities.Slot Slot, SlotContract SlotContract, Extra Extra, int FacilityId)> CreateBookingSetup(AppDbContext db)
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
            Name = "Booking Facility",
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

        var contract = new Contract
        {
            Name = $"Contract_{Guid.NewGuid()}",
            FacilityId = facility.Id,
            Facility = facility,
        };
        db.Contract.Add(contract);
        await db.SaveChangesAsync();

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
        await db.SaveChangesAsync();

        return (slot, slotContract, extra, facility.Id);
    }
}
