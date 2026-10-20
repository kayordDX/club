using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Club.Common.Enums;
using Club.Data;
using Club.Entities;
using Club.Features.Booking.Common;
using Club.Services;
using IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Features.Booking;

[Collection("AppFixture collection")]
public class AdminBookingCreationTests(AppFixture app)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CustomerLookup_ReturnsOnlySelectionData_AndNoMatchDoesNotCreateAccounts()
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (_, _, facilityId) = await AdminBookingTests.CreateBookingSetup(db);
        await AdminBookingTests.AssignManagerRole(db, facilityId);
        var owner = await CreateCustomerAsync(db, ct);
        var keycloak = scope.ServiceProvider.GetRequiredService<FakeCustomKeycloakService>();
        var accountCreatesBefore = keycloak.CreateUserCalls;

        var response = await app.Client.GetAsync($"/admin/facility/{facilityId}/booking/customer/search?query={Uri.EscapeDataString(owner.Email!)}", ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        result.RootElement.GetProperty("userId").GetGuid().ShouldBe(owner.Id);
        result.RootElement.GetProperty("email").GetString().ShouldBe(owner.Email);
        string[] selectionFields = ["userId", "firstName", "lastName", "email", "phoneNumber"];
        result.RootElement.EnumerateObject().Select(x => x.Name).ShouldAllBe(name => selectionFields.Contains(name));

        var unknown = await app.Client.GetAsync($"/admin/facility/{facilityId}/booking/customer/search?query=absent-{Guid.NewGuid()}%40example.com", ct);
        unknown.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await unknown.Content.ReadAsStringAsync(ct);
        (string.IsNullOrWhiteSpace(body) || body == "null").ShouldBeTrue();
        keycloak.CreateUserCalls.ShouldBe(accountCreatesBefore);
        (await db.Booking.AnyAsync(x => x.FacilityId == facilityId, ct)).ShouldBeFalse();
    }

    [Fact]
    public async Task Create_DefaultsToConfirmedUnpaid_OwnsForCustomer_AndRecordsActingAdmin()
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await AdminBookingTests.CreateBookingSetup(db);
        await AdminBookingTests.AssignManagerRole(db, facilityId);
        var owner = await CreateCustomerAsync(db, ct);

        var response = await CreateAsync(facilityId, owner.Id, slot.Id, contract.Id, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var bookingId = payload.RootElement.GetProperty("id").GetInt32();
        var booking = await db.Booking.AsNoTracking().SingleAsync(x => x.Id == bookingId, ct);
        booking.UserId.ShouldBe(owner.Id);
        booking.FacilityId.ShouldBe(facilityId);
        booking.BookingStatusId.ShouldBe((int)BookingStatusEnum.Confirmed);
        booking.IsPaid.ShouldBeFalse();
        booking.AmountPaid.ShouldBe(0m);
        booking.AmountOutstanding.ShouldBe(contract.Price);
        var action = await db.BookingAction.SingleAsync(x => x.BookingId == bookingId, ct);
        action.ActingAdminId.ShouldBe(TestClaims.UserIdGuid);
        action.Kind.ShouldBe(BookingActionKind.Created);
        action.Timestamp.ShouldBe(booking.BookingStatusDate);
        action.Timestamp.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-2));
        action.Timestamp.ShouldBeLessThan(DateTime.UtcNow.AddMinutes(1));

        await db.Booking.Where(x => x.Id == bookingId).ExecuteUpdateAsync(x => x.SetProperty(b => b.ExpiresAt, DateTime.UtcNow.AddDays(-1)), ct);
        (await db.Booking.AsNoTracking().SingleAsync(x => x.Id == bookingId, ct)).BookingStatusId.ShouldBe((int)BookingStatusEnum.Confirmed);
        (await db.SlotContractBooking.ActiveReservations(DateTime.UtcNow).CountAsync(x => x.SlotContract.SlotId == slot.Id, ct)).ShouldBe(1);

        var ownerBookings = await GetAsAsync(owner.Id, $"/booking/user?limit=100", ct);
        ownerBookings.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ownerBookings.Content.ReadAsStringAsync(ct)).ShouldContain($"\"id\":{bookingId}");
        var actorBookings = await GetAsAsync(TestClaims.UserIdGuid, "/booking/user?limit=100", ct);
        (await actorBookings.Content.ReadAsStringAsync(ct)).ShouldNotContain($"\"id\":{bookingId}");
    }

    [Fact]
    public async Task Create_ExplicitPendingUsesNormalExpiry_AndRejectsUnsupportedStatusesWithoutWrites()
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await AdminBookingTests.CreateBookingSetup(db);
        await AdminBookingTests.AssignManagerRole(db, facilityId);
        var owner = await CreateCustomerAsync(db, ct);
        var timeout = scope
            .ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Club.Common.Config.AppConfig>>()
            .Value.PendingTimeoutMinutes;
        var before = DateTime.UtcNow;

        var pending = await CreateAsync(facilityId, owner.Id, slot.Id, contract.Id, ct, (int)BookingStatusEnum.Pending);
        pending.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var payload = JsonDocument.Parse(await pending.Content.ReadAsStringAsync(ct));
        var id = payload.RootElement.GetProperty("id").GetInt32();
        var booking = await db.Booking.AsNoTracking().SingleAsync(x => x.Id == id, ct);
        booking.BookingStatusId.ShouldBe((int)BookingStatusEnum.Pending);
        booking.ExpiresAt.ShouldBeGreaterThan(before.AddMinutes(Math.Max(1, timeout) - 1));
        booking.ExpiresAt.ShouldBeLessThan(before.AddMinutes(Math.Max(1, timeout) + 2));
        (await db.BookingAction.SingleAsync(x => x.BookingId == id, ct)).Kind.ShouldBe(BookingActionKind.Created);

        var bookingsBefore = await db.Booking.CountAsync(x => x.FacilityId == facilityId, ct);
        var actionsBefore = await db.BookingAction.CountAsync(x => x.Booking.FacilityId == facilityId, ct);
        var invalid = await CreateAsync(facilityId, owner.Id, slot.Id, contract.Id, ct, (int)BookingStatusEnum.Cancelled);
        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await db.Booking.CountAsync(x => x.FacilityId == facilityId, ct)).ShouldBe(bookingsBefore);
        (await db.BookingAction.CountAsync(x => x.Booking.FacilityId == facilityId, ct)).ShouldBe(actionsBefore);
    }

    [Fact]
    public async Task CustomerSearchAndCreate_SynchronisesExistingKeycloakAccountWithoutCreatingAnAccount()
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await AdminBookingTests.CreateBookingSetup(db);
        await AdminBookingTests.AssignManagerRole(db, facilityId);
        var accountId = Guid.NewGuid();
        var keycloak = scope.ServiceProvider.GetRequiredService<FakeCustomKeycloakService>();
        keycloak.AddUser(new KeycloakUserResult(accountId, "Existing", "Customer", $"{accountId}@example.com", "5551234567"));

        var search = await app.Client.GetAsync(
            $"/admin/facility/{facilityId}/booking/customer/search?query={Uri.EscapeDataString($"{accountId}@example.com")}",
            ct
        );
        search.StatusCode.ShouldBe(HttpStatusCode.OK);
        using (var result = JsonDocument.Parse(await search.Content.ReadAsStringAsync(ct)))
        {
            result.RootElement.GetProperty("userId").GetGuid().ShouldBe(accountId);
            result.RootElement.GetProperty("firstName").GetString().ShouldBe("Existing");
        }
        (await db.Users.AnyAsync(x => x.Id == accountId, ct)).ShouldBeFalse();
        var createsBefore = keycloak.CreateUserCalls;

        var create = await CreateAsync(facilityId, accountId, slot.Id, contract.Id, ct);
        create.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await db.Users.AnyAsync(x => x.Id == accountId, ct)).ShouldBeTrue();
        (await db.Booking.AsNoTracking().AnyAsync(x => x.UserId == accountId && x.FacilityId == facilityId, ct)).ShouldBeTrue();
        keycloak.CreateUserCalls.ShouldBe(createsBefore);
    }

    [Fact]
    public async Task OptionsAndCreation_UseOwnersDatedMembership_NotManagerMembership()
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, publicContract, facilityId) = await AdminBookingTests.CreateBookingSetup(db);
        await AdminBookingTests.AssignManagerRole(db, facilityId);
        var owner = await CreateCustomerAsync(db, ct);
        var privateContract = new Contract
        {
            Name = $"Member only {Guid.NewGuid()}",
            IsPublic = false,
            IsActive = true,
            StartDate = slot.StartDatetime.AddYears(-1),
            EndDate = slot.StartDatetime.AddYears(1),
        };
        db.Contract.Add(privateContract);
        var facility = await db.Facility.SingleAsync(x => x.Id == facilityId, ct);
        db.ContractFacility.Add(new ContractFacility { Contract = privateContract, Facility = facility });
        await db.SaveChangesAsync(ct);
        var privateSlotContract = new SlotContract
        {
            SlotId = slot.Id,
            Slot = slot,
            Contract = privateContract,
            Price = 25m,
        };
        db.SlotContract.Add(privateSlotContract);
        var managerMembership = new UserContract
        {
            Contract = privateContract,
            UserId = TestClaims.UserIdGuid,
            User = await db.Users.SingleAsync(x => x.Id == TestClaims.UserIdGuid, ct),
            StartDate = slot.StartDatetime.AddDays(-1),
            EndDate = slot.StartDatetime.AddDays(1),
            IsActive = true,
        };
        db.UserContract.Add(managerMembership);
        await db.SaveChangesAsync(ct);

        var options = await app.Client.GetAsync($"/admin/facility/{facilityId}/booking/options?slotId={slot.Id}&userId={owner.Id}", ct);
        options.StatusCode.ShouldBe(HttpStatusCode.OK);
        using (var result = JsonDocument.Parse(await options.Content.ReadAsStringAsync(ct)))
        {
            var contracts = result.RootElement.GetProperty("contracts").EnumerateArray().ToArray();
            contracts.Any(x => x.GetProperty("id").GetInt32() == publicContract.Id).ShouldBeTrue();
            contracts.Any(x => x.GetProperty("id").GetInt32() == privateSlotContract.Id).ShouldBeFalse();
        }
        var denied = await CreateAsync(facilityId, owner.Id, slot.Id, privateSlotContract.Id, ct);
        denied.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await db.Booking.AnyAsync(x => x.UserId == owner.Id && x.FacilityId == facilityId, ct)).ShouldBeFalse();

        var ownerMembership = new UserContract
        {
            Contract = privateContract,
            UserId = owner.Id,
            User = owner,
            StartDate = slot.StartDatetime.Date,
            EndDate = slot.StartDatetime.Date,
            IsActive = true,
        };
        db.UserContract.Add(ownerMembership);
        await db.SaveChangesAsync(ct);
        var nowEligible = await app.Client.GetAsync($"/admin/facility/{facilityId}/booking/options?slotId={slot.Id}&userId={owner.Id}", ct);
        using (var result = JsonDocument.Parse(await nowEligible.Content.ReadAsStringAsync(ct)))
            result.RootElement.GetProperty("contracts").EnumerateArray().Any(x => x.GetProperty("id").GetInt32() == privateSlotContract.Id).ShouldBeTrue();
        var accepted = await CreateAsync(facilityId, owner.Id, slot.Id, privateSlotContract.Id, ct);
        accepted.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("not-started")]
    [InlineData("expired")]
    public async Task OptionsAndCreation_RejectInactiveOrOutOfDateOwnerMemberships(string state)
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, _, facilityId) = await AdminBookingTests.CreateBookingSetup(db);
        await AdminBookingTests.AssignManagerRole(db, facilityId);
        var owner = await CreateCustomerAsync(db, ct);
        var contract = new Contract
        {
            Name = $"Dated membership {Guid.NewGuid()}",
            IsPublic = false,
            IsActive = true,
        };
        db.Contract.Add(contract);
        db.ContractFacility.Add(new ContractFacility { Contract = contract, Facility = await db.Facility.SingleAsync(x => x.Id == facilityId, ct) });
        await db.SaveChangesAsync(ct);
        var slotContract = new SlotContract
        {
            Slot = slot,
            SlotId = slot.Id,
            Contract = contract,
            ContractId = contract.Id,
            Price = 20m,
        };
        db.SlotContract.Add(slotContract);
        var start = slot.StartDatetime.Date;
        var membership = new UserContract
        {
            Contract = contract,
            User = owner,
            UserId = owner.Id,
            IsActive = state != "inactive",
            StartDate = state == "not-started" ? start.AddDays(1) : start.AddDays(-1),
            EndDate = state == "expired" ? start.AddDays(-1) : start.AddDays(1),
        };
        db.UserContract.Add(membership);
        await db.SaveChangesAsync(ct);

        var options = await app.Client.GetAsync($"/admin/facility/{facilityId}/booking/options?slotId={slot.Id}&userId={owner.Id}", ct);
        options.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var optionsJson = JsonDocument.Parse(await options.Content.ReadAsStringAsync(ct));
        optionsJson.RootElement.GetProperty("contracts").EnumerateArray().Any(x => x.GetProperty("id").GetInt32() == slotContract.Id).ShouldBeFalse();
        (await CreateAsync(facilityId, owner.Id, slot.Id, slotContract.Id, ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await db.Booking.AnyAsync(x => x.UserId == owner.Id && x.FacilityId == facilityId, ct)).ShouldBeFalse();
    }

    [Fact]
    public async Task SearchOptionsAndCreate_EnforceManagerFacilityScopeAndRejectUnknownOwner()
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await AdminBookingTests.CreateBookingSetup(db);
        var (_, _, otherFacilityId) = await AdminBookingTests.CreateBookingSetup(db);
        await AdminBookingTests.AssignManagerRole(db, facilityId);
        var owner = await CreateCustomerAsync(db, ct);

        (
            await app.Client.GetAsync($"/admin/facility/{otherFacilityId}/booking/customer/search?query={Uri.EscapeDataString(owner.Email!)}", ct)
        ).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await app.Client.GetAsync($"/admin/facility/{otherFacilityId}/booking/options?slotId={slot.Id}&userId={owner.Id}", ct)).StatusCode.ShouldBe(
            HttpStatusCode.Forbidden
        );
        (await CreateAsync(otherFacilityId, owner.Id, slot.Id, contract.Id, ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var unknownOwner = Guid.NewGuid();
        var before = await db.Booking.CountAsync(x => x.FacilityId == facilityId, ct);
        (await CreateAsync(facilityId, unknownOwner, slot.Id, contract.Id, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await db.Booking.CountAsync(x => x.FacilityId == facilityId, ct)).ShouldBe(before);
        (await db.BookingAction.CountAsync(x => x.Booking.FacilityId == facilityId, ct)).ShouldBe(0);
    }

    [Fact]
    public async Task Create_CrossFacilitySlotOrExtraAndUnauthorisedManagerFailWithoutPartialBookingOrAction()
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await AdminBookingTests.CreateBookingSetup(db);
        var (otherSlot, otherContract, otherFacilityId) = await AdminBookingTests.CreateBookingSetup(db);
        await AdminBookingTests.AssignManagerRole(db, facilityId);
        var owner = await CreateCustomerAsync(db, ct);
        var noManager = await CreateAsync(otherFacilityId, owner.Id, otherSlot.Id, otherContract.Id, ct);
        noManager.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var crossFacility = await PostAsync(
            $"/admin/facility/{facilityId}/booking",
            new
            {
                userId = owner.Id,
                bookings = new[]
                {
                    new
                    {
                        slotId = slot.Id,
                        slotContractId = contract.Id,
                        name = "Valid player",
                        email = (string?)null,
                        cellphone = (string?)null,
                    },
                    new
                    {
                        slotId = otherSlot.Id,
                        slotContractId = otherContract.Id,
                        name = "Foreign player",
                        email = (string?)null,
                        cellphone = (string?)null,
                    },
                },
                extras = Array.Empty<object>(),
            },
            ct
        );
        crossFacility.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var facility = await db.Facility.SingleAsync(x => x.Id == facilityId, ct);
        var otherExtra = new Extra
        {
            FacilityId = otherFacilityId,
            OutletId = facility.OutletId,
            Name = "Foreign",
            Code = $"F-{Guid.NewGuid()}",
            Price = 5m,
            IsAvailable = true,
            IsOnline = true,
        };
        db.Extra.Add(otherExtra);
        await db.SaveChangesAsync(ct);
        var wrongExtra = await PostAsync(
            $"/admin/facility/{facilityId}/booking",
            new
            {
                userId = owner.Id,
                bookings = new[]
                {
                    new
                    {
                        slotId = slot.Id,
                        slotContractId = contract.Id,
                        name = "Valid player",
                        email = (string?)null,
                        cellphone = (string?)null,
                    },
                },
                extras = new[] { new { extraId = otherExtra.Id, amount = 1 } },
            },
            ct
        );
        wrongExtra.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await db.Booking.AnyAsync(x => x.UserId == owner.Id && x.FacilityId == facilityId, ct)).ShouldBeFalse();
        (await db.BookingAction.AnyAsync(x => x.Booking.UserId == owner.Id && x.Booking.FacilityId == facilityId, ct)).ShouldBeFalse();
    }

    [Fact]
    public async Task ConcurrentCreationForLastPlace_AllowsOnlyOneRequest()
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await AdminBookingTests.CreateBookingSetup(db);
        await AdminBookingTests.AssignManagerRole(db, facilityId);
        slot.MaxBookings = 1;
        await db.SaveChangesAsync(ct);
        var firstOwner = await CreateCustomerAsync(db, ct);
        var secondOwner = await CreateCustomerAsync(db, ct);

        var first = CreateAsync(facilityId, firstOwner.Id, slot.Id, contract.Id, ct);
        var second = CreateAsync(facilityId, secondOwner.Id, slot.Id, contract.Id, ct);
        var responses = await Task.WhenAll(first, second);

        responses.Count(x => x.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        responses.Count(x => x.StatusCode == HttpStatusCode.Conflict).ShouldBe(1);
        (await db.SlotContractBooking.ActiveReservations(DateTime.UtcNow).CountAsync(x => x.SlotContract.SlotId == slot.Id, ct)).ShouldBe(1);
        (await db.BookingAction.CountAsync(x => x.Booking.FacilityId == facilityId, ct)).ShouldBe(1);
    }

    [Fact]
    public async Task GuestBooking_HasGuestOwnerWithoutAccount_AndSupportsAdminDetailsEditingAndPaymentOnly()
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await AdminBookingTests.CreateBookingSetup(db);
        await AdminBookingTests.AssignManagerRole(db, facilityId);
        var keycloak = scope.ServiceProvider.GetRequiredService<FakeCustomKeycloakService>();
        var usersBefore = await db.Users.CountAsync(ct);
        var createsBefore = keycloak.CreateUserCalls;
        var body = new
        {
            guestName = "  Guest Owner  ",
            guestEmail = "guest@example.com",
            guestPhone = "0821234567",
            bookings = new[]
            {
                new
                {
                    slotId = slot.Id,
                    slotContractId = contract.Id,
                    name = "Guest Player",
                },
            },
            extras = Array.Empty<object>(),
        };
        var response = await PostAsync($"/admin/facility/{facilityId}/booking", body, ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var id = payload.RootElement.GetProperty("id").GetInt32();
        var booking = await db.Booking.AsNoTracking().SingleAsync(x => x.Id == id, ct);
        booking.UserId.ShouldBeNull();
        booking.GuestName.ShouldBe("Guest Owner");
        booking.GuestEmail.ShouldBe(body.guestEmail);
        booking.GuestPhone.ShouldBe(body.guestPhone);
        booking.BookingStatusId.ShouldBe((int)BookingStatusEnum.Confirmed);
        booking.AmountOutstanding.ShouldBe(contract.Price);
        (await db.BookingAction.SingleAsync(x => x.BookingId == id, ct)).ActingAdminId.ShouldBe(TestClaims.UserIdGuid);
        (await db.Users.CountAsync(ct)).ShouldBe(usersBefore);
        keycloak.CreateUserCalls.ShouldBe(createsBefore);

        var details = await app.Client.GetAsync($"/admin/facility/{facilityId}/booking/{id}", ct);
        details.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await details.Content.ReadAsStringAsync(ct)).ShouldContain("\"guestName\":\"Guest Owner\"");
        var list = await app.Client.GetAsync($"/admin/facility/{facilityId}/booking?limit=100", ct);
        (await list.Content.ReadAsStringAsync(ct)).ShouldContain("\"customerName\":\"Guest Owner\"");
        var options = await app.Client.GetAsync($"/admin/facility/{facilityId}/booking/options?slotId={slot.Id}", ct);
        options.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var optionJson = JsonDocument.Parse(await options.Content.ReadAsStringAsync(ct));
        optionJson.RootElement.GetProperty("contracts").EnumerateArray().Any(x => x.GetProperty("id").GetInt32() == contract.Id).ShouldBeTrue();

        using var edit = new HttpRequestMessage(HttpMethod.Put, $"/admin/facility/{facilityId}/booking/{id}") { Content = JsonContent.Create(body) };
        (await app.Client.SendAsync(edit, ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        foreach (var path in new[] { $"/booking/{id}", $"/booking/{id}/path", $"/payment/booking/{id}", $"/payment/booking/{id}/vouchers" })
            (await GetAsAsync(TestClaims.UserIdGuid, path, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync("/payment/initiate", new { bookingId = id, providerName = "payfast" }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var payment = await PostAsync($"/admin/facility/{facilityId}/booking/{id}/payment/accept", new { amount = contract.Price }, ct);
        payment.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await db.Booking.AsNoTracking().SingleAsync(x => x.Id == id, ct)).IsPaid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("blank")]
    [InlineData("both")]
    [InlineData("invalid-email")]
    [InlineData("empty-user")]
    [InlineData("long-name")]
    public async Task GuestBooking_RejectsInvalidOrAmbiguousOwnerWithoutWrites(string scenario)
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await AdminBookingTests.CreateBookingSetup(db);
        await AdminBookingTests.AssignManagerRole(db, facilityId);
        var response = await PostAsync(
            $"/admin/facility/{facilityId}/booking",
            new
            {
                userId = scenario == "both" ? (Guid?)TestClaims.UserIdGuid
                : scenario == "empty-user" ? Guid.Empty
                : null,
                guestName = scenario == "missing" ? null
                : scenario == "blank" ? "  "
                : scenario == "long-name" ? new string('x', 201)
                : "Guest",
                guestEmail = scenario == "invalid-email" ? "not-an-email" : null,
                bookings = new[]
                {
                    new
                    {
                        slotId = slot.Id,
                        slotContractId = contract.Id,
                        name = "Guest",
                    },
                },
                extras = Array.Empty<object>(),
            },
            ct
        );
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await db.Booking.AnyAsync(x => x.FacilityId == facilityId, ct)).ShouldBeFalse();
        (await db.BookingAction.AnyAsync(x => x.Booking.FacilityId == facilityId, ct)).ShouldBeFalse();
    }

    [Fact]
    public async Task GuestBooking_RejectsMemberRatesAndOtherFacilityAccess_AndAllowsNameOnly()
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await AdminBookingTests.CreateBookingSetup(db);
        await AdminBookingTests.AssignManagerRole(db, facilityId);
        contract.Contract.IsPublic = false;
        await db.SaveChangesAsync(ct);
        var body = new
        {
            guestName = "Walk-in Guest",
            bookings = new[]
            {
                new
                {
                    slotId = slot.Id,
                    slotContractId = contract.Id,
                    name = "Guest",
                },
            },
            extras = Array.Empty<object>(),
        };
        var options = await app.Client.GetAsync($"/admin/facility/{facilityId}/booking/options?slotId={slot.Id}", ct);
        options.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var optionJson = JsonDocument.Parse(await options.Content.ReadAsStringAsync(ct));
        optionJson.RootElement.GetProperty("contracts").GetArrayLength().ShouldBe(0);
        (await PostAsync($"/admin/facility/{facilityId}/booking", body, ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await db.Booking.AnyAsync(x => x.FacilityId == facilityId, ct)).ShouldBeFalse();
        contract.Contract.IsPublic = true;
        await db.SaveChangesAsync(ct);
        var (_, _, otherFacilityId) = await AdminBookingTests.CreateBookingSetup(db);
        (await PostAsync($"/admin/facility/{otherFacilityId}/booking", body, ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var nonManager = await CreateCustomerAsync(db, ct);
        (await SendAsAsync(nonManager.Id, HttpMethod.Post, $"/admin/facility/{facilityId}/booking", body, ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await PostAsync($"/admin/facility/{facilityId}/booking", body, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        var booking = await db.Booking.AsNoTracking().SingleAsync(x => x.FacilityId == facilityId, ct);
        booking.UserId.ShouldBeNull();
        booking.GuestEmail.ShouldBeNull();
        booking.GuestPhone.ShouldBeNull();
    }

    private async Task<User> CreateCustomerAsync(AppDbContext db, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            UserName = $"customer-{id}",
            Email = $"{id}@example.com",
            EmailConfirmed = true,
            FirstName = "Booking",
            LastName = "Owner",
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    private Task<HttpResponseMessage> CreateAsync(int facilityId, Guid userId, Guid slotId, int slotContractId, CancellationToken ct, int? statusId = null) =>
        PostAsync(
            $"/admin/facility/{facilityId}/booking",
            new
            {
                userId,
                bookingStatusId = statusId,
                bookings = new[]
                {
                    new
                    {
                        slotId,
                        slotContractId,
                        name = "Booked player",
                        email = "player@example.com",
                        cellphone = "5550100",
                    },
                },
                extras = Array.Empty<object>(),
            },
            ct
        );

    private Task<HttpResponseMessage> PostAsync(string path, object body, CancellationToken ct) =>
        SendAsAsync(TestClaims.UserIdGuid, HttpMethod.Post, path, body, ct);

    private Task<HttpResponseMessage> GetAsAsync(Guid userId, string path, CancellationToken ct) => SendAsAsync(userId, HttpMethod.Get, path, null, ct);

    private async Task<HttpResponseMessage> SendAsAsync(Guid userId, HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Test-User-Id", userId.ToString());
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);
        return await app.Client.SendAsync(request, ct);
    }
}
