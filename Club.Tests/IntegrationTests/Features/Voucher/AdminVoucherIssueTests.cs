using System.Net;
using System.Net.Http.Json;
using Club.Common.Enums;
using Club.Data;
using Club.Entities;
using Club.Features.Admin.Voucher.Issue;
using IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Features.Vouchers;

[Collection("AppFixture collection")]
public class AdminVoucherIssueTests(AppFixture app)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Issue_ByExactContact_CreatesZarWalletAndAuditedGrant(bool phone)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (request, user) = await Setup(db);
        request.Recipient = phone ? $" {user.PhoneNumber} " : $" {user.Email!.ToUpperInvariant()} ";

        var response = await Send(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var grantId = await response.Content.ReadFromJsonAsync<Guid>();
        var wallet = await db.Wallet.SingleAsync(x => x.UserId == user.Id);
        wallet.Currency.ShouldBe("ZAR");
        wallet.IsActive.ShouldBeTrue();
        var grant = await db.WalletVoucherGrant.SingleAsync(x => x.Id == grantId);
        grant.WalletId.ShouldBe(wallet.Id);
        grant.AmountGranted.ShouldBe(2m);
        grant.AmountRemaining.ShouldBe(2m);
        grant.GrantedAt.ShouldBe(request.ValidFrom, TimeSpan.FromSeconds(1));
        grant.ExpiryDate.ShouldBe(request.ExpiryDate, TimeSpan.FromSeconds(1));
        var audit = await db.WalletVoucherGrantAudit.SingleAsync(x => x.WalletVoucherGrantId == grantId);
        audit.AssigningUserId.ShouldBe(TestClaims.UserIdGuid);
        audit.SourceType.ShouldBe(WalletVoucherGrantSource.Admin);
    }

    [Fact]
    public async Task Issue_ReusesExistingWallet_AndConcurrentSendsCreateOnlyOneWallet()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (request, user) = await Setup(db);

        var otherVoucher = new Club.Entities.Voucher
        {
            Name = "Gift extra",
            RedemptionKind = VoucherRedemptionKind.Entitlement,
            IsExtra = true,
        };
        db.VoucherFacility.Add(new VoucherFacility { Voucher = otherVoucher, Facility = await db.Facility.SingleAsync(x => x.Id == request.FacilityId) });
        await db.SaveChangesAsync();
        var otherRequest = new AdminVoucherIssueRequest
        {
            FacilityId = request.FacilityId,
            VoucherId = otherVoucher.Id,
            Recipient = request.Recipient,
            Amount = request.Amount,
            ValidFrom = request.ValidFrom,
            ExpiryDate = request.ExpiryDate,
        };

        var responses = await Task.WhenAll(Send(request), Send(otherRequest));
        responses.ShouldAllBe(x => x.StatusCode == HttpStatusCode.OK);
        var wallet = await db.Wallet.SingleAsync(x => x.UserId == user.Id);
        (await Send(request)).StatusCode.ShouldBe(HttpStatusCode.OK);

        (await db.Wallet.CountAsync(x => x.UserId == user.Id)).ShouldBe(1);
        (await db.WalletVoucherGrant.CountAsync(x => x.WalletId == wallet.Id)).ShouldBe(3);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("partial-email")]
    [InlineData("partial-phone")]
    [InlineData("name")]
    [InlineData("blank")]
    public async Task Issue_RequiresExactContact_AndDoesNotCreateWallet(string invalid)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (request, user) = await Setup(db);
        request.Recipient = invalid switch
        {
            "partial-email" => user.Email![..8],
            "partial-phone" => user.PhoneNumber![..6],
            "name" => user.FirstName,
            "blank" => " ",
            _ => $"{Guid.NewGuid()}@missing.example",
        };

        var response = await Send(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        if (invalid != "blank")
            (await response.Content.ReadAsStringAsync()).ShouldContain("No user found");
        await AssertNoWrites(db, request, user);
    }

    [Fact]
    public async Task Issue_DuplicatePhone_DoesNotAssignToArbitraryUser()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (request, user) = await Setup(db);
        db.Users.Add(
            new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Duplicate",
                LastName = "Recipient",
                PhoneNumber = user.PhoneNumber,
            }
        );
        await db.SaveChangesAsync();
        request.Recipient = user.PhoneNumber!;

        var response = await Send(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("More than one user matches");
        await AssertNoWrites(db, request, user);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Issue_RequiresManagerAtThisFacility(bool managerElsewhere)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (request, user) = await Setup(db, authorize: false);
        if (managerElsewhere)
            await AdminVoucherTests.AssignManagerRole(db, await AdminVoucherTests.CreateFacility(db));

        (await Send(request)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        await AssertNoWrites(db, request, user);
    }

    [Theory]
    [InlineData("facility")]
    [InlineData("shared")]
    [InlineData("amount")]
    [InlineData("fractional")]
    [InlineData("expiry")]
    [InlineData("source")]
    public async Task Issue_InvalidGrant_DoesNotCreateWallet(string invalid)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (request, user) = await Setup(db);
        if (invalid == "facility")
            db.VoucherFacility.RemoveRange(await db.VoucherFacility.Where(x => x.VoucherId == request.VoucherId).ToListAsync());
        if (invalid == "shared")
            db.VoucherFacility.Add(
                new VoucherFacility
                {
                    VoucherId = request.VoucherId,
                    Voucher = await db.Voucher.SingleAsync(x => x.Id == request.VoucherId),
                    FacilityId = await AdminVoucherTests.CreateFacility(db),
                    Facility = (await db.Facility.OrderByDescending(x => x.Id).FirstAsync()),
                }
            );
        if (invalid == "amount")
            request.Amount = 0;
        if (invalid == "fractional")
            request.Amount = 1.5m;
        if (invalid == "expiry")
            request.ExpiryDate = request.ValidFrom.AddDays(-1);
        if (invalid == "source")
            request.SourceUserContractId = int.MaxValue;
        await db.SaveChangesAsync();

        (await Send(request)).StatusCode.ShouldBe(invalid is "facility" or "shared" ? HttpStatusCode.NotFound : HttpStatusCode.BadRequest);

        await AssertNoWrites(db, request, user);
    }

    [Theory]
    [InlineData(false, "ZAR")]
    [InlineData(true, "USD")]
    public async Task Issue_IneligibleWallet_IsNotChanged(bool active, string currency)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (request, user) = await Setup(db);
        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            User = user,
            IsActive = active,
            Currency = currency,
        };
        db.Wallet.Add(wallet);
        await db.SaveChangesAsync();

        (await Send(request)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        await db.Entry(wallet).ReloadAsync();
        wallet.Currency.ShouldBe(currency);
        wallet.IsActive.ShouldBe(active);
        (await db.WalletVoucherGrant.AnyAsync(x => x.VoucherId == request.VoucherId)).ShouldBeFalse();
    }

    [Fact]
    public async Task Issue_BodyCannotOverrideAuthorizedFacilityRoute()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (request, user) = await Setup(db);
        var facilityId = request.FacilityId;
        request.FacilityId = await AdminVoucherTests.CreateFacility(db);

        var response = await app.Client.PostAsJsonAsync($"/admin/facility/{facilityId}/voucher/issue", request, app.Context.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await db.Wallet.AnyAsync(x => x.UserId == user.Id)).ShouldBeTrue();
    }

    private Task<HttpResponseMessage> Send(AdminVoucherIssueRequest request) =>
        app.Client.PostAsJsonAsync($"/admin/facility/{request.FacilityId}/voucher/issue", request, app.Context.CancellationToken);

    private static async Task AssertNoWrites(AppDbContext db, AdminVoucherIssueRequest request, User user)
    {
        (await db.Wallet.AnyAsync(x => x.UserId == user.Id)).ShouldBeFalse();
        (await db.WalletVoucherGrant.AnyAsync(x => x.VoucherId == request.VoucherId)).ShouldBeFalse();
    }

    private static async Task<(AdminVoucherIssueRequest, User)> Setup(AppDbContext db, bool authorize = true)
    {
        var facilityId = await AdminVoucherTests.CreateFacility(db);
        if (authorize)
            await AdminVoucherTests.AssignManagerRole(db, facilityId);
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Voucher",
            LastName = "Recipient",
            Email = $"recipient-{Guid.NewGuid()}@example.com",
            PhoneNumber = $"+27{Random.Shared.NextInt64(100000000, 999999999)}",
        };
        var voucher = new Club.Entities.Voucher { Name = "Gift round", RedemptionKind = VoucherRedemptionKind.Entitlement };
        db.Users.Add(user);
        db.VoucherFacility.Add(new VoucherFacility { Voucher = voucher, Facility = await db.Facility.SingleAsync(x => x.Id == facilityId) });
        await db.SaveChangesAsync();
        return (
            new AdminVoucherIssueRequest
            {
                FacilityId = facilityId,
                VoucherId = voucher.Id,
                Recipient = user.Email,
                Amount = 2,
                ValidFrom = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(30),
            },
            user
        );
    }
}
