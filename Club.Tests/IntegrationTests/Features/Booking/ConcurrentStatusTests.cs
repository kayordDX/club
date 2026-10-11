using System.Net.Http.Json;
using Club.Common.Enums;
using Club.Data;
using Club.Features.Booking.Create;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Features.Booking;

public partial class AdminBookingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Restore_CompetingWithRestoreOrEdit_SerializesCapacity(bool editing)
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await CreateBookingSetup(db);
        var (otherSlot, otherContract, otherFacilityId) = await CreateBookingSetup(db);
        otherSlot.FacilityId = facilityId;
        await db.SaveChangesAsync(ct);
        await AssignManagerRole(db, facilityId);
        var id = await CreateStatusBooking(slot, contract);
        (await SetAdminStatus(facilityId, id, BookingStatusEnum.Cancelled)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var competitorId = editing ? await CreateStatusBooking(otherSlot, otherContract) : await CreateStatusBooking(slot, contract);
        if (!editing)
            (await SetAdminStatus(facilityId, competitorId, BookingStatusEnum.Cancelled)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        slot.MaxBookings = 1;
        await db.SaveChangesAsync(ct);

        var restore = SetAdminStatus(facilityId, id, BookingStatusEnum.Pending);
        var competitor = editing
            ? app.Client.PutAsJsonAsync(
                $"/admin/facility/{facilityId}/booking/{competitorId}",
                new { bookings = new[] { Player(slot, contract) }, extras = Array.Empty<object>() },
                ct
            )
            : SetAdminStatus(facilityId, competitorId, BookingStatusEnum.Confirmed);
        var responses = await Task.WhenAll(restore, competitor);
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
        await CleanupFacilities(db, facilityId, otherFacilityId);
    }

    [Fact]
    public async Task CustomerStatusChange_PreservesOwnBooking_AndDoesNotCreateAdminHistory()
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await CreateBookingSetup(db);
        var id = await CreateStatusBooking(slot, contract);
        foreach (var status in new[] { BookingStatusEnum.Confirmed, BookingStatusEnum.Cancelled })
            (await app.Client.PutAsJsonAsync("/booking/status", new { bookingId = id, status }, ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var booking = await db.Booking.AsNoTracking().SingleAsync(x => x.Id == id, ct);
        booking.AmountPaid.ShouldBe(0m);
        booking.AmountOutstanding.ShouldBe(100m);
        booking.IsPaid.ShouldBeFalse();
        booking.FacilityId.ShouldBe(facilityId);
        (await db.SlotContractBooking.CountAsync(x => x.BookingId == id, ct)).ShouldBe(1);
        (await db.BookingAction.CountAsync(x => x.BookingId == id, ct)).ShouldBe(0);
        await CleanupFacilities(db, facilityId);
    }
}
