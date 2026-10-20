using Club.Common.Enums;
using Club.Data;
using Club.Entities;
using Club.Features.Admin.Contract;
using Club.Features.Admin.Contract.AddMember;
using Club.Features.Admin.Contract.CreateMember;
using Club.Features.Admin.Contract.GetVouchers;
using Club.Features.Admin.Contract.SetVouchers;
using IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AddEndpoint = Club.Features.Admin.Contract.AddMember.Endpoint;
using CreateEndpoint = Club.Features.Admin.Contract.CreateMember.Endpoint;
using GetEndpoint = Club.Features.Admin.Contract.GetVouchers.Endpoint;
using SetEndpoint = Club.Features.Admin.Contract.SetVouchers.Endpoint;

namespace IntegrationTests.Features.Contracts;

[Collection("AppFixture collection")]
public class AdminContractVoucherTests(AppFixture app)
{
    private CancellationToken Ct => app.Context.CancellationToken;

    [Fact]
    public async Task Vouchers_RoundTripReplaceAndClear_RejectInvalidAmountsAndDuplicates()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (facility, contract, voucher) = await Seed(db);
        (await Set(facility.Id, contract.Id, voucher.Id, 3)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var (response, result) = await app
            .Client.GETAsync<GetEndpoint, AdminContractGetVouchersRequest, AdminContractVouchersDTO>(new() { FacilityId = facility.Id, Id = contract.Id })
            .WaitAsync(Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();
        result.Vouchers.Single().Amount.ShouldBe(3);
        result.AvailableVouchers.ShouldContain(x => x.Id == voucher.Id);
        foreach (var amount in new[] { 0m, -1m, 1.5m })
            (await Set(facility.Id, contract.Id, voucher.Id, amount)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var duplicate = await app
            .Client.PUTAsync<SetEndpoint, AdminContractSetVouchersRequest>(
                new()
                {
                    FacilityId = facility.Id,
                    Id = contract.Id,
                    Vouchers = [new() { VoucherId = voucher.Id, Amount = 1 }, new() { VoucherId = voucher.Id, Amount = 2 }],
                }
            )
            .WaitAsync(Ct);
        duplicate.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await db.ContractVoucher.AsNoTracking().SingleAsync(x => x.ContractId == contract.Id, Ct)).Amount.ShouldBe(3);
        (await Set(facility.Id, contract.Id, voucher.Id, 5)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await db.ContractVoucher.AsNoTracking().SingleAsync(x => x.ContractId == contract.Id, Ct)).Amount.ShouldBe(5);
        var clear = await app
            .Client.PUTAsync<SetEndpoint, AdminContractSetVouchersRequest>(
                new()
                {
                    FacilityId = facility.Id,
                    Id = contract.Id,
                    Vouchers = [],
                }
            )
            .WaitAsync(Ct);
        clear.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await db.ContractVoucher.AnyAsync(x => x.ContractId == contract.Id, Ct)).ShouldBeFalse();
    }

