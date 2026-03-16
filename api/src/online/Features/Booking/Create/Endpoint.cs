using System.Data;
using Microsoft.EntityFrameworkCore;
using Online.Common;
using Online.Data;
using Online.Entities;

namespace Online.Features.Booking.Create;

public class Endpoint(AppDbContext dbContext) : Endpoint<BookingCreateRequest, BookingCreateResponse>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Post("/booking");
        Description(x => x.WithName("BookingCreate"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(BookingCreateRequest req, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var pendingCutoff = now.Subtract(BookingConstants.PendingTimeout);
        var currentUserId = Helpers.GetCurrentUserId(HttpContext);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct);

        var selectedContract = await _dbContext.SlotContract
            .Where(sc => sc.Id == req.SlotContractId)
            .Select(sc => new
            {
                sc.Id,
                sc.ContractId,
                sc.Price,
                sc.ValidationId,
                sc.CanPayLater,
                sc.Description
            })
            .FirstOrDefaultAsync(ct);

        if (selectedContract is null)
        {
            await transaction.RollbackAsync(ct);
            await Send.NotFoundAsync(ct);
            return;
        }

        var slotOptions = await _dbContext.Slot
            .Where(s => s.Id == req.SlotId || s.SlotGroupId == req.SlotId)
            .OrderBy(s => s.Id)
            .Select(s => new
            {
                s.Id,
                s.RequiresLogin,
                CanBookForGuests = s.SlotGroupId.HasValue
                    ? _dbContext.SlotGroup
                        .Where(sg => sg.Id == s.SlotGroupId)
                        .Select(sg => sg.CanBookForGuests)
                        .FirstOrDefault()
                    : false,
                MatchingContract = s.SlotContracts
                    .Where(sc =>
                        sc.ContractId == selectedContract.ContractId &&
                        sc.Price == selectedContract.Price &&
                        sc.ValidationId == selectedContract.ValidationId &&
                        sc.CanPayLater == selectedContract.CanPayLater &&
                        sc.Description == selectedContract.Description)
                    .Select(sc => new
                    {
                        sc.Id,
                        sc.Price
                    })
                    .FirstOrDefault(),
                HasActiveBooking = s.SlotBookings.Any(sb =>
                    sb.BookingStatus.Name == BookingConstants.ConfirmedStatus ||
                    (sb.BookingStatus.Name == BookingConstants.PendingStatus &&
                     sb.BookingStatusDate >= pendingCutoff))
            })
            .ToListAsync(ct);

        if (slotOptions.Count == 0 || slotOptions.All(x => x.MatchingContract is null))
        {
            await transaction.RollbackAsync(ct);
            await Send.NotFoundAsync(ct);
            return;
        }

        if (!currentUserId.HasValue && slotOptions.Any(x => x.RequiresLogin || !x.CanBookForGuests))
        {
            await transaction.RollbackAsync(ct);
            await Send.ForbiddenAsync(ct);
            return;
        }

        var availableSlots = slotOptions
            .Where(x => x.MatchingContract is not null && !x.HasActiveBooking)
            .Take(req.Quantity)
            .ToList();

        if (availableSlots.Count < req.Quantity)
        {
            await transaction.RollbackAsync(ct);
            await HttpContext.Response.SendAsync(new
            {
                message = "The selected slot is no longer available for the requested quantity."
            }, StatusCodes.Status409Conflict, cancellation: ct);
            return;
        }

        var pendingStatus = await _dbContext.BookingStatus
            .FirstOrDefaultAsync(x => x.Name == BookingConstants.PendingStatus, ct);

        if (pendingStatus is null)
        {
            pendingStatus = new BookingStatus { Name = BookingConstants.PendingStatus };
            await _dbContext.BookingStatus.AddAsync(pendingStatus, ct);
            await _dbContext.SaveChangesAsync(ct);
        }

        var email = req.Email.Trim();
        var bookings = availableSlots
            .Select(slot => new SlotBooking
            {
                SlotId = slot.Id,
                SlotContractId = slot.MatchingContract!.Id,
                BookingStatusId = pendingStatus.Id,
                BookingStatusDate = now,
                UserId = currentUserId,
                Email = email
            })
            .ToList();

        await _dbContext.SlotBooking.AddRangeAsync(bookings, ct);
        await _dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var response = new BookingCreateResponse
        {
            BookingIds = bookings.Select(x => x.Id).ToList(),
            SlotId = req.SlotId,
            Quantity = bookings.Count,
            Email = email,
            Status = BookingConstants.PendingStatus,
            TotalPrice = bookings.Sum(x => availableSlots
                .First(slot => slot.Id == x.SlotId)
                .MatchingContract!.Price),
            CreatedAt = now,
            ExpiresAt = now.Add(BookingConstants.PendingTimeout)
        };

        await HttpContext.Response.SendAsync(response, StatusCodes.Status201Created, cancellation: ct);
    }
}
