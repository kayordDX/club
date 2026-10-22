using System.Net.Http.Json;
using System.Text.Json;
using Club.Common.Enums;
using Club.Data;
using Club.DTO;
using Club.Entities;
using Club.Features.Booking.Create;
using Club.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Features.Booking;

public partial class AdminBookingTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(40)]
    public async Task Expiry_PreservesBookingDetailsAcrossRepeatedRuns_WhileReleasingCapacity(int amountPaid)
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await CreateBookingSetup(db);
        await AssignManagerRole(db, facilityId);
        slot.MaxBookings = 2;
        await db.SaveChangesAsync(ct);

        var request = new BookingCreateRequest
        {
            Bookings =
            [
                Player(slot, contract),
                new BookingRequest
                {
                    SlotId = slot.Id,
                    SlotContractId = contract.Id,
                    Name = "Second player",
                    Email = "second@example.com",
                    Cellphone = "0987654321",
                },
            ],
        };
        var createdResponse = await app.Client.PostAsJsonAsync("/booking", request, ct);
        createdResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var created = await createdResponse.Content.ReadFromJsonAsync<BookingCreateResponse>(ct);
        var id = created!.Id;
        var booking = await db.Booking.SingleAsync(x => x.Id == id, ct);
        booking.AmountPaid = amountPaid;
        booking.AmountOutstanding = 220m - amountPaid;
        booking.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        var outletId = await db.Facility.Where(x => x.Id == facilityId).Select(x => x.OutletId).SingleAsync(ct);
        db.ExtraBooking.Add(
            new ExtraBooking
            {
                BookingId = id,
                Amount = 2,
                Extra = new Extra
                {
                    FacilityId = facilityId,
                    OutletId = outletId,
                    Name = "Hire equipment",
                    Code = "HIRE",
                    Price = 10m,
                },
            }
        );
        if (amountPaid > 0)
        {
            db.PaymentBooking.Add(
                new PaymentBooking
                {
                    BookingId = id,
                    Payment = new Club.Entities.Payment
                    {
                        Amount = amountPaid,
                        PaymentStatusId = (int)PaymentStatusEnum.Completed,
                        PaymentStatus = await db.PaymentStatus.SingleAsync(x => x.Id == (int)PaymentStatusEnum.Completed, ct),
                        PaymentStatusDate = DateTime.UtcNow,
                        PaymentTypeId = (int)PaymentTypeEnum.CreditCard,
                        TransactionId = Guid.NewGuid().ToString(),
                        ProviderName = "payfast",
                    },
                }
            );
        }
        await db.SaveChangesAsync(ct);

        async Task<BookingDTO> GetDetail(string url)
        {
            var response = await app.Client.GetAsync(url, ct);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            return (await response.Content.ReadFromJsonAsync<BookingDTO>(ct))!;
        }

        var before = await GetDetail($"/booking/{id}");
        before.SlotContractBookings.Count.ShouldBe(2);
        before.ExtraBookings.Count.ShouldBe(1);
        DateTime? firstExpiryDate = null;
        var job = new FunctionJob(db);
        for (var run = 0; run < 2; run++)
        {
            await job.ClearExpiredBookings(ct);
            var persisted = await db.Booking.AsNoTracking().SingleAsync(x => x.Id == id, ct);
            persisted.BookingStatusId.ShouldBe((int)BookingStatusEnum.Expired);
            firstExpiryDate ??= persisted.BookingStatusDate;
            persisted.BookingStatusDate.ShouldBe(firstExpiryDate.Value);
            persisted.FacilityId.ShouldBe(facilityId);
            persisted.ExpiresAt.ShouldBe(before.ExpiresAt);

            foreach (var url in new[] { $"/booking/{id}", $"/admin/facility/{facilityId}/booking/{id}" })
            {
                var detail = await GetDetail(url);
                detail.BookingStatusId.ShouldBe((int)BookingStatusEnum.Expired);
                detail.UserId.ShouldBe(before.UserId);
                detail.AmountPaid.ShouldBe((decimal)amountPaid);
                detail.AmountOutstanding.ShouldBe(220m - amountPaid);
                detail.IsPaid.ShouldBeFalse();
                JsonSerializer.Serialize(detail.SlotContractBookings).ShouldBe(JsonSerializer.Serialize(before.SlotContractBookings));
                JsonSerializer.Serialize(detail.ExtraBookings).ShouldBe(JsonSerializer.Serialize(before.ExtraBookings));
            }

            var listResponse = await app.Client.GetAsync($"/admin/facility/{facilityId}/booking?filters={Uri.EscapeDataString($"id == {id}")}", ct);
            listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
            var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
            var item = list.GetProperty("items").EnumerateArray().Single();
            item.GetProperty("playerCount").GetInt32().ShouldBe(2);
            item.GetProperty("slotStartDatetime").GetDateTime().ShouldBe(slot.StartDatetime, TimeSpan.FromMilliseconds(1));
            item.GetProperty("slotEndDatetime").GetDateTime().ShouldBe(slot.EndDatetime!.Value, TimeSpan.FromMilliseconds(1));
        }

        var payments = await db.PaymentBooking.AsNoTracking().Include(x => x.Payment).Where(x => x.BookingId == id).ToListAsync(ct);
        payments.Count.ShouldBe(amountPaid > 0 ? 1 : 0);
        payments.Sum(x => x.Payment.Amount).ShouldBe((decimal)amountPaid);
        var availableResponse = await app.Client.PostAsJsonAsync("/slot/available", new { id = slot.Id, slotCount = 2 }, ct);
        availableResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await availableResponse.Content.ReadFromJsonAsync<bool>(ct)).ShouldBeTrue();
        var replacementResponse = await app.Client.PostAsJsonAsync("/booking", request, ct);
        replacementResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        await CleanupFacilities(db, facilityId);
    }
}
