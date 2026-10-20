using System.Net;
using System.Net.Http.Json;
using Club.Common.Enums;
using Club.Data;
using Club.DTO;
using Club.Entities;
using IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Features.WalletVouchers;

[Collection("AppFixture collection")]
public class WalletTransferTests(AppFixture app)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Transfer_MovesRemainingGrantAndPreservesIssuance_WithSenderAndRecipientAudit(bool byPhone)
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (grant, recipient) = await SetupAsync(db, ct);
        var originalWalletId = grant.WalletId;
        var response = await TransferAsync(grant.Id, byPhone ? recipient.PhoneNumber! : $" {recipient.Email!.ToUpperInvariant()} ", ct);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        db.ChangeTracker.Clear();
        var moved = await db.WalletVoucherGrant.Include(x => x.Wallet).SingleAsync(x => x.Id == grant.Id, ct);
        moved.WalletId.ShouldNotBe(originalWalletId);
        moved.Wallet.UserId.ShouldBe(recipient.Id);
        moved.AmountRemaining.ShouldBe(3m);
        moved.AmountGranted.ShouldBe(5m);
        moved.GrantedAt.ShouldBe(grant.GrantedAt, TimeSpan.FromMilliseconds(1));
        moved.ExpiryDate.ShouldBe(grant.ExpiryDate, TimeSpan.FromMilliseconds(1));
        moved.VoucherId.ShouldBe(grant.VoucherId);
        var audits = await db.WalletVoucherGrantAudit.Where(x => x.WalletVoucherGrantId == grant.Id).ToListAsync(ct);
        audits.Count.ShouldBe(2);
        audits.Single(x => x.Action == WalletVoucherGrantAction.Issued).Reference.ShouldBe("original issuance");
        var transfer = audits.Single(x => x.Action == WalletVoucherGrantAction.Transferred);
        transfer.AssigningUserId.ShouldBe(TestClaims.UserIdGuid);
        transfer.RecipientUserId.ShouldBe(recipient.Id);
        transfer.SourceType.ShouldBe(WalletVoucherGrantSource.Gift);
        transfer.Timestamp.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
        var senderList = (await app.Client.GetFromJsonAsync<List<WalletVoucherDTO>>("/wallet/vouchers", ct))!;
        senderList.ShouldNotContain(x => x.GrantId == grant.Id);
        using var listRequest = new HttpRequestMessage(HttpMethod.Get, "/wallet/vouchers");
        listRequest.Headers.Add("X-Test-User-Id", recipient.Id.ToString());
        using var listResponse = await app.Client.SendAsync(listRequest, ct);
        var recipientList = (await listResponse.Content.ReadFromJsonAsync<List<WalletVoucherDTO>>(ct))!;
        recipientList.Single(x => x.GrantId == grant.Id).CanTransfer.ShouldBeFalse();
        (await TransferAsync(grant.Id, "test@example.com", ct, recipient.Id)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await TransferAsync(grant.Id, recipient.Email!, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await db.WalletVoucherGrantAudit.CountAsync(x => x.WalletVoucherGrantId == grant.Id, ct)).ShouldBe(2);
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("other-owner", HttpStatusCode.NotFound)]
    [InlineData("unknown-grant", HttpStatusCode.NotFound)]
    [InlineData("self", HttpStatusCode.BadRequest)]
    [InlineData("missing-recipient", HttpStatusCode.BadRequest)]
    [InlineData("unknown-recipient", HttpStatusCode.BadRequest)]
    [InlineData("ambiguous-recipient", HttpStatusCode.BadRequest)]
    [InlineData("expired", HttpStatusCode.BadRequest)]
    [InlineData("exhausted", HttpStatusCode.BadRequest)]
    [InlineData("inactive-sender", HttpStatusCode.BadRequest)]
    [InlineData("inactive-recipient", HttpStatusCode.BadRequest)]
    [InlineData("currency", HttpStatusCode.BadRequest)]
    public async Task Transfer_RejectsInvalidRequestsWithoutMovingOrAuditing(string invalid, HttpStatusCode expected)
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (grant, recipient) = await SetupAsync(db, ct);
        var address = recipient.Email!;
        switch (invalid)
        {
            case "self":
                address = "test@example.com";
                break;
            case "missing-recipient":
                address = " ";
                break;
            case "unknown-recipient":
                address = $"{Guid.NewGuid()}@example.com";
                break;
            case "ambiguous-recipient":
                db.Users.Add(
                    new User
                    {
                        Id = Guid.NewGuid(),
                        UserName = Guid.NewGuid().ToString(),
                        PhoneNumber = address,
                        FirstName = "Ambiguous",
                        LastName = "User",
                    }
                );
                break;
            case "expired":
                grant.ExpiryDate = DateTime.UtcNow.AddSeconds(-1);
                break;
            case "exhausted":
                grant.AmountRemaining = 0;
                break;
            case "inactive-sender":
                grant.Wallet.IsActive = false;
                break;
            case "inactive-recipient":
            case "currency":
                db.Wallet.Add(
                    new Club.Entities.Wallet
                    {
                        Id = Guid.NewGuid(),
                        User = recipient,
                        IsActive = invalid != "inactive-recipient",
                        Currency = invalid == "currency" ? "USD" : "ZAR",
                    }
                );
                break;
        }
        await db.SaveChangesAsync(ct);
        var walletId = grant.WalletId;
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/wallet/vouchers/{(invalid == "unknown-grant" ? Guid.NewGuid() : grant.Id)}/transfer")
        {
            Content = JsonContent.Create(new { recipient = address }),
        };
        if (invalid == "anonymous")
            request.Headers.Add("X-Test-Anonymous", "true");
        if (invalid == "other-owner")
            request.Headers.Add("X-Test-User-Id", recipient.Id.ToString());
        using var response = await app.Client.SendAsync(request, ct);
        response.StatusCode.ShouldBe(expected);
        db.ChangeTracker.Clear();
        (await db.WalletVoucherGrant.SingleAsync(x => x.Id == grant.Id, ct)).WalletId.ShouldBe(walletId);
        (await db.WalletVoucherGrantAudit.CountAsync(x => x.WalletVoucherGrantId == grant.Id, ct)).ShouldBe(1);
        // Restore the shared sender wallet for other tests in this fixture.
        await db.Wallet.Where(x => x.Id == walletId).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, true), ct);
    }

    [Fact]
    public async Task ConcurrentTransfers_OnlyOneSucceeds()
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (grant, recipient) = await SetupAsync(db, ct);
        var responses = await Task.WhenAll(TransferAsync(grant.Id, recipient.Email!, ct), TransferAsync(grant.Id, recipient.Email!, ct));
        responses.Count(x => x.StatusCode == HttpStatusCode.NoContent).ShouldBe(1);
        responses.Count(x => x.StatusCode == HttpStatusCode.NotFound).ShouldBe(1);
        (await db.WalletVoucherGrantAudit.CountAsync(x => x.WalletVoucherGrantId == grant.Id && x.Action == WalletVoucherGrantAction.Transferred, ct)).ShouldBe(
            1
        );
        (await db.Wallet.CountAsync(x => x.UserId == recipient.Id, ct)).ShouldBe(1);
    }

    [Fact]
    public async Task Database_RejectsASecondTransferAudit()
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (grant, recipient) = await SetupAsync(db, ct);
        (await TransferAsync(grant.Id, recipient.Email!, ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        db.WalletVoucherGrantAudit.Add(
            new WalletVoucherGrantAudit
            {
                Id = Guid.NewGuid(),
                WalletVoucherGrant = grant,
                Action = WalletVoucherGrantAction.Transferred,
                Timestamp = DateTime.UtcNow,
                SourceType = WalletVoucherGrantSource.Gift,
                AssigningUserId = recipient.Id,
                RecipientUserId = TestClaims.UserIdGuid,
            }
        );
        var error = await Should.ThrowAsync<DbUpdateException>(() => db.SaveChangesAsync(ct));
        ((Npgsql.PostgresException)error.InnerException!).SqlState.ShouldBe("23505");
        db.ChangeTracker.Clear();
        (await db.WalletVoucherGrantAudit.CountAsync(x => x.WalletVoucherGrantId == grant.Id, ct)).ShouldBe(2);
    }

    private async Task<HttpResponseMessage> TransferAsync(Guid grantId, string recipient, CancellationToken ct, Guid? sender = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/wallet/vouchers/{grantId}/transfer") { Content = JsonContent.Create(new { recipient }) };
        if (sender.HasValue)
            request.Headers.Add("X-Test-User-Id", sender.Value.ToString());
        return await app.Client.SendAsync(request, ct);
    }

    private static async Task<(WalletVoucherGrant, User)> SetupAsync(AppDbContext db, CancellationToken ct)
    {
        var sender = await db.Users.SingleAsync(x => x.Id == TestClaims.UserIdGuid, ct);
        var wallet = await db.Wallet.SingleOrDefaultAsync(x => x.UserId == sender.Id, ct) ?? new Club.Entities.Wallet { Id = Guid.NewGuid(), User = sender };
        wallet.IsActive = true;
        wallet.Currency = "ZAR";
        var recipient = new User
        {
            Id = Guid.NewGuid(),
            UserName = Guid.NewGuid().ToString(),
            Email = $"{Guid.NewGuid()}@example.com",
            PhoneNumber = Guid.NewGuid().ToString(),
            FirstName = "Gift",
            LastName = "Recipient",
        };
        db.Users.Add(recipient);
        var grant = new WalletVoucherGrant
        {
            Id = Guid.NewGuid(),
            Wallet = wallet,
            Voucher = new Voucher { Name = "Gift rounds", RedemptionKind = VoucherRedemptionKind.Entitlement },
            AmountGranted = 5,
            AmountRemaining = 3,
            GrantedAt = DateTime.UtcNow.AddDays(-1),
            ExpiryDate = DateTime.UtcNow.AddDays(10),
        };
        db.WalletVoucherGrant.Add(grant);
        db.WalletVoucherGrantAudit.Add(
            new WalletVoucherGrantAudit
            {
                Id = Guid.NewGuid(),
                WalletVoucherGrant = grant,
                Action = WalletVoucherGrantAction.Issued,
                Timestamp = DateTime.UtcNow,
                SourceType = WalletVoucherGrantSource.System,
                Reference = "original issuance",
            }
        );
        await db.SaveChangesAsync(ct);
        return (grant, recipient);
    }
}
