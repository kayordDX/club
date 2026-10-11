using System.Net.Http.Json;
using Club.Common.Enums;
using Club.Data;
using Club.Entities;
using Club.Features.Booking.Create;
using IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Features.Booking;

public partial class AdminBookingTests
{
    private static BookingRequest Player(Club.Entities.Slot slot, SlotContract contract) =>
        new()
        {
            SlotId = slot.Id,
            SlotContractId = contract.Id,
            Name = "Status player",
            Email = "player@example.com",
            Cellphone = "0123456789",
        };

    private async Task<int> CreateStatusBooking(Club.Entities.Slot slot, SlotContract contract)
    {
        var response = await app.Client.PostAsJsonAsync(
            "/booking",
            new BookingCreateRequest { Bookings = [Player(slot, contract)] },
            app.Context.CancellationToken
        );
        var created = await response.Content.ReadFromJsonAsync<BookingCreateResponse>(app.Context.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return created!.Id;
    }

    private Task<HttpResponseMessage> SetAdminStatus(int facilityId, int id, BookingStatusEnum status) =>
        app.Client.PutAsJsonAsync($"/admin/facility/{facilityId}/booking/{id}/status", new { status }, app.Context.CancellationToken);

    [Theory]
    [InlineData(BookingStatusEnum.Cancelled)]
    [InlineData(BookingStatusEnum.Expired)]
    [InlineData(BookingStatusEnum.Pending)]
    public async Task Restore_GrantsFreshHold_AndRecordsAdminWithoutChangingData(BookingStatusEnum initial)
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await CreateBookingSetup(db);
        await AssignManagerRole(db, facilityId);
        var id = await CreateStatusBooking(slot, contract);
        var booking = await db.Booking.SingleAsync(x => x.Id == id, ct);
        booking.BookingStatusId = (int)initial;
        booking.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        booking.AmountPaid = 25m;
        booking.AmountOutstanding = 75m;
        await db.SaveChangesAsync(ct);
        var extra = new Extra
        {
            FacilityId = facilityId,
            OutletId = await db.Facility.Where(x => x.Id == facilityId).Select(x => x.OutletId).SingleAsync(ct),
            Name = "Status extra",
            Code = "STATUS",
            Price = 10m,
        };
        db.ExtraBooking.Add(
            new ExtraBooking
            {
                BookingId = id,
                Extra = extra,
                Amount = 2,
            }
        );
        await db.SaveChangesAsync(ct);
        var creator = booking.CreatedBy;
        var before = DateTime.UtcNow;
        (await SetAdminStatus(facilityId, id, BookingStatusEnum.Pending)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        db.ChangeTracker.Clear();
        var restored = await db.Booking.Include(x => x.SlotContractBookings).SingleAsync(x => x.Id == id, ct);
        restored.ExpiresAt.ShouldBeGreaterThan(before.AddMinutes(1));
        var timeout = scope
            .ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Club.Common.Config.AppConfig>>()
            .Value.PendingTimeoutMinutes;
        restored.ExpiresAt.ShouldBe(restored.BookingStatusDate.AddMinutes(Math.Max(1, timeout)));
        restored.AmountPaid.ShouldBe(25m);
        restored.AmountOutstanding.ShouldBe(75m);
        restored.IsPaid.ShouldBeFalse();
        restored.UserId.ShouldBe(TestClaims.UserIdGuid);
        restored.FacilityId.ShouldBe(facilityId);
        restored.CreatedBy.ShouldBe(creator);
        restored.LastModifiedBy.ShouldBe(TestClaims.UserIdGuid);
        var player = restored.SlotContractBookings.ShouldHaveSingleItem();
        player.SlotContractId.ShouldBe(contract.Id);
        player.Name.ShouldBe("Status player");
        player.Email.ShouldBe("player@example.com");
        player.Cellphone.ShouldBe("0123456789");
        var action = (await db.BookingAction.Where(x => x.BookingId == id).ToListAsync(ct)).ShouldHaveSingleItem();
        action.ActingAdminId.ShouldBe(TestClaims.UserIdGuid);
        action.Kind.ShouldBe(BookingActionKind.RestoredToPending);
        action.Timestamp.ShouldBeGreaterThanOrEqualTo(before);
        action.Timestamp.ShouldBe(restored.BookingStatusDate);
        (await SetAdminStatus(facilityId, id, BookingStatusEnum.Confirmed)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await db.Booking.Where(x => x.Id == id).ExecuteUpdateAsync(x => x.SetProperty(b => b.ExpiresAt, DateTime.UtcNow.AddDays(-1)), ct);
        await new Club.Jobs.FunctionJob(db).ClearExpiredBookings(ct);
        (await db.Booking.AsNoTracking().SingleAsync(x => x.Id == id, ct)).BookingStatusId.ShouldBe((int)BookingStatusEnum.Confirmed);
        (await SetAdminStatus(facilityId, id, BookingStatusEnum.Cancelled)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var actions = await db.BookingAction.Where(x => x.BookingId == id).OrderBy(x => x.Id).ToListAsync(ct);
        actions.Select(x => x.Kind).ShouldBe(new[] { BookingActionKind.RestoredToPending, BookingActionKind.Confirmed, BookingActionKind.Cancelled });
        actions.ShouldAllBe(x => x.ActingAdminId == TestClaims.UserIdGuid);
        (await db.ExtraBooking.SingleAsync(x => x.BookingId == id, ct)).Amount.ShouldBe(2);
        var cancelled = await db.Booking.AsNoTracking().SingleAsync(x => x.Id == id, ct);
        cancelled.AmountPaid.ShouldBe(25m);
        cancelled.AmountOutstanding.ShouldBe(75m);
        cancelled.FacilityId.ShouldBe(facilityId);
        (await db.SlotContractBooking.CountAsync(x => x.BookingId == id, ct)).ShouldBe(1);
        await CleanupFacilities(db, facilityId);
    }

    [Theory]
    [InlineData(BookingStatusEnum.Pending, false, 1)]
    [InlineData(BookingStatusEnum.Pending, true, 0)]
    [InlineData(BookingStatusEnum.Confirmed, true, 1)]
    [InlineData(BookingStatusEnum.Cancelled, false, 0)]
    [InlineData(BookingStatusEnum.Expired, false, 0)]
    public async Task ReservationStates_AgreeAcrossPublicAdminAndCreation(BookingStatusEnum status, bool expired, int occupied)
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await CreateBookingSetup(db);
        await AssignManagerRole(db, facilityId);
        slot.MaxBookings = 1;
        await db.SaveChangesAsync(ct);
        var id = await CreateStatusBooking(slot, contract);
        var booking = await db.Booking.SingleAsync(x => x.Id == id, ct);
        booking.BookingStatusId = (int)status;
        booking.ExpiresAt = expired ? DateTime.UtcNow.AddDays(-1) : DateTime.UtcNow.AddHours(1);
        await db.SaveChangesAsync(ct);
        var date = Uri.EscapeDataString(slot.StartDatetime.ToString("O"));
        foreach (var url in new[] { $"/slot?facilityId={facilityId}&date={date}", $"/admin/facility/{facilityId}/slot?date={date}" })
        {
            var response = await app.Client.GetAsync(url, ct);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var slots = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(ct);
            slots.EnumerateArray().Single().GetProperty("booked").GetInt32().ShouldBe(occupied);
        }
        var available = await app.Client.PostAsJsonAsync("/slot/available", new { id = slot.Id, slotCount = 1 }, ct);
        (await available.Content.ReadFromJsonAsync<bool>(ct)).ShouldBe(occupied == 0);
        var createdResponse = await app.Client.PostAsJsonAsync("/booking", new BookingCreateRequest { Bookings = [Player(slot, contract)] }, ct);
        createdResponse.StatusCode.ShouldBe(occupied == 0 ? HttpStatusCode.OK : HttpStatusCode.Conflict);
        if (occupied == 0)
        {
            var newBooking = await createdResponse.Content.ReadFromJsonAsync<BookingCreateResponse>(ct);
            (await SetAdminStatus(facilityId, newBooking!.Id, BookingStatusEnum.Cancelled)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }
        var (otherSlot, otherContract, otherFacilityId) = await CreateBookingSetup(db);
        otherSlot.FacilityId = facilityId;
        await db.SaveChangesAsync(ct);
        var editId = await CreateStatusBooking(otherSlot, otherContract);
        var edit = new { bookings = new[] { Player(slot, contract) }, extras = Array.Empty<object>() };
        foreach (var url in new[] { $"/booking/{editId}", $"/admin/facility/{facilityId}/booking/{editId}" })
            (await app.Client.PutAsJsonAsync(url, edit, ct)).StatusCode.ShouldBe(occupied == 0 ? HttpStatusCode.NoContent : HttpStatusCode.Conflict);
        await CleanupFacilities(db, facilityId, otherFacilityId);
    }

    [Fact]
    public async Task StatusMutation_RejectsOtherCustomerAndWrongFacility_WithoutActionRecords()
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await CreateBookingSetup(db);
        var (_, _, otherFacilityId) = await CreateBookingSetup(db);
        await AssignManagerRole(db, otherFacilityId);
        var id = await CreateStatusBooking(slot, contract);
        (await SetAdminStatus(facilityId, id, BookingStatusEnum.Cancelled)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await SetAdminStatus(otherFacilityId, id, BookingStatusEnum.Cancelled)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var owner = new User
        {
            Id = Guid.NewGuid(),
            UserName = $"owner-{Guid.NewGuid()}",
            FirstName = "Other",
            LastName = "Customer",
        };
        db.Users.Add(owner);
        var booking = await db.Booking.SingleAsync(x => x.Id == id, ct);
        booking.UserId = owner.Id;
        await db.SaveChangesAsync(ct);
        foreach (var status in new[] { BookingStatusEnum.Cancelled, BookingStatusEnum.Confirmed })
            (await app.Client.PutAsJsonAsync("/booking/status", new { bookingId = id, status }, ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await db.Booking.AsNoTracking().SingleAsync(x => x.Id == id, ct)).BookingStatusId.ShouldBe((int)BookingStatusEnum.Pending);
        (await db.BookingAction.CountAsync(x => x.BookingId == id, ct)).ShouldBe(0);
        await CleanupFacilities(db, facilityId, otherFacilityId);
    }

    [Fact]
    public async Task Restore_CompetingWithCreationForLastPlace_NeverOverbooks()
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await CreateBookingSetup(db);
        await AssignManagerRole(db, facilityId);
        slot.MaxBookings = 1;
        await db.SaveChangesAsync(ct);
        var id = await CreateStatusBooking(slot, contract);
        (await SetAdminStatus(facilityId, id, BookingStatusEnum.Cancelled)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var restore = SetAdminStatus(facilityId, id, BookingStatusEnum.Confirmed);
        var create = app.Client.PostAsJsonAsync("/booking", new BookingCreateRequest { Bookings = [Player(slot, contract)] }, ct);
        var responses = await Task.WhenAll(restore, create);
        responses.Count(x => x.IsSuccessStatusCode).ShouldBe(1);
        responses.Count(x => x.StatusCode == HttpStatusCode.Conflict).ShouldBe(1);
        (
            await db.SlotContractBooking.CountAsync(
                x =>
                    x.SlotContractId == contract.Id
                    && (
                        x.Booking.BookingStatusId == (int)BookingStatusEnum.Confirmed
                        || (x.Booking.BookingStatusId == (int)BookingStatusEnum.Pending && x.Booking.ExpiresAt > DateTime.UtcNow)
                    ),
                ct
            )
        ).ShouldBe(1);
        (await db.BookingAction.CountAsync(x => x.BookingId == id, ct)).ShouldBe(responses[0].IsSuccessStatusCode ? 2 : 1);
        await CleanupFacilities(db, facilityId);
    }

    [Theory]
    [InlineData(BookingStatusEnum.Pending)]
    [InlineData(BookingStatusEnum.Confirmed)]
    public async Task Restore_WhenPlacesTaken_FailsWithoutChangesOrAction(BookingStatusEnum status)
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await CreateBookingSetup(db);
        await AssignManagerRole(db, facilityId);
        slot.MaxBookings = 1;
        await db.SaveChangesAsync(ct);
        var id = await CreateStatusBooking(slot, contract);
        (await SetAdminStatus(facilityId, id, BookingStatusEnum.Cancelled)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await CreateStatusBooking(slot, contract);
        var before = await db.Booking.AsNoTracking().SingleAsync(x => x.Id == id, ct);
        var response = await SetAdminStatus(facilityId, id, status);
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync(ct)).ShouldContain("Not enough availability");
        var after = await db.Booking.AsNoTracking().SingleAsync(x => x.Id == id, ct);
        after.BookingStatusId.ShouldBe(before.BookingStatusId);
        after.ExpiresAt.ShouldBe(before.ExpiresAt);
        (await db.BookingAction.CountAsync(x => x.BookingId == id, ct)).ShouldBe(1);
        (await app.Client.PutAsJsonAsync("/booking/status", new { bookingId = id, status = BookingStatusEnum.Confirmed }, ct)).StatusCode.ShouldBe(
            HttpStatusCode.Conflict
        );
        await CleanupFacilities(db, facilityId);
    }
}