    [Fact]
    public async Task Vouchers_EnforceFacilityScope_AndAllowCreditDecimals()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (facility, contract, voucher) = await Seed(db);
        var (other, otherContract, otherVoucher) = await Seed(db);
        (await Set(facility.Id, otherContract.Id, voucher.Id, 1)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await Set(facility.Id, contract.Id, otherVoucher.Id, 1)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        voucher.RedemptionKind = VoucherRedemptionKind.Credit;
        await db.SaveChangesAsync(Ct);
        (await Set(facility.Id, contract.Id, voucher.Id, 50.25m)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Set(facility.Id, contract.Id, voucher.Id, 50.255m)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var role = await db.UserRoles.SingleAsync(x => x.UserId == TestClaims.UserIdGuid && x.FacilityId == other.Id, Ct);
        db.UserRoles.Remove(role);
        await db.SaveChangesAsync(Ct);
        (await Set(other.Id, otherContract.Id, otherVoucher.Id, 1)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var (denied, _) = await app
            .Client.GETAsync<GetEndpoint, AdminContractGetVouchersRequest, AdminContractVouchersDTO>(new() { FacilityId = other.Id, Id = otherContract.Id })
            .WaitAsync(Ct);
        denied.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Assignment_IssuesBenefitsWithProvenance_WithoutChangingExistingGrants(bool createMember)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (facility, contract, voucher) = await Seed(db);
        (await Set(facility.Id, contract.Id, voucher.Id, 4)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var end = DateTime.UtcNow.Date.AddMonths(12);
        var email = $"member-{Guid.NewGuid()}@example.com";
        HttpResponseMessage response;
        Guid userId;
        if (createMember)
        {
            scope.ServiceProvider.GetRequiredService<FakeCustomKeycloakService>().AllowCreationFor(email);
            response = await app
                .Client.POSTAsync<CreateEndpoint, AdminContractCreateMemberRequest>(
                    new()
                    {
                        FacilityId = facility.Id,
                        Id = contract.Id,
                        Email = email,
                        FirstName = "New",
                        LastName = "Member",
                        EndDate = end,
                    }
                )
                .WaitAsync(Ct);
            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            userId = (await db.Users.SingleAsync(x => x.Email == email, Ct)).Id;
        }
        else
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FirstName = "Existing",
                LastName = "Member",
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(Ct);
            userId = user.Id;
            response = await Add(facility.Id, contract.Id, userId, end);
        }
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var membership = await db.UserContract.SingleAsync(x => x.ContractId == contract.Id && x.UserId == userId, Ct);
        var grant = await db.WalletVoucherGrant.SingleAsync(x => x.VoucherId == voucher.Id && x.Wallet.UserId == userId, Ct);
        grant.AmountGranted.ShouldBe(4);
        grant.AmountRemaining.ShouldBe(4);
        grant.GrantedAt.ShouldBe(membership.StartDate);
        grant.ExpiryDate.ShouldBe(end);
        var audit = await db.WalletVoucherGrantAudit.SingleAsync(x => x.WalletVoucherGrantId == grant.Id, Ct);
        audit.SourceType.ShouldBe(WalletVoucherGrantSource.Contract);
        audit.SourceUserContractId.ShouldBe(membership.Id);
        audit.AssigningUserId.ShouldBe(TestClaims.UserIdGuid);
        (await Add(facility.Id, contract.Id, userId, end)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await Set(facility.Id, contract.Id, voucher.Id, 10)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await db.Entry(grant).ReloadAsync(Ct);
        grant.AmountGranted.ShouldBe(4);
        (await db.WalletVoucherGrant.CountAsync(x => x.VoucherId == voucher.Id, Ct)).ShouldBe(1);
    }

    [Fact]
    public async Task Assignment_ReusesWalletAndIssuesAllKinds_AndRejectsInactiveWalletAtomically()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (facility, contract, entitlement) = await Seed(db);
        var credit = new Voucher { Name = "Global credit", RedemptionKind = VoucherRedemptionKind.Credit };
        var discount = new Voucher
        {
            Name = "Discount",
            RedemptionKind = VoucherRedemptionKind.Discount,
            DiscountMode = VoucherDiscountMode.Percentage,
            DiscountValue = 10,
        };
        db.Voucher.AddRange(credit, discount);
        var email = $"wallet-{Guid.NewGuid()}@example.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = "Wallet",
            LastName = "Owner",
        };
        var wallet = new Club.Entities.Wallet
        {
            Id = Guid.NewGuid(),
            User = user,
            Currency = "ZAR",
            IsActive = false,
        };
        db.Wallet.Add(wallet);
        await db.SaveChangesAsync(Ct);
        var set = await app
            .Client.PUTAsync<SetEndpoint, AdminContractSetVouchersRequest>(
                new()
                {
                    FacilityId = facility.Id,
                    Id = contract.Id,
                    Vouchers =
                    [
                        new() { VoucherId = entitlement.Id, Amount = 2 },
                        new() { VoucherId = credit.Id, Amount = 50.25m },
                        new() { VoucherId = discount.Id, Amount = 3 },
                    ],
                }
            )
            .WaitAsync(Ct);
        set.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var end = DateTime.UtcNow.Date.AddMonths(12);
        (await Add(facility.Id, contract.Id, user.Id, end)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await db.UserContract.AnyAsync(x => x.ContractId == contract.Id && x.UserId == user.Id, Ct)).ShouldBeFalse();
        (await db.WalletVoucherGrant.AnyAsync(x => x.WalletId == wallet.Id, Ct)).ShouldBeFalse();
        wallet.IsActive = true;
        await db.SaveChangesAsync(Ct);
        (await Add(facility.Id, contract.Id, user.Id, end)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var grants = await db.WalletVoucherGrant.Where(x => x.WalletId == wallet.Id).ToListAsync(Ct);
        grants.Count.ShouldBe(3);
        grants.Single(x => x.VoucherId == entitlement.Id).AmountRemaining.ShouldBe(2);
        grants.Single(x => x.VoucherId == credit.Id).AmountRemaining.ShouldBe(50.25m);
        grants.Single(x => x.VoucherId == discount.Id).AmountRemaining.ShouldBe(3);
        (await db.Wallet.CountAsync(x => x.UserId == user.Id, Ct)).ShouldBe(1);
    }

    private Task<HttpResponseMessage> Set(int facilityId, int contractId, int voucherId, decimal amount) =>
        app
            .Client.PUTAsync<SetEndpoint, AdminContractSetVouchersRequest>(
                new()
                {
                    FacilityId = facilityId,
                    Id = contractId,
                    Vouchers = [new() { VoucherId = voucherId, Amount = amount }],
                }
            )
            .WaitAsync(Ct);

    private Task<HttpResponseMessage> Add(int facilityId, int contractId, Guid userId, DateTime end) =>
        app
            .Client.POSTAsync<AddEndpoint, AdminContractAddMemberRequest>(
                new()
                {
                    FacilityId = facilityId,
                    Id = contractId,
                    UserId = userId,
                    EndDate = end,
                }
            )
            .WaitAsync(Ct);

    private async Task<(Facility, Contract, Voucher)> Seed(AppDbContext db)
    {
        var type = await db.Database.SqlQueryRaw<FacilityType>("SELECT id, name FROM facility_type ORDER BY id LIMIT 1").FirstAsync(Ct);
        var outletType = new OutletType { Name = $"Type {Guid.NewGuid()}" };
        var outlet = new Outlet
        {
            Name = "Contract vouchers",
            Slug = $"vouchers-{Guid.NewGuid()}",
            DisplayName = "Vouchers",
            VatNumber = "0000",
            Business = new Business { Name = "Voucher business" },
            OutletType = outletType,
            IsActive = true,
        };
        var facility = new Facility
        {
            Name = "Voucher facility",
            Outlet = outlet,
            FacilityTypeId = type.Id,
            IsActive = true,
        };
        var contract = new Contract
        {
            Name = "Membership",
            Price = 100,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1),
        };
        var voucher = new Voucher { Name = "Free round", RedemptionKind = VoucherRedemptionKind.Entitlement };
        db.ContractFacility.Add(new ContractFacility { Contract = contract, Facility = facility });
        db.VoucherFacility.Add(new VoucherFacility { Voucher = voucher, Facility = facility });
        await db.SaveChangesAsync(Ct);
        var role = await db.Roles.FirstOrDefaultAsync(x => x.NormalizedName == "MANAGER", Ct);
        if (role is null)
        {
            role = new Role { Name = Club.Constants.Policy.Manager, NormalizedName = "MANAGER" };
            db.Roles.Add(role);
            await db.SaveChangesAsync(Ct);
        }
        db.UserRoles.Add(
            new UserRole
            {
                UserId = TestClaims.UserIdGuid,
                RoleId = role.Id,
                FacilityId = facility.Id,
            }
        );
        await db.SaveChangesAsync(Ct);
        return (facility, contract, voucher);
    }
}
