using Club.Common.Enums;
using Club.Data;
using Club.Entities;
using Club.Features.Booking.Create;

namespace Club.Features.Booking.Common;

public sealed record BookingCreationResult(int? BookingId, int StatusCode, string? Error)
{
    public bool Succeeded => BookingId.HasValue;
}

/// <summary>Shared transactional booking-creation rules, with owner and policy supplied by the authorised adapter.</summary>
public static class BookingCreationMutation
{
    public static async Task<BookingCreationResult> CreateAsync(
        AppDbContext db,
        BookingCreateRequest request,
        Guid? ownerId,
        int statusId,
        BookingCreationPolicy policy,
        CancellationToken ct
    )
    {
        if (statusId is not ((int)BookingStatusEnum.Pending) and not ((int)BookingStatusEnum.Confirmed))
            return new(null, 400, "Only Pending and Confirmed bookings can be created.");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var (selections, failure) = await BookingMutationValidation.ValidateAsync(
            db,
            request,
            ownerId,
            policy.ExpectedFacilityId,
            policy.AllowManagerContractBypass,
            policy.ExpectedFacilityId.HasValue,
            null,
            [],
            ct
        );
        if (failure is not null)
            return new(null, failure.StatusCode, failure.Message);
        var now = DateTime.UtcNow;
        var booking = new Entities.Booking
        {
            FacilityId = selections!.FacilityId,
            BookingStatusId = statusId,
            BookingStatusDate = now,
            IsPaid = false,
            AmountOutstanding = selections.Total,
            AmountPaid = 0,
            ExpiresAt = now.AddMinutes(Math.Max(1, policy.PendingTimeoutMinutes)),
            UserId = ownerId,
            GuestName = policy.GuestName,
            GuestEmail = policy.GuestEmail,
            GuestPhone = policy.GuestPhone,
        };
        await db.Booking.AddAsync(booking, ct);
        await db.SaveChangesAsync(ct);
        await db.SlotContractBooking.AddRangeAsync(
            request.Bookings.Select(x => new SlotContractBooking
            {
                SlotContractId = x.SlotContractId,
                BookingId = booking.Id,
                Name = x.Name,
                Email = x.Email,
                Cellphone = x.Cellphone,
            }),
            ct
        );
        foreach (var extra in selections.Extras)
            extra.BookingId = booking.Id;
        await db.ExtraBooking.AddRangeAsync(selections.Extras, ct);
        if (policy.ActingAdminId.HasValue)
            db.BookingAction.Add(
                new BookingAction
                {
                    BookingId = booking.Id,
                    ActingAdminId = policy.ActingAdminId.Value,
                    Kind = BookingActionKind.Created,
                    Timestamp = now,
                }
            );
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(booking.Id, 200, null);
    }
}
