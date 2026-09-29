using Club.Common.Enums;
using Club.Data;
using Club.Entities;
using IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Testcontainers.PostgreSql;

namespace IntegrationTests.Features.Payment;

[Collection("AppFixture collection")]
public class VoucherGrantMigrationTests(AppFixture app)
{
    [Fact]
    public async Task ItemMigration_DoesNotInferRedemptionPermissionsFromIssuanceAssociations()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var postgres = new PostgreSqlBuilder("postgres:18").Build();
        await postgres.StartAsync(ct);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseApplicationServiceProvider(app.Server.Services)
            .UseNpgsql(postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var db = new AppDbContext(options, new HttpContextAccessor());
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260929204410_WalletVoucherGrantProvenance", ct);
        var rounds = new Voucher { Name = "Legacy round", RedemptionKind = VoucherRedemptionKind.Entitlement };
        var extras = new Voucher
        {
            Name = "Legacy extra",
            RedemptionKind = VoucherRedemptionKind.Entitlement,
            IsExtra = true,
        };
        var discount = new Voucher
        {
            Name = "Legacy discount",
            RedemptionKind = VoucherRedemptionKind.Discount,
            DiscountMode = VoucherDiscountMode.Percentage,
            DiscountValue = 10,
        };
        db.Voucher.AddRange(rounds, extras, discount);
        db.ContractVoucher.Add(
            new ContractVoucher
            {
                Voucher = rounds,
                Contract = new Contract { Name = "Issuing contract" },
                Amount = 7,
            }
        );
        await db.SaveChangesAsync(ct);
        await migrator.MigrateAsync(cancellationToken: ct);
        (await db.VoucherContract.CountAsync(ct)).ShouldBe(0);
        (await db.VoucherExtra.CountAsync(ct)).ShouldBe(0);
        (await db.Voucher.CountAsync(ct)).ShouldBe(3);
        (await db.ContractVoucher.AsNoTracking().SingleAsync(ct)).Amount.ShouldBe(7m);
        (await db.Voucher.AsNoTracking().SingleAsync(x => x.Id == discount.Id, ct)).DiscountValue.ShouldBe(10m);
    }

    [Fact]
    public async Task Migration_BackfillsKnownProvenance_WithoutChangingGrantBalancesOrDates()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var postgres = new PostgreSqlBuilder("postgres:18").Build();
        await postgres.StartAsync(ct);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseApplicationServiceProvider(app.Server.Services)
            .UseNpgsql(postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var db = new AppDbContext(options, new HttpContextAccessor());
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260929200113_AddPaymentVoucherLedger", ct);
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "legacy",
            FirstName = "Legacy",
            LastName = "Owner",
        };
        var wallet = new Wallet { Id = Guid.NewGuid(), User = user };
        var voucher = new Voucher { Name = "Legacy credit", RedemptionKind = VoucherRedemptionKind.Credit };
        var membership = new UserContract
        {
            Contract = new Contract { Name = "Legacy contract" },
            User = user,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            IsActive = false,
            EndDate = DateTime.UtcNow.AddDays(-1),
        };
        db.Wallet.Add(wallet);
        db.Voucher.Add(voucher);
        db.UserContract.Add(membership);
        await db.SaveChangesAsync(ct);
        var id = Guid.NewGuid();
        var grantedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var expiry = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO wallet_voucher_grant (id, wallet_id, voucher_id, user_contract_id, amount_granted, amount_remaining, granted_at, expiry_date)
            VALUES ({id}, {wallet.Id}, {voucher.Id}, {membership.Id}, {500m}, {123.45m}, {grantedAt}, {expiry})
            """,
            ct
        );
        var payment = new Club.Entities.Payment
        {
            PaymentStatus = new PaymentStatus { Name = "Legacy settled" },
            PaymentType = await db.Set<PaymentType>().SingleAsync(x => x.Id == (int)PaymentTypeEnum.Voucher, ct),
            PaymentStatusDate = grantedAt,
            Amount = 376.55m,
            TransactionId = "legacy-voucher-payment",
            ProviderName = "voucher",
            ProviderReference = id.ToString(),
        };
        var booking = new Club.Entities.Booking
        {
            User = user,
            BookingStatus = new BookingStatus { Name = "Legacy pending" },
            BookingStatusDate = grantedAt,
            AmountPaid = 376.55m,
            AmountOutstanding = 23.45m,
            ExpiresAt = expiry,
        };
        db.PaymentBooking.Add(new PaymentBooking { Booking = booking, Payment = payment });
        await db.SaveChangesAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO payment_voucher (payment_id, wallet_voucher_grant_id, units)
            VALUES ({payment.Id}, {id}, {376.55m})
            """,
            ct
        );
        await migrator.MigrateAsync(cancellationToken: ct);
        var grant = await db.WalletVoucherGrant.AsNoTracking().SingleAsync(ct);
        grant.Id.ShouldBe(id);
        grant.AmountGranted.ShouldBe(500m);
        grant.AmountRemaining.ShouldBe(123.45m);
        grant.GrantedAt.ShouldBe(grantedAt);
        grant.ExpiryDate.ShouldBe(expiry);
        var audit = await db.WalletVoucherGrantAudit.AsNoTracking().SingleAsync(ct);
        audit.WalletVoucherGrantId.ShouldBe(id);
        audit.SourceType.ShouldBe(WalletVoucherGrantSource.Contract);
        audit.SourceUserContractId.ShouldBe(membership.Id);
        audit.Action.ShouldBe(WalletVoucherGrantAction.Issued);
        audit.Timestamp.ShouldBe(grantedAt);
        audit.AssigningUserId.ShouldBeNull();
        audit.Reason.ShouldBeNull();
        audit.Reference.ShouldBeNull();
        var redemption = await db.PaymentVoucher.AsNoTracking().Include(x => x.Payment).SingleAsync(ct);
        redemption.WalletVoucherGrantId.ShouldBe(id);
        redemption.Units.ShouldBe(376.55m);
        redemption.Payment.Amount.ShouldBe(376.55m);
        redemption.Payment.TransactionId.ShouldBe("legacy-voucher-payment");
        (await db.PaymentBooking.AsNoTracking().SingleAsync(ct)).BookingId.ShouldBe(booking.Id);
        var preservedBooking = await db.Booking.AsNoTracking().SingleAsync(ct);
        preservedBooking.AmountPaid.ShouldBe(376.55m);
        preservedBooking.AmountOutstanding.ShouldBe(23.45m);
        db.UserContract.Remove(membership);
        await db.SaveChangesAsync(ct);
        (await db.WalletVoucherGrantAudit.AsNoTracking().SingleAsync(ct)).SourceUserContractId.ShouldBe(membership.Id);
        (
            await db
                .Database.SqlQueryRaw<int>(
                    "SELECT count(*)::int AS \"Value\" FROM information_schema.columns WHERE table_name = 'wallet_voucher_grant' AND column_name = 'user_contract_id'"
                )
                .SingleAsync(ct)
        ).ShouldBe(0);
    }
}
