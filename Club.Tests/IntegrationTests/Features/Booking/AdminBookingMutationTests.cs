using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Club.Common.Enums;
using Club.Common.Payments.Provider.Payfast;
using Club.Data;
using Club.DTO;
using Club.Entities;
using Club.Features.Booking.Common;
using Club.Features.Booking.Create;
using Club.Services;
using IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BookingEntity = Club.Entities.Booking;
using SlotEntity = Club.Entities.Slot;

namespace IntegrationTests.Features.Booking;

[Collection("AppFixture collection")]
public class AdminBookingMutationTests(AppFixture app)
{
    private CancellationToken Ct => app.Context.CancellationToken;

    [Theory]
    [InlineData("contacts", true)]
    [InlineData("append", false)]
    [InlineData("reorder", false)]
    [InlineData("remove-first", false)]
    public async Task LegacyAllowance_IsPerOriginalPlayerIndex_NotAPool(string operation, bool succeeds)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, publicContract, facilityId) = await SetupAsync(db);
        var owner = await CustomerAsync(db);
        var legacy = await ContractAsync(db, slot, false, 25m);
        var membership = new UserContract
        {
            Contract = null!,
            User = null!,
            UserId = owner.Id,
            ContractId = legacy.ContractId,
            IsActive = true,
            StartDate = slot.StartDatetime.AddDays(-1),
        };
        db.UserContract.Add(membership);
        await db.SaveChangesAsync(Ct);
        var id = await CreateAsync(facilityId, owner.Id, [Player(slot, publicContract, "Public"), Player(slot, legacy, "Legacy")]);
        membership.IsActive = false;
        db.UserContract.Add(
            new UserContract
            {
                Contract = null!,
                User = null!,
                UserId = TestClaims.UserIdGuid,
                ContractId = legacy.ContractId,
                IsActive = true,
                StartDate = slot.StartDatetime.AddDays(-1),
            }
        );
        await db.SaveChangesAsync(Ct);
        var before = await ReadAsync(db, id);
        var players = new List<BookingRequest> { Player(slot, publicContract, "Public"), Player(slot, legacy, "Legacy") };
        switch (operation)
        {
            case "contacts":
                players[1].Name = "Changed contact";
                players[1].Email = "changed@example.com";
                players[1].Cellphone = "9999999999";
                break;
            case "append":
                players.Add(Player(slot, legacy, "New legacy player"));
                break;
            case "reorder":
                players.Reverse();
                break;
            case "remove-first":
                players.RemoveAt(0);
                break;
        }

        var response = await EditAsync(facilityId, id, players);

        response.StatusCode.ShouldBe(succeeds ? HttpStatusCode.NoContent : HttpStatusCode.BadRequest);
        if (!succeeds)
            await AssertUnchangedAsync(db, before);
        else
        {
            var persisted = await ReadAsync(db, id);
            persisted.SlotContractBookings.OrderBy(x => x.Id).Last().Name.ShouldBe("Changed contact");
            persisted.AmountOutstanding.ShouldBe(125m);
            await AssertActionAsync(db, id, BookingActionKind.Edited);
            foreach (var path in new[] { $"/booking/{id}", $"/admin/facility/{facilityId}/booking/{id}" })
            {
                var detail = await app.Client.GetAsync(path, Ct);
                detail.StatusCode.ShouldBe(HttpStatusCode.OK);
                var dto = (await detail.Content.ReadFromJsonAsync<BookingDTO>(Ct))!;
                dto.SlotContractBookings.Select(x => x.Id).ShouldBe(dto.SlotContractBookings.Select(x => x.Id).Order());
                dto.SlotContractBookings.Last().SlotContractId.ShouldBe(legacy.Id);
            }
        }
    }

    [Theory]
    [InlineData("inactive", false)]
    [InlineData("future", false)]
    [InlineData("expired", false)]
    [InlineData("valid", true)]
    public async Task ChangedContract_UsesOwnersActiveDatedMembership_NotActingAdmin(string membershipState, bool succeeds)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, original, facilityId) = await SetupAsync(db);
        var owner = await CustomerAsync(db);
        var memberContract = await ContractAsync(db, slot, false, 30m);
        var id = await CreateAsync(facilityId, owner.Id, [Player(slot, original)]);
        db.UserContract.Add(
            new UserContract
            {
                Contract = null!,
                User = null!,
                UserId = TestClaims.UserIdGuid,
                ContractId = memberContract.ContractId,
                IsActive = true,
                StartDate = slot.StartDatetime.AddDays(-1),
            }
        );
        db.UserContract.Add(
            new UserContract
            {
                Contract = null!,
                User = null!,
                UserId = owner.Id,
                ContractId = memberContract.ContractId,
                IsActive = membershipState != "inactive",
                StartDate = membershipState == "future" ? slot.StartDatetime.AddDays(1) : slot.StartDatetime.AddDays(-10),
                EndDate = membershipState == "expired" ? slot.StartDatetime.AddDays(-1) : null,
            }
        );
        await db.SaveChangesAsync(Ct);
        var before = await ReadAsync(db, id);

        var response = await EditAsync(facilityId, id, [Player(slot, memberContract)]);

        response.StatusCode.ShouldBe(succeeds ? HttpStatusCode.NoContent : HttpStatusCode.BadRequest);
        if (!succeeds)
            await AssertUnchangedAsync(db, before);
        else
        {
            (await ReadAsync(db, id)).AmountOutstanding.ShouldBe(30m);
            await AssertActionAsync(db, id, BookingActionKind.Edited);
        }
    }

    [Fact]
    public async Task Move_DoesNotCarryLegacyEligibilityToDestination_EvenForSameUnderlyingContract()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (source, _, facilityId) = await SetupAsync(db);
        var owner = await CustomerAsync(db);
        var legacy = await ContractAsync(db, source, false, 25m);
        var membership = new UserContract
        {
            Contract = null!,
            User = null!,
            UserId = owner.Id,
            ContractId = legacy.ContractId,
            IsActive = true,
            StartDate = source.StartDatetime.AddDays(-1),
        };
        db.UserContract.Add(membership);
        await db.SaveChangesAsync(Ct);
        var id = await CreateAsync(facilityId, owner.Id, [Player(source, legacy)]);
        var (destination, contract) = await DestinationAsync(db, source, legacy, 60m);
        membership.IsActive = false;
        await db.SaveChangesAsync(Ct);
        var before = await ReadAsync(db, id);

        (await EditAsync(facilityId, id, [Player(destination, contract)])).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await AssertUnchangedAsync(db, before);
        membership.IsActive = true;
        await db.SaveChangesAsync(Ct);
        (await EditAsync(facilityId, id, [Player(destination, contract)])).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var moved = await ReadAsync(db, id);
        moved.AmountOutstanding.ShouldBe(60m);
        moved.BookingStatusId.ShouldBe(before.BookingStatusId);
        moved.ExpiresAt.ShouldBe(before.ExpiresAt);
        await AssertActionAsync(db, id, BookingActionKind.Moved);
    }

    [Theory]
    [InlineData(BookingStatusEnum.Pending, false)]
    [InlineData(BookingStatusEnum.Pending, true)]
    [InlineData(BookingStatusEnum.Confirmed, true)]
    [InlineData(BookingStatusEnum.Cancelled, false)]
    [InlineData(BookingStatusEnum.Expired, false)]
    public async Task Move_PreservesIdentityOwnerStatusExpiryPlayersAndExtras_AndInactiveEditsDoNotReserve(BookingStatusEnum status, bool expired)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (source, sourceContract, facilityId) = await SetupAsync(db);
        var owner = await CustomerAsync(db);
        var id = await CreateAsync(facilityId, owner.Id, [Player(source, sourceContract)]);
        var (destination, destinationContract) = await DestinationAsync(db, source, sourceContract, 150m, 1);
        var extra = await ExtraAsync(db, facilityId);
        await db
            .Booking.Where(x => x.Id == id)
            .ExecuteUpdateAsync(
                x =>
                    x.SetProperty(b => b.BookingStatusId, (int)status)
                        .SetProperty(b => b.ExpiresAt, expired ? DateTime.UtcNow.AddDays(-1) : DateTime.UtcNow.AddHours(1)),
                Ct
            );
        var before = await ReadAsync(db, id);
        var active = status == BookingStatusEnum.Confirmed || (status == BookingStatusEnum.Pending && !expired);
        if (!active)
            await CreateAsync(facilityId, owner.Id, [Player(destination, destinationContract, "Occupant")]);
        var player = Player(destination, destinationContract);
        var started = DateTime.UtcNow;

        var response = await EditAsync(facilityId, id, [player], [new BookingExtraRequest { ExtraId = extra.Id, Amount = 2 }]);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var moved = await ReadAsync(db, id);
        moved.Id.ShouldBe(before.Id);
        moved.UserId.ShouldBe(owner.Id);
        moved.FacilityId.ShouldBe(facilityId);
        moved.BookingStatusId.ShouldBe(before.BookingStatusId);
        moved.BookingStatusDate.ShouldBe(before.BookingStatusDate);
        moved.ExpiresAt.ShouldBe(before.ExpiresAt);
        moved.CreatedBy.ShouldBe(before.CreatedBy);
        moved.AmountPaid.ShouldBe(0m);
        moved.AmountOutstanding.ShouldBe(170m);
        moved.IsPaid.ShouldBeFalse();
        var persistedPlayer = moved.SlotContractBookings.ShouldHaveSingleItem();
        persistedPlayer.Name.ShouldBe(player.Name);
        persistedPlayer.Email.ShouldBe(player.Email);
        persistedPlayer.Cellphone.ShouldBe(player.Cellphone);
        persistedPlayer.SlotContractId.ShouldBe(destinationContract.Id);
        moved.ExtraBookings.ShouldHaveSingleItem().Amount.ShouldBe(2);
        await AssertActionAsync(db, id, BookingActionKind.Moved, started);
        (await db.SlotContractBooking.ActiveReservations(DateTime.UtcNow).CountAsync(x => x.SlotContract.SlotId == source.Id, Ct)).ShouldBe(0);
        (await db.SlotContractBooking.ActiveReservations(DateTime.UtcNow).CountAsync(x => x.SlotContract.SlotId == destination.Id, Ct)).ShouldBe(1);
        var schedule = await app.Client.GetAsync($"/admin/facility/{facilityId}/slot?date={Uri.EscapeDataString(destination.StartDatetime.ToString("O"))}", Ct);
        schedule.StatusCode.ShouldBe(HttpStatusCode.OK);
        var slots = await schedule.Content.ReadFromJsonAsync<JsonElement>(Ct);
        slots.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == destination.Id).GetProperty("booked").GetInt32().ShouldBe(1);
        var list = await app.Client.GetAsync($"/admin/facility/{facilityId}/booking?filters={Uri.EscapeDataString($"id == {id}")}", Ct);
        list.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await list.Content.ReadAsStringAsync(Ct)).ShouldContain(destination.StartDatetime.ToString("yyyy-MM-dd"));
    }

    [Theory]
    [InlineData("mismatched-contract", HttpStatusCode.NotFound)]
    [InlineData("foreign-slot", HttpStatusCode.NotFound)]
    [InlineData("foreign-extra", HttpStatusCode.BadRequest)]
    [InlineData("unavailable-extra", HttpStatusCode.BadRequest)]
    [InlineData("offline-extra", HttpStatusCode.BadRequest)]
    [InlineData("negative-extra", HttpStatusCode.BadRequest)]
    [InlineData("mixed-extra-amounts", HttpStatusCode.BadRequest)]
    [InlineData("empty", HttpStatusCode.BadRequest)]
    [InlineData("full", HttpStatusCode.Conflict)]
    public async Task InvalidMove_IsAtomicAndDoesNotRecordSuccessfulAction(string invalid, HttpStatusCode expected)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (source, sourceContract, facilityId) = await SetupAsync(db);
        var owner = await CustomerAsync(db);
        var id = await CreateAsync(facilityId, owner.Id, [Player(source, sourceContract)]);
        var (destination, destinationContract) = await DestinationAsync(db, source, sourceContract, 200m, 1);
        var (foreign, foreignContract, foreignFacility) = await AdminBookingTests.CreateBookingSetup(db);
        var extra = await ExtraAsync(db, invalid == "foreign-extra" ? foreignFacility : facilityId);
        extra.IsAvailable = invalid != "unavailable-extra";
        extra.IsOnline = invalid != "offline-extra";
        await db.SaveChangesAsync(Ct);
        var player = Player(destination, destinationContract);
        if (invalid == "mismatched-contract")
            player.SlotContractId = sourceContract.Id;
        if (invalid == "foreign-slot")
            player = Player(foreign, foreignContract);
        if (invalid == "full")
            await CreateAsync(facilityId, owner.Id, [Player(destination, destinationContract, "Competitor")]);
        var before = await ReadAsync(db, id);
        var extras = new List<BookingExtraRequest>
        {
            new() { ExtraId = extra.Id, Amount = invalid == "negative-extra" ? -1 : 1 },
        };
        if (invalid == "mixed-extra-amounts")
        {
            extras[0].Amount = 2;
            extras.Add(new() { ExtraId = extra.Id, Amount = -1 });
        }

        var response = await EditAsync(facilityId, id, invalid == "empty" ? [] : [player], extras);

        response.StatusCode.ShouldBe(expected);
        await AssertUnchangedAsync(db, before);
    }

    [Fact]
    public async Task Edit_AllowsPastSourceChanges_ButRequiresFutureDestinationSlots()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (source, contract, facilityId) = await SetupAsync(db);
        var owner = await CustomerAsync(db);
        var id = await CreateAsync(facilityId, owner.Id, [Player(source, contract)]);
        var (destination, destinationContract) = await DestinationAsync(db, source, contract, 60m);
        await db.Slot.Where(x => x.Id == source.Id).ExecuteUpdateAsync(x => x.SetProperty(s => s.StartDatetime, DateTime.UtcNow.AddHours(-2)), Ct);
        await db.Slot.Where(x => x.Id == destination.Id).ExecuteUpdateAsync(x => x.SetProperty(s => s.StartDatetime, DateTime.UtcNow.AddHours(-1)), Ct);

        // A player kept on its original (now past) slot at the same position stays editable.
        var retained = Player(source, contract);
        retained.Name = "Past slot contact edit";
        (await EditAsync(facilityId, id, [retained])).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ReadAsync(db, id)).SlotContractBookings.ShouldHaveSingleItem().Name.ShouldBe("Past slot contact edit");
        await AssertActionAsync(db, id, BookingActionKind.Edited);

        // Moving to a past destination is rejected without recording a successful action.
        var move = await EditAsync(facilityId, id, [Player(destination, destinationContract)]);
        move.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await move.Content.ReadAsStringAsync(Ct)).ShouldContain("future available slots");
        var after = await ReadAsync(db, id);
        after.SlotContractBookings.ShouldHaveSingleItem().Name.ShouldBe("Past slot contact edit");
        (await db.BookingAction.CountAsync(x => x.BookingId == id, Ct)).ShouldBe(2);

        // Admins can still correct contracts and add eligible players within the historical source slot.
        var changedContract = await ContractAsync(db, source, true, 80m);
        (await EditAsync(facilityId, id, [Player(source, changedContract), Player(source, contract, "Added late")])).StatusCode.ShouldBe(
            HttpStatusCode.NoContent
        );
        after = await ReadAsync(db, id);
        after.SlotContractBookings.Count.ShouldBe(2);
        after.SlotContractBookings.Select(x => x.SlotContractId).ShouldContain(changedContract.Id);
        (await db.BookingAction.CountAsync(x => x.BookingId == id, Ct)).ShouldBe(3);
    }

    [Fact]
    public async Task SameSlotEdit_DeductsOwnPlacesOnlyOnSource_AndDoesNotGrantDestinationCapacity()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (source, contract, facilityId) = await SetupAsync(db);
        source.MaxBookings = 1;
        await db.SaveChangesAsync(Ct);
        var owner = await CustomerAsync(db);
        var id = await CreateAsync(facilityId, owner.Id, [Player(source, contract)]);
        (await EditAsync(facilityId, id, [Player(source, contract, "Edited")])).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var (destination, destinationContract) = await DestinationAsync(db, source, contract, 100m, 1);
        await CreateAsync(facilityId, owner.Id, [Player(destination, destinationContract)]);
        var before = await ReadAsync(db, id);
        var actionCount = await db.BookingAction.CountAsync(x => x.BookingId == id, Ct);

        (await EditAsync(facilityId, id, [Player(destination, destinationContract)])).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        await AssertUnchangedAsync(db, before, actionCount);
        (await EditAsync(facilityId, id, [Player(source, contract), Player(source, contract, "Extra player")])).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        await AssertUnchangedAsync(db, before, actionCount);
    }

    [Fact]
    public async Task MoveBetweenSameSetOfSlots_RecordsMoveForPerPlayerRedistribution()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (source, contract, facilityId) = await SetupAsync(db);
        var (other, otherContract) = await DestinationAsync(db, source, contract, 50m);
        var owner = await CustomerAsync(db);
        var id = await CreateAsync(facilityId, owner.Id, [Player(source, contract, "First"), Player(other, otherContract, "Second")]);

        (await EditAsync(facilityId, id, [Player(other, otherContract, "First"), Player(source, contract, "Second")])).StatusCode.ShouldBe(
            HttpStatusCode.NoContent
        );

        await AssertActionAsync(db, id, BookingActionKind.Moved);
    }

    [Theory]
    [InlineData("status", HttpStatusCode.BadRequest)]
    [InlineData("expiry", HttpStatusCode.BadRequest)]
    [InlineData("paid", HttpStatusCode.Conflict)]
    [InlineData("pending-payment", HttpStatusCode.Conflict)]
    [InlineData("owner", HttpStatusCode.Forbidden)]
    [InlineData("past-destination", HttpStatusCode.Conflict)]
    public async Task CustomerEdit_PreservesDistinctRestrictions(string restriction, HttpStatusCode expected)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await SetupAsync(db);
        var owner = restriction == "owner" ? (await CustomerAsync(db)).Id : TestClaims.UserIdGuid;
        var id = await CreateAsync(facilityId, owner, [Player(slot, contract)], BookingStatusEnum.Pending);
        var booking = await db.Booking.SingleAsync(x => x.Id == id, Ct);
        if (restriction == "status")
            booking.BookingStatusId = (int)BookingStatusEnum.Confirmed;
        if (restriction == "expiry")
            booking.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        if (restriction == "paid")
        {
            booking.AmountPaid = 20m;
            booking.AmountOutstanding = 80m;
        }
        if (restriction == "pending-payment")
            await PendingPaymentAsync(db, id, 20m);
        await db.SaveChangesAsync(Ct);
        var (pastDestination, pastContract) = await DestinationAsync(db, slot, contract, 60m);
        await db.Slot.Where(x => x.Id == pastDestination.Id).ExecuteUpdateAsync(x => x.SetProperty(s => s.StartDatetime, DateTime.UtcNow.AddHours(-1)), Ct);
        var before = await ReadAsync(db, id);
        var edited = restriction == "past-destination" ? Player(pastDestination, pastContract, "Changed") : Player(slot, contract, "Changed");

        var response = await app.Client.PutAsJsonAsync($"/booking/{id}", new BookingCreateRequest { Bookings = [edited] }, Ct);

        response.StatusCode.ShouldBe(expected);
        await AssertUnchangedAsync(db, before);
    }

    [Fact]
    public async Task Edit_RequiresFacilityManager_AndRejectsBookingFromAnotherFacility()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await SetupAsync(db);
        var owner = await CustomerAsync(db);
        var id = await CreateAsync(facilityId, owner.Id, [Player(slot, contract)]);
        var (_, _, foreignFacility) = await SetupAsync(db);
        var before = await ReadAsync(db, id);
        (await EditAsync(foreignFacility, id, [Player(slot, contract)])).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/admin/facility/{facilityId}/booking/{id}");
        request.Headers.Add("X-Test-User-Id", owner.Id.ToString());
        request.Content = JsonContent.Create(new BookingCreateRequest { Bookings = [Player(slot, contract)] });
        (await app.Client.SendAsync(request, Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        await AssertUnchangedAsync(db, before);
    }

    [Theory]
    [InlineData(40, 0, 30, false)]
    [InlineData(40, 50, 80, false)]
    [InlineData(40, 50, 90, true)]
    [InlineData(40, 0, 40, true)]
    public async Task Move_RepricesWithoutChangingSettledPayments_AndEnforcesPaidPlusPendingFloor(int paid, int pending, int price, bool succeeds)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (source, contract, facilityId) = await SetupAsync(db);
        var owner = await CustomerAsync(db);
        var id = await CreateAsync(facilityId, owner.Id, [Player(source, contract)]);
        var (destination, destinationContract) = await DestinationAsync(db, source, contract, price);
        var payment = await PendingPaymentAsync(db, id, paid);
        var booking = await db.Booking.SingleAsync(x => x.Id == id, Ct);
        await Club.Common.Payments.BookingPayments.ApplyAsync(db, booking, payment, Ct);
        if (pending > 0)
            await PendingPaymentAsync(db, id, pending);
        await db.SaveChangesAsync(Ct);
        var before = await ReadAsync(db, id);

        (await EditAsync(facilityId, id, [Player(destination, destinationContract)])).StatusCode.ShouldBe(
            succeeds ? HttpStatusCode.NoContent : HttpStatusCode.BadRequest
        );

        if (!succeeds)
            await AssertUnchangedAsync(db, before);
        else
        {
            var moved = await ReadAsync(db, id);
            moved.AmountPaid.ShouldBe((decimal)paid);
            moved.AmountOutstanding.ShouldBe((decimal)(price - paid));
            moved.IsPaid.ShouldBe(price == paid);
            moved.BookingStatusId.ShouldBe(before.BookingStatusId);
            moved.ExpiresAt.ShouldBe(before.ExpiresAt);
            await AssertActionAsync(db, id, BookingActionKind.Moved);
        }
        var settled = await db.Payment.AsNoTracking().SingleAsync(x => x.Id == payment.Id, Ct);
        settled.Amount.ShouldBe((decimal)paid);
        settled.PaymentStatusId.ShouldBe((int)PaymentStatusEnum.Partial);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("move")]
    [InlineData("restore")]
    public async Task Move_CompetingForLastPlace_SerializesWithOtherReservationMutations(string competitor)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (source, contract, facilityId) = await SetupAsync(db);
        var owner = await CustomerAsync(db);
        var (destination, destinationContract) = await DestinationAsync(db, source, contract, 100m, 1);
        var (otherSource, otherContract) = await DestinationAsync(db, source, contract, 100m);
        var id = await CreateAsync(facilityId, owner.Id, [Player(source, contract)]);
        var otherId =
            competitor == "create"
                ? 0
                : await CreateAsync(
                    facilityId,
                    owner.Id,
                    [competitor == "restore" ? Player(destination, destinationContract) : Player(otherSource, otherContract)]
                );
        if (competitor == "restore")
            (
                await app.Client.PutAsJsonAsync($"/admin/facility/{facilityId}/booking/{otherId}/status", new { status = BookingStatusEnum.Cancelled }, Ct)
            ).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var move = EditAsync(facilityId, id, [Player(destination, destinationContract)]);
        var other = competitor switch
        {
            "create" => app.Client.PostAsJsonAsync(
                $"/admin/facility/{facilityId}/booking",
                new
                {
                    userId = owner.Id,
                    bookings = new[] { Player(destination, destinationContract) },
                    extras = Array.Empty<object>(),
                },
                Ct
            ),
            "restore" => app.Client.PutAsJsonAsync($"/admin/facility/{facilityId}/booking/{otherId}/status", new { status = BookingStatusEnum.Confirmed }, Ct),
            _ => EditAsync(facilityId, otherId, [Player(destination, destinationContract)]),
        };
        var responses = await Task.WhenAll(move, other);

        responses.Count(x => x.IsSuccessStatusCode).ShouldBe(1);
        responses.Count(x => x.StatusCode == HttpStatusCode.Conflict).ShouldBe(1);
        (await db.SlotContractBooking.ActiveReservations(DateTime.UtcNow).CountAsync(x => x.SlotContract.SlotId == destination.Id, Ct)).ShouldBe(1);
        (await db.BookingAction.CountAsync(x => x.BookingId == id && x.Kind == BookingActionKind.Moved, Ct)).ShouldBe(responses[0].IsSuccessStatusCode ? 1 : 0);
        var persisted = await ReadAsync(db, id);
        persisted.SlotContractBookings.ShouldHaveSingleItem().SlotContractId.ShouldBe(responses[0].IsSuccessStatusCode ? destinationContract.Id : contract.Id);
    }

    [Fact]
    public async Task OppositeMoves_LockBothAffectedSlotsDeterministically()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (source, contract, facilityId) = await SetupAsync(db);
        source.MaxBookings = 2;
        await db.SaveChangesAsync(Ct);
        var owner = await CustomerAsync(db);
        var (destination, destinationContract) = await DestinationAsync(db, source, contract, 150m, 2);
        var first = await CreateAsync(facilityId, owner.Id, [Player(source, contract)]);
        var second = await CreateAsync(facilityId, owner.Id, [Player(destination, destinationContract)]);

        var responses = await Task.WhenAll(
            EditAsync(facilityId, first, [Player(destination, destinationContract)]),
            EditAsync(facilityId, second, [Player(source, contract)])
        );

        responses.ShouldAllBe(x => x.StatusCode == HttpStatusCode.NoContent);
        (await ReadAsync(db, first)).AmountOutstanding.ShouldBe(150m);
        (await ReadAsync(db, second)).AmountOutstanding.ShouldBe(100m);
        await AssertActionAsync(db, first, BookingActionKind.Moved);
        await AssertActionAsync(db, second, BookingActionKind.Moved);
    }

    [Fact]
    public async Task Move_CompetingWithPaymentInitiation_ProtectsFinancialCommitment()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (source, contract, facilityId) = await SetupAsync(db);
        var id = await CreateAsync(facilityId, TestClaims.UserIdGuid, [Player(source, contract)], BookingStatusEnum.Pending);
        var (destination, destinationContract) = await DestinationAsync(db, source, contract, 50m);
        await ProviderAsync(db, facilityId, scope.ServiceProvider.GetRequiredService<EncryptionService>());

        var responses = await Task.WhenAll(
            EditAsync(facilityId, id, [Player(destination, destinationContract)]),
            app.Client.PostAsJsonAsync(
                "/payment/initiate",
                new
                {
                    bookingId = id,
                    providerName = "payfast",
                    amount = 80m,
                },
                Ct
            )
        );

        responses.Count(x => x.IsSuccessStatusCode).ShouldBe(1);
        responses.Count(x => x.StatusCode == HttpStatusCode.BadRequest).ShouldBe(1);
        var persisted = await ReadAsync(db, id);
        var pending = await db
            .PaymentBooking.Where(x => x.BookingId == id && x.Payment.PaymentStatusId == (int)PaymentStatusEnum.Pending)
            .SumAsync(x => x.Payment.Amount, Ct);
        persisted.AmountOutstanding.ShouldBeGreaterThanOrEqualTo(pending);
        persisted.AmountPaid.ShouldBe(0m);
        persisted.AmountOutstanding.ShouldBe(responses[0].IsSuccessStatusCode ? 50m : 100m);
        pending.ShouldBe(responses[1].IsSuccessStatusCode ? 80m : 0m);
        (await db.BookingAction.CountAsync(x => x.BookingId == id && x.Kind == BookingActionKind.Moved, Ct)).ShouldBe(responses[0].IsSuccessStatusCode ? 1 : 0);
    }

    [Fact]
    public async Task Move_CompetingWithPaymentSettlement_PreservesPaidBalanceAndRejectsUnsafeReduction()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (source, contract, facilityId) = await SetupAsync(db);
        var id = await CreateAsync(facilityId, TestClaims.UserIdGuid, [Player(source, contract)], BookingStatusEnum.Pending);
        var (destination, destinationContract) = await DestinationAsync(db, source, contract, 50m);
        var payment = await PendingPaymentAsync(db, id, 80m);
        await ProviderAsync(db, facilityId, scope.ServiceProvider.GetRequiredService<EncryptionService>());
        await db.SaveChangesAsync(Ct);
        var before = await ReadAsync(db, id);
        var redirectClient = app.CreateClient(new FastEndpoints.Testing.ClientOptions { AllowAutoRedirect = false, HandleCookies = false });

        var responses = await Task.WhenAll(
            EditAsync(facilityId, id, [Player(destination, destinationContract)]),
            redirectClient.GetAsync($"/payment/result/payfast?merchantTransactionId={payment.TransactionId}", Ct)
        );

        responses[0].StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        responses[1].StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var after = await ReadAsync(db, id);
        after.AmountPaid.ShouldBe(80m);
        after.AmountOutstanding.ShouldBe(20m);
        after.IsPaid.ShouldBeFalse();
        after.SlotContractBookings.ShouldHaveSingleItem().SlotContractId.ShouldBe(contract.Id);
        after.ExpiresAt.ShouldBe(before.ExpiresAt);
        (await db.BookingAction.CountAsync(x => x.BookingId == id && x.Kind == BookingActionKind.Moved, Ct)).ShouldBe(0);
    }

    private async Task<(SlotEntity Slot, SlotContract Contract, int FacilityId)> SetupAsync(AppDbContext db)
    {
        var setup = await AdminBookingTests.CreateBookingSetup(db);
        await AdminBookingTests.AssignManagerRole(db, setup.FacilityId);
        return setup;
    }

    private async Task<User> CustomerAsync(AppDbContext db)
    {
        var owner = new User
        {
            Id = Guid.NewGuid(),
            UserName = $"mutation-{Guid.NewGuid()}",
            FirstName = "Booking",
            LastName = "Owner",
            Email = $"{Guid.NewGuid()}@example.com",
        };
        db.Users.Add(owner);
        await db.SaveChangesAsync(Ct);
        return owner;
    }

    private async Task<SlotContract> ContractAsync(AppDbContext db, SlotEntity slot, bool isPublic, decimal price)
    {
        var contract = new Contract
        {
            Name = $"Mutation contract {Guid.NewGuid()}",
            IsPublic = isPublic,
            IsActive = true,
        };
        var slotContract = new SlotContract
        {
            SlotId = slot.Id,
            Contract = contract,
            Price = price,
        };
        db.SlotContract.Add(slotContract);
        await db.SaveChangesAsync(Ct);
        return slotContract;
    }

    private async Task<(SlotEntity, SlotContract)> DestinationAsync(AppDbContext db, SlotEntity source, SlotContract contract, decimal price, int capacity = 4)
    {
        var slot = new SlotEntity
        {
            Id = Guid.NewGuid(),
            FacilityId = source.FacilityId,
            StartDatetime = source.StartDatetime.AddHours(2),
            EndDatetime = source.EndDatetime?.AddHours(2),
            MaxBookings = capacity,
        };
        var destinationContract = new SlotContract
        {
            Slot = slot,
            ContractId = contract.ContractId,
            Price = price,
        };
        db.SlotContract.Add(destinationContract);
        await db.SaveChangesAsync(Ct);
        return (slot, destinationContract);
    }

    private async Task<Extra> ExtraAsync(AppDbContext db, int facilityId)
    {
        var extra = new Extra
        {
            FacilityId = facilityId,
            OutletId = await db.Facility.Where(x => x.Id == facilityId).Select(x => x.OutletId).SingleAsync(Ct),
            Name = "Move extra",
            Code = Guid.NewGuid().ToString(),
            Price = 10m,
            IsAvailable = true,
            IsOnline = true,
        };
        db.Extra.Add(extra);
        await db.SaveChangesAsync(Ct);
        return extra;
    }

    private static BookingRequest Player(SlotEntity slot, SlotContract contract, string name = "Retained player") =>
        new()
        {
            SlotId = slot.Id,
            SlotContractId = contract.Id,
            Name = name,
            Email = "player@example.com",
            Cellphone = "0123456789",
        };

    private async Task<int> CreateAsync(int facilityId, Guid owner, List<BookingRequest> players, BookingStatusEnum status = BookingStatusEnum.Confirmed)
    {
        var response = await app.Client.PostAsJsonAsync(
            $"/admin/facility/{facilityId}/booking",
            new
            {
                userId = owner,
                bookingStatusId = (int)status,
                bookings = players,
                extras = Array.Empty<object>(),
            },
            Ct
        );
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        return (await response.Content.ReadFromJsonAsync<BookingCreateResponse>(Ct))!.Id;
    }

    private Task<HttpResponseMessage> EditAsync(int facilityId, int id, List<BookingRequest> players, List<BookingExtraRequest>? extras = null) =>
        app.Client.PutAsJsonAsync($"/admin/facility/{facilityId}/booking/{id}", new BookingCreateRequest { Bookings = players, Extras = extras ?? [] }, Ct);

    private Task<BookingEntity> ReadAsync(AppDbContext db, int id) =>
        db.Booking.AsNoTracking().Include(x => x.SlotContractBookings).Include(x => x.ExtraBookings).SingleAsync(x => x.Id == id, Ct);

    private async Task AssertUnchangedAsync(AppDbContext db, BookingEntity before, int actions = 1)
    {
        var after = await ReadAsync(db, before.Id);
        after.FacilityId.ShouldBe(before.FacilityId);
        after.UserId.ShouldBe(before.UserId);
        after.BookingStatusId.ShouldBe(before.BookingStatusId);
        after.BookingStatusDate.ShouldBe(before.BookingStatusDate);
        after.ExpiresAt.ShouldBe(before.ExpiresAt);
        after.AmountOutstanding.ShouldBe(before.AmountOutstanding);
        after.AmountPaid.ShouldBe(before.AmountPaid);
        after.IsPaid.ShouldBe(before.IsPaid);
        after
            .SlotContractBookings.OrderBy(x => x.Id)
            .Select(x => (x.Id, x.SlotContractId, x.Name, x.Email, x.Cellphone))
            .ShouldBe(before.SlotContractBookings.OrderBy(x => x.Id).Select(x => (x.Id, x.SlotContractId, x.Name, x.Email, x.Cellphone)));
        after
            .ExtraBookings.OrderBy(x => x.ExtraId)
            .Select(x => (x.ExtraId, x.Amount))
            .ShouldBe(before.ExtraBookings.OrderBy(x => x.ExtraId).Select(x => (x.ExtraId, x.Amount)));
        (await db.BookingAction.CountAsync(x => x.BookingId == before.Id, Ct)).ShouldBe(actions);
    }

    private async Task AssertActionAsync(AppDbContext db, int id, BookingActionKind kind, DateTime? started = null)
    {
        var action = await db.BookingAction.SingleAsync(x => x.BookingId == id && x.Kind != BookingActionKind.Created, Ct);
        action.Kind.ShouldBe(kind);
        action.ActingAdminId.ShouldBe(TestClaims.UserIdGuid);
        action.Timestamp.ShouldBeGreaterThanOrEqualTo(started ?? DateTime.UtcNow.AddMinutes(-2));
        action.Timestamp.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    private async Task<Club.Entities.Payment> PendingPaymentAsync(AppDbContext db, int bookingId, decimal amount)
    {
        var payment = new Club.Entities.Payment
        {
            Amount = amount,
            PaymentStatusId = (int)PaymentStatusEnum.Pending,
            PaymentStatus = await db.PaymentStatus.SingleAsync(x => x.Id == (int)PaymentStatusEnum.Pending, Ct),
            PaymentTypeId = (int)PaymentTypeEnum.CreditCard,
            PaymentStatusDate = DateTime.UtcNow,
            TransactionId = Guid.NewGuid().ToString(),
            ProviderName = "payfast",
        };
        db.PaymentBooking.Add(new PaymentBooking { BookingId = bookingId, Payment = payment });
        await db.SaveChangesAsync(Ct);
        return payment;
    }

    private async Task ProviderAsync(AppDbContext db, int facilityId, EncryptionService encryption)
    {
        var options = new PayfastOptions
        {
            MerchantId = "10016644",
            MerchantKey = "g9xjpawq6f6pr",
            Passphrase = "jt7NOE43FZPn",
            BaseUrl = "https://sandbox.payfast.co.za/eng/process",
            ReturnUrl = "http://localhost:5173/payment/success",
            CancelUrl = "http://localhost:5173/payment/cancelled",
            NotifyUrl = "http://localhost:5000/payment/result/payfast",
        };
        var iv = encryption.GenerateIV();
        db.PaymentProviderConfig.Add(
            new PaymentProviderConfig
            {
                FacilityId = facilityId,
                ProviderKey = PayfastOptions.Key,
                Type = PaymentProviderType.Payfast,
                Iv = iv,
                EncryptedSettings = encryption.Encrypt(JsonSerializer.Serialize(options), iv),
                Enabled = true,
            }
        );
        await db.SaveChangesAsync(Ct);
    }
}
