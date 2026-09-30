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
public class WalletVouchersTests(AppFixture app)
{
    [Fact]
    public async Task List_ReturnsAllOwnGrantsWithoutABooking_AndExcludesOtherOwners()
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(x => x.Id == TestClaims.UserIdGuid, ct);
        var wallet = await db.Wallet.SingleOrDefaultAsync(x => x.UserId == user.Id, ct);
        wallet ??= new Club.Entities.Wallet { Id = Guid.NewGuid(), User = user };
        var otherWallet = new Club.Entities.Wallet
        {
            Id = Guid.NewGuid(),
            User = new User
            {
                Id = Guid.NewGuid(),
                UserName = $"wallet-other-{Guid.NewGuid()}",
                FirstName = "Other",
                LastName = "Owner",
            },
        };
        var now = DateTime.UtcNow;
        var voucher = new Voucher
        {
            Name = $"Rounds-{Guid.NewGuid()}",
            Description = "Issued rounds",
            RedemptionKind = VoucherRedemptionKind.Entitlement,
        };
        WalletVoucherGrant Grant(Club.Entities.Wallet owner, Voucher definition, decimal remaining, DateTime from, DateTime expiry) =>
            new()
            {
                Id = Guid.NewGuid(),
                Wallet = owner,
                Voucher = definition,
                AmountGranted = 5,
                AmountRemaining = remaining,
                GrantedAt = from,
                ExpiryDate = expiry,
            };
        var own = new[]
        {
            Grant(wallet, voucher, 3, now.AddDays(-1), now.AddDays(10)),
            Grant(wallet, voucher, 2, now.AddDays(-1), now.AddDays(20)),
            Grant(wallet, voucher, 1, now.AddDays(-10), now.AddDays(-1)),
            Grant(wallet, voucher, 0, now.AddDays(-1), now.AddDays(10)),
            Grant(wallet, voucher, 5, now.AddDays(1), now.AddDays(10)),
            Grant(wallet, new Voucher { Name = "Credit", RedemptionKind = VoucherRedemptionKind.Credit }, 4.5m, now.AddDays(-1), now.AddDays(10)),
            Grant(
                wallet,
                new Voucher
                {
                    Name = "Discount",
                    RedemptionKind = VoucherRedemptionKind.Discount,
                    DiscountMode = VoucherDiscountMode.Percentage,
                    DiscountValue = 20,
                    MaxDiscountAmount = 50,
                    IsExtra = true,
                },
                1,
                now.AddDays(-1),
                now.AddDays(10)
            ),
        };
        var outlet = new Outlet
        {
            Name = "Wallet voucher test",
            Slug = $"wallet-voucher-{Guid.NewGuid()}",
            DisplayName = "Test",
            VatNumber = "0",
            Business = new Business { Name = "Test" },
            OutletType = new OutletType { Name = "Test" },
        };
        var golf = new FacilityType { Name = "Golf Course" };
        Facility Facility(string name) =>
            new()
            {
                Name = name,
                Outlet = outlet,
                FacilityType = golf,
            };
        var north = Facility("North Golf Course");
        var south = Facility("South Golf Course");
        var unrelated = Facility("Unrelated Golf Course");
        foreach (var definition in new[] { voucher, own[^1].Voucher })
        {
            db.VoucherFacility.AddRange(
                new VoucherFacility { Voucher = definition, Facility = north },
                new VoucherFacility { Voucher = definition, Facility = south }
            );
        }
        await db.SaveChangesAsync(ct);
        db.Extra.AddRange(
            new Extra
            {
                Name = "Golf Cart",
                Facility = north,
                OutletId = outlet.Id,
            },
            new Extra
            {
                Name = "Golf Cart",
                Facility = south,
                OutletId = outlet.Id,
            },
            new Extra
            {
                Name = "Club Hire",
                Facility = south,
                OutletId = outlet.Id,
            },
            new Extra
            {
                Name = "Unrelated Extra",
                Facility = unrelated,
                OutletId = outlet.Id,
            }
        );
        var other = Grant(otherWallet, voucher, 5, now.AddDays(-1), now.AddDays(10));
        db.WalletVoucherGrant.AddRange(own.Append(other));
        await db.SaveChangesAsync(ct);

        var response = await app.Client.GetAsync("/wallet/vouchers", ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = (await response.Content.ReadFromJsonAsync<List<WalletVoucherDTO>>(ct))!;
        var expectedIds = await db.WalletVoucherGrant.Where(x => x.Wallet.UserId == user.Id).Select(x => x.Id).ToListAsync(ct);
        result.Select(x => x.GrantId).Order().ShouldBe(expectedIds.Order());
        result.ShouldNotContain(x => x.GrantId == other.Id);
        result.Select(x => x.ExpiryDate).ShouldBe(result.Select(x => x.ExpiryDate).Order());
        foreach (var grant in own)
        {
            var item = result.Single(x => x.GrantId == grant.Id);
            item.VoucherId.ShouldBe(grant.VoucherId);
            item.Name.ShouldBe(grant.Voucher.Name);
            item.Description.ShouldBe(grant.Voucher.Description);
            item.AmountGranted.ShouldBe(5);
            item.AmountRemaining.ShouldBe(grant.AmountRemaining);
            item.RedemptionKind.ShouldBe(grant.Voucher.RedemptionKind);
            item.DiscountMode.ShouldBe(grant.Voucher.DiscountMode);
            item.DiscountValue.ShouldBe(grant.Voucher.DiscountValue);
            item.MaxDiscountAmount.ShouldBe(grant.Voucher.MaxDiscountAmount);
            item.IsExtra.ShouldBe(grant.Voucher.IsExtra);
            if (grant.Voucher == voucher || grant.Voucher == own[^1].Voucher)
            {
                item.FacilityNames.ShouldBe(["North Golf Course", "South Golf Course"]);
                item.GameTypes.ShouldBe(["Golf Course"]);
                item.ExtraNames.ShouldBe(["Club Hire", "Golf Cart"]);
            }
            else
            {
                item.FacilityNames.ShouldBeEmpty();
                item.GameTypes.ShouldBeEmpty();
                item.ExtraNames.ShouldBeEmpty();
            }
            item.GrantedAt.ShouldBe(grant.GrantedAt, TimeSpan.FromMilliseconds(1));
            item.ExpiryDate.ShouldBe(grant.ExpiryDate, TimeSpan.FromMilliseconds(1));
            item.Currency.ShouldBe(wallet.Currency);
            item.IsWalletActive.ShouldBe(wallet.IsActive);
        }
    }
}
