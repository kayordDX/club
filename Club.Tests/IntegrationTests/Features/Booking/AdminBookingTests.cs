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
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
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

    [Theory]
    [InlineData(100, 60, false, 0)]
    [InlineData(150, 110, false, 0)]
    [InlineData(40, 0, true, 0)]
    [InlineData(30, 60, false, 0)]
    [InlineData(80, 60, false, 50)]
    public async Task AdminBookingUpdate_AfterPartialPayment_PreservesPaidBalance(int price, int outstanding, bool isPaid, int pending)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, slotContract, facilityId) = await CreateBookingSetup(db);
        await AssignManagerRole(db, facilityId);
        var player = new BookingRequest
        {
            SlotId = slot.Id,
            SlotContractId = slotContract.Id,
            Name = "Partial payment player",
            Email = "partial@example.com",
            Cellphone = "0842502311",
        };
        var (createResponse, created) = await app.Client.POSTAsync<BookingCreateEndpoint, BookingCreateRequest, BookingCreateResponse>(
            new BookingCreateRequest { Bookings = [player] }
        );
        createResponse.IsSuccessStatusCode.ShouldBeTrue();
        var booking = await db.Booking.SingleAsync(b => b.Id == created.Id, cancellationToken: TestContext.Current.CancellationToken);
        var payment = new Club.Entities.Payment
        {
            Amount = 40m,
            PaymentStatus = await db.PaymentStatus.SingleAsync(s => s.Id == (int)PaymentStatusEnum.Pending, app.Context.CancellationToken),
            PaymentStatusId = (int)PaymentStatusEnum.Pending,
            PaymentTypeId = (int)PaymentTypeEnum.CreditCard,
            PaymentStatusDate = DateTime.UtcNow,
            TransactionId = Guid.NewGuid().ToString(),
            ProviderName = "payfast",
        };
        db.PaymentBooking.Add(new PaymentBooking { Booking = booking, Payment = payment });
        await Club.Common.Payments.BookingPayments.ApplyAsync(db, booking, payment, app.Context.CancellationToken);
        if (pending > 0)
        {
            db.PaymentBooking.Add(
                new PaymentBooking
                {
                    Booking = booking,
                    Payment = new Club.Entities.Payment
                    {
                        Amount = pending,
                        PaymentStatus = payment.PaymentStatus,
                        PaymentStatusId = (int)PaymentStatusEnum.Pending,
                        PaymentTypeId = (int)PaymentTypeEnum.CreditCard,
                        PaymentStatusDate = DateTime.UtcNow,
                        TransactionId = Guid.NewGuid().ToString(),
                        ProviderName = "payfast",
                    },
                }
            );
        }
        slotContract.Price = price;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var response = await app.Client.PUTAsync<Club.Features.Admin.Booking.Update.Endpoint, Club.Features.Admin.Booking.Update.AdminBookingUpdateRequest>(
            new Club.Features.Admin.Booking.Update.AdminBookingUpdateRequest
            {
                FacilityId = facilityId,
                Id = booking.Id,
                Bookings = [player],
            }
        );

        response.StatusCode.ShouldBe(price < 40 + pending ? HttpStatusCode.BadRequest : HttpStatusCode.NoContent);
        var persisted = await db.Booking.AsNoTracking().SingleAsync(b => b.Id == booking.Id, cancellationToken: TestContext.Current.CancellationToken);
        persisted.AmountPaid.ShouldBe(40m);
        persisted.AmountOutstanding.ShouldBe((decimal)outstanding);
        persisted.IsPaid.ShouldBe(isPaid);
        (await db.Payment.AsNoTracking().SingleAsync(p => p.Id == payment.Id, cancellationToken: TestContext.Current.CancellationToken)).Amount.ShouldBe(40m);
    }

    [Theory]
    [InlineData(BookingStatusEnum.Cancelled)]
    [InlineData(BookingStatusEnum.Expired)]
    public async Task BookingWithoutSlotLinks_RemainsLinkedToFacility(BookingStatusEnum status)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ct = app.Context.CancellationToken;
        var (slot, slotContract, facilityId) = await CreateBookingSetup(db);
        await AssignManagerRole(db, facilityId);
        var (_, _, otherFacilityId) = await CreateBookingSetup(db);
        await AssignManagerRole(db, otherFacilityId);
        var (response, created) = await app.Client.POSTAsync<BookingCreateEndpoint, BookingCreateRequest, BookingCreateResponse>(
            new BookingCreateRequest
            {
                Bookings =
                [
                    new BookingRequest
                    {
                        SlotId = slot.Id,
                        SlotContractId = slotContract.Id,
                        Name = "Player",
                    },
                ],
            }
        );
        response.IsSuccessStatusCode.ShouldBeTrue();
        var booking = await db.Booking.SingleAsync(b => b.Id == created.Id, ct);
        booking.FacilityId.ShouldBe(facilityId);

        if (status == BookingStatusEnum.Expired)
        {
            booking.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync(ct);
            await new Club.Jobs.FunctionJob(db).ClearExpiredBookings(ct);
        }
        else
        {
            booking.BookingStatusId = (int)status;
            await db.SaveChangesAsync(ct);
            // Simulate older cancellations, which deleted their slot links.
            await db.SlotContractBooking.Where(scb => scb.BookingId == booking.Id).ExecuteDeleteAsync(ct);
        }

        (await db.Booking.AsNoTracking().SingleAsync(b => b.Id == booking.Id, ct)).FacilityId.ShouldBe(facilityId);
        (await db.SlotContractBooking.CountAsync(scb => scb.BookingId == booking.Id, ct)).ShouldBe(0);
        var list = await app.Client.GetAsync($"/admin/facility/{facilityId}/booking?filters={Uri.EscapeDataString($"id == {booking.Id}")}", ct);
        list.IsSuccessStatusCode.ShouldBeTrue();
        using var document = JsonDocument.Parse(await list.Content.ReadAsStringAsync(ct));
        var item = document.RootElement.GetProperty("items").EnumerateArray().Single();
        item.GetProperty("bookingStatusId").GetInt32().ShouldBe((int)status);
        item.GetProperty("facilityName").GetString().ShouldBe("Admin Facility");
        item.GetProperty("slotStartDatetime").ValueKind.ShouldBe(JsonValueKind.Null);
        item.GetProperty("slotEndDatetime").ValueKind.ShouldBe(JsonValueKind.Null);

        var (detailResponse, _) = await app.Client.GETAsync<AdminBookingGetEndpoint, AdminBookingGetRequest, BookingDTO>(
            new AdminBookingGetRequest { FacilityId = facilityId, Id = booking.Id }
        );
        detailResponse.IsSuccessStatusCode.ShouldBeTrue();
        var (wrongFacilityResponse, _) = await app.Client.GETAsync<AdminBookingGetEndpoint, AdminBookingGetRequest, BookingDTO>(
            new AdminBookingGetRequest { FacilityId = otherFacilityId, Id = booking.Id }
        );
        wrongFacilityResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await GetItemIdsAsync($"/admin/facility/{otherFacilityId}/booking?filters={Uri.EscapeDataString($"id == {booking.Id}")}")).ShouldBeEmpty();

        var (pathResponse, path) = await app.Client.GETAsync<
            Club.Features.Booking.GetPath.Endpoint,
            Club.Features.Booking.GetPath.BookingGetPathRequest,
            Club.Features.Booking.GetPath.BookingPathDTO
        >(new Club.Features.Booking.GetPath.BookingGetPathRequest { Id = booking.Id });
        pathResponse.IsSuccessStatusCode.ShouldBeTrue();
        path.FacilityId.ShouldBe(facilityId);
        path.OutletName.ShouldNotBeNullOrEmpty();
        path.SlotId.ShouldBeNull();
        path.SlotStartDatetime.ShouldBeNull();

        var summaryResponse = await app.Client.GetAsync($"/booking/user?filters={Uri.EscapeDataString($"id == {booking.Id}")}", ct);
        summaryResponse.IsSuccessStatusCode.ShouldBeTrue();
        using var summaryDocument = JsonDocument.Parse(await summaryResponse.Content.ReadAsStringAsync(ct));
        summaryDocument.RootElement.GetProperty("items").EnumerateArray().Single().GetProperty("facilityName").GetString().ShouldBe("Admin Facility");
        await CleanupFacilities(db, facilityId, otherFacilityId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateBooking_WithMultipleOrMissingFacilities_IsRejected(bool missingFacility)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, slotContract, facilityId) = await CreateBookingSetup(db);
        var (otherSlot, otherContract, otherFacilityId) = await CreateBookingSetup(db);
        if (missingFacility)
        {
            otherSlot.FacilityId = null;
            await db.SaveChangesAsync(app.Context.CancellationToken);
        }
        var count = await db.Booking.CountAsync(app.Context.CancellationToken);
        var (response, _) = await app.Client.POSTAsync<BookingCreateEndpoint, BookingCreateRequest, BookingCreateResponse>(
            new BookingCreateRequest
            {
                Bookings =
                [
                    new BookingRequest
                    {
                        SlotId = slot.Id,
                        SlotContractId = slotContract.Id,
                        Name = "Player",
                    },
                    new BookingRequest
                    {
                        SlotId = otherSlot.Id,
                        SlotContractId = otherContract.Id,
                        Name = "Other player",
                    },
                ],
            }
        );
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await db.Booking.CountAsync(app.Context.CancellationToken)).ShouldBe(count);
        await CleanupFacilities(db, facilityId, otherFacilityId);
    }

    [Fact]
    public async Task UpdateBooking_KeepsFacilityLinkInSync_AndManagersCannotMoveItToAnotherFacility()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, slotContract, facilityId) = await CreateBookingSetup(db);
        var (otherSlot, otherContract, otherFacilityId) = await CreateBookingSetup(db);
        await AssignManagerRole(db, facilityId);
        var (response, created) = await app.Client.POSTAsync<BookingCreateEndpoint, BookingCreateRequest, BookingCreateResponse>(
            new BookingCreateRequest
            {
                Bookings =
                [
                    new BookingRequest
                    {
                        SlotId = slot.Id,
                        SlotContractId = slotContract.Id,
                        Name = "Player",
                    },
                ],
            }
        );
        response.IsSuccessStatusCode.ShouldBeTrue();
        var player = new BookingRequest
        {
            SlotId = otherSlot.Id,
            SlotContractId = otherContract.Id,
            Name = "Moved player",
        };
        var adminResponse = await app.Client.PUTAsync<
            Club.Features.Admin.Booking.Update.Endpoint,
            Club.Features.Admin.Booking.Update.AdminBookingUpdateRequest
        >(
            new Club.Features.Admin.Booking.Update.AdminBookingUpdateRequest
            {
                Id = created.Id,
                FacilityId = facilityId,
                Bookings = [player],
            }
        );
        adminResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await db.Booking.AsNoTracking().SingleAsync(b => b.Id == created.Id, app.Context.CancellationToken)).FacilityId.ShouldBe(facilityId);

        var updateResponse = await app.Client.PUTAsync<Club.Features.Booking.Update.Endpoint, Club.Features.Booking.Update.BookingUpdateRequest>(
            new Club.Features.Booking.Update.BookingUpdateRequest { Id = created.Id, Bookings = [player] }
        );
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await db.Booking.AsNoTracking().SingleAsync(b => b.Id == created.Id, app.Context.CancellationToken)).FacilityId.ShouldBe(otherFacilityId);
        await CleanupFacilities(db, facilityId, otherFacilityId);
    }

    private async Task CleanupFacilities(AppDbContext db, params int[] facilityIds)
    {
        var ct = app.Context.CancellationToken;
        var outletIds = await db.Facility.Where(f => facilityIds.Contains(f.Id)).Select(f => f.OutletId).ToListAsync(ct);
        await db.Booking.Where(b => b.FacilityId.HasValue && facilityIds.Contains(b.FacilityId.Value)).ExecuteDeleteAsync(ct);
        await db.Slot.Where(s => s.FacilityId.HasValue && facilityIds.Contains(s.FacilityId.Value)).ExecuteDeleteAsync(ct);
        await db.UserRoles.Where(r => r.FacilityId.HasValue && facilityIds.Contains(r.FacilityId.Value)).ExecuteDeleteAsync(ct);
        await db.Outlet.Where(o => outletIds.Contains(o.Id)).ExecuteDeleteAsync(ct);
    }

    [Fact]
    public async Task BookingFacilityMigration_BackfillsUnambiguousLinks_AndLeavesUnknownBookingsNull()
    {
        var ct = app.Context.CancellationToken;
        await using var postgres = new PostgreSqlBuilder("postgres:18").Build();
        await postgres.StartAsync(ct);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseApplicationServiceProvider(app.Server.Services)
            .UseNpgsql(postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var db = new AppDbContext(options, new HttpContextAccessor());
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260929204410_WalletVoucherGrantProvenance", ct);
        var (_, slotContract, facilityId) = await CreateBookingSetup(db);
        var (_, otherContract, _) = await CreateBookingSetup(db);
        var facility = await db.Facility.SingleAsync(f => f.Id == facilityId, ct);
        var extra = new Extra
        {
            FacilityId = facilityId,
            OutletId = facility.OutletId,
            Name = "Legacy extra",
            Code = "LEGACY",
            Price = 10m,
        };
        db.Extra.Add(extra);
        await db.SaveChangesAsync(ct);
        db.BookingStatus.Add(new BookingStatus { Id = (int)BookingStatusEnum.Cancelled, Name = "Cancelled" });
        await db.SaveChangesAsync(ct);
        var now = DateTime.UtcNow;
        for (var id = 1; id <= 4; id++)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO booking (id, booking_status_id, booking_status_date, amount_paid, amount_outstanding, expires_at, is_paid, created)
                VALUES ({id}, {(int)BookingStatusEnum.Cancelled}, {now}, {25m}, {75m}, {now}, false, {now})
                """,
                ct
            );
        }
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO slot_contract_booking (booking_id, slot_contract_id, name)
            VALUES (1, {slotContract.Id}, 'Player'), (2, {slotContract.Id}, 'Player'), (2, {otherContract.Id}, 'Other player');
            INSERT INTO extra_booking (booking_id, extra_id, amount) VALUES (4, {extra.Id}, 1);
            """,
            ct
        );

        await migrator.MigrateAsync(cancellationToken: ct);

        var bookings = await db.Booking.AsNoTracking().OrderBy(b => b.Id).ToListAsync(ct);
        bookings.Count.ShouldBe(4);
        bookings[0].FacilityId.ShouldBe(facilityId);
        bookings[1].FacilityId.ShouldBeNull();
        bookings[2].FacilityId.ShouldBeNull();
        bookings[3].FacilityId.ShouldBe(facilityId);
        bookings.ShouldAllBe(b => b.AmountPaid == 25m && b.AmountOutstanding == 75m && b.BookingStatusId == (int)BookingStatusEnum.Cancelled);
        await migrator.MigrateAsync("20260929204410_WalletVoucherGrantProvenance", ct);
        (await db.Database.SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM booking").SingleAsync(ct)).ShouldBe(4);
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

        var contract = new Contract { Name = $"Contract_{Guid.NewGuid()}", IsPublic = true };
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
