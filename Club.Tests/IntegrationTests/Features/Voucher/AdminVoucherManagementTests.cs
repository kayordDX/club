using System.Net;
using Club.Common.Enums;
using Club.Data;
using Club.DTO;
using Club.Entities;
using Club.Features.Admin.Voucher.Delete;
using Club.Features.Admin.Voucher.GetAll;
using Club.Features.Admin.Voucher.Update;
using IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DeleteEndpoint = Club.Features.Admin.Voucher.Delete.Endpoint;
using GetAllEndpoint = Club.Features.Admin.Voucher.GetAll.Endpoint;
using UpdateEndpoint = Club.Features.Admin.Voucher.Update.Endpoint;

namespace IntegrationTests.Features.Vouchers;

[Collection("AppFixture collection")]
public class AdminVoucherManagementTests(AppFixture app)
{
    [Fact]
    public async Task Update_UnusedVoucherChangesAllFields_AndInUseVoucherAllowsOnlyMetadata()
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        await AssignManagerRole(db, facilityId);
        var voucher = await CreateVoucher(db, facilityId);
        var response = await app.Client.PUTAsync<UpdateEndpoint, AdminVoucherUpdateRequest>(Request(facilityId, voucher.Id, "Updated", true));
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var saved = await db.Voucher.SingleAsync(x => x.Id == voucher.Id, ct);
        await db.Entry(saved).ReloadAsync(ct);
        saved.Name.ShouldBe("Updated");
        saved.IsExtra.ShouldBeTrue();
        saved.DiscountValue.ShouldBe(25m);

        var user = await db.Users.SingleAsync(x => x.Id == TestClaims.UserIdGuid, ct);
        var wallet = await GetWallet(db, user);
        db.WalletVoucherGrant.Add(
            new WalletVoucherGrant
            {
                Id = Guid.NewGuid(),
                Wallet = wallet,
                Voucher = saved,
                AmountGranted = 1,
                AmountRemaining = 0,
                GrantedAt = DateTime.UtcNow.AddDays(-10),
                ExpiryDate = DateTime.UtcNow.AddDays(-1),
            }
        );
        await db.SaveChangesAsync(ct);
        var metadata = await app.Client.PUTAsync<UpdateEndpoint, AdminVoucherUpdateRequest>(Request(facilityId, voucher.Id, "Metadata", true));
        metadata.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await db.Voucher.AsNoTracking().SingleAsync(x => x.Id == voucher.Id, ct)).Name.ShouldBe("Metadata");
        var conflicting = await app.Client.PUTAsync<UpdateEndpoint, AdminVoucherUpdateRequest>(Request(facilityId, voucher.Id, "Must not persist", false, 30));
        conflicting.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await db.Voucher.AsNoTracking().SingleAsync(x => x.Id == voucher.Id, ct)).DiscountValue.ShouldBe(25m);
        (await db.Voucher.AsNoTracking().SingleAsync(x => x.Id == voucher.Id, ct)).Name.ShouldBe("Metadata");
    }

    [Fact]
    public async Task Delete_UnusedVoucherRemovesLink_ButIssuedAndContractLinkedVouchersConflict()
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        await AssignManagerRole(db, facilityId);
        var removable = await CreateVoucher(db, facilityId);
        var delete = await app.Client.DELETEAsync<DeleteEndpoint, AdminVoucherDeleteRequest>(new() { FacilityId = facilityId, Id = removable.Id });
        delete.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await db.Voucher.AnyAsync(x => x.Id == removable.Id, ct)).ShouldBeFalse();
        (await db.VoucherFacility.AnyAsync(x => x.VoucherId == removable.Id, ct)).ShouldBeFalse();

        var issued = await CreateVoucher(db, facilityId);
        var user = await db.Users.SingleAsync(x => x.Id == TestClaims.UserIdGuid, ct);
        db.WalletVoucherGrant.Add(
            new WalletVoucherGrant
            {
                Id = Guid.NewGuid(),
                Wallet = await GetWallet(db, user),
                Voucher = issued,
                AmountGranted = 1,
                AmountRemaining = 0,
                GrantedAt = DateTime.UtcNow.AddDays(-10),
                ExpiryDate = DateTime.UtcNow.AddDays(-1),
            }
        );
        await db.SaveChangesAsync(ct);
        var issuedDelete = await app.Client.DELETEAsync<DeleteEndpoint, AdminVoucherDeleteRequest>(new() { FacilityId = facilityId, Id = issued.Id });
        issuedDelete.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await db.Voucher.AnyAsync(x => x.Id == issued.Id, ct)).ShouldBeTrue();

        var contracted = await CreateVoucher(db, facilityId);
        var contract = new Contract
        {
            Name = $"Voucher contract {Guid.NewGuid()}",
            Price = 1,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
        };
        db.ContractVoucher.Add(
            new ContractVoucher
            {
                Contract = contract,
                Voucher = contracted,
                Amount = 1,
            }
        );
        await db.SaveChangesAsync(ct);
        var contractDelete = await app.Client.DELETEAsync<DeleteEndpoint, AdminVoucherDeleteRequest>(new() { FacilityId = facilityId, Id = contracted.Id });
        contractDelete.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await db.Voucher.AnyAsync(x => x.Id == contracted.Id, ct)).ShouldBeTrue();
        (await db.ContractVoucher.AnyAsync(x => x.VoucherId == contracted.Id, ct)).ShouldBeTrue();

        var metadata = await app.Client.PUTAsync<UpdateEndpoint, AdminVoucherUpdateRequest>(Request(facilityId, contracted.Id, "Contract metadata"));
        metadata.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var benefitChange = await app.Client.PUTAsync<UpdateEndpoint, AdminVoucherUpdateRequest>(Request(facilityId, contracted.Id, "Not saved", discount: 50));
        benefitChange.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await db.Voucher.AsNoTracking().SingleAsync(x => x.Id == contracted.Id, ct)).Name.ShouldBe("Contract metadata");

        var (_, listed) = await app.Client.GETAsync<GetAllEndpoint, AdminVoucherGetAllRequest, List<AdminVoucherDTO>>(new() { FacilityId = facilityId });
        listed.ShouldNotBeNull();
        listed.Single(x => x.Id == issued.Id).IsInUse.ShouldBeTrue();
        listed.Single(x => x.Id == contracted.Id).IsInUse.ShouldBeTrue();
    }

    [Fact]
    public async Task Management_IsFacilityScopedAndRejectsSharedVouchersAndInvalidDefinitions()
    {
        var ct = app.Context.CancellationToken;
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        var otherId = await CreateFacility(db);
        await AssignManagerRole(db, facilityId);
        var voucher = await CreateVoucher(db, facilityId);
        var unauthorized = await app.Client.PUTAsync<UpdateEndpoint, AdminVoucherUpdateRequest>(Request(otherId, voucher.Id, "Denied"));
        unauthorized.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var unauthorizedDelete = await app.Client.DELETEAsync<DeleteEndpoint, AdminVoucherDeleteRequest>(new() { FacilityId = otherId, Id = voucher.Id });
        unauthorizedDelete.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        await AssignManagerRole(db, otherId);
        var wrongScopeUpdate = await app.Client.PUTAsync<UpdateEndpoint, AdminVoucherUpdateRequest>(Request(otherId, voucher.Id, "Wrong scope"));
        wrongScopeUpdate.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var wrongScopeDelete = await app.Client.DELETEAsync<DeleteEndpoint, AdminVoucherDeleteRequest>(new() { FacilityId = otherId, Id = voucher.Id });
        wrongScopeDelete.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var notFound = await app.Client.PUTAsync<UpdateEndpoint, AdminVoucherUpdateRequest>(Request(facilityId, int.MaxValue, "Missing"));
        notFound.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var shared = await CreateVoucher(db, facilityId);
        db.VoucherFacility.Add(new VoucherFacility { Voucher = shared, Facility = await db.Facility.SingleAsync(x => x.Id == otherId, ct) });
        await db.SaveChangesAsync(ct);
        var sharedUpdate = await app.Client.PUTAsync<UpdateEndpoint, AdminVoucherUpdateRequest>(Request(facilityId, shared.Id, "Nope"));
        sharedUpdate.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var sharedDelete = await app.Client.DELETEAsync<DeleteEndpoint, AdminVoucherDeleteRequest>(new() { FacilityId = facilityId, Id = shared.Id });
        sharedDelete.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await db.Voucher.AnyAsync(x => x.Id == shared.Id, ct)).ShouldBeTrue();

        var invalid = Request(facilityId, voucher.Id, "Invalid");
        invalid.DiscountValue = 101;
        var invalidResponse = await app.Client.PUTAsync<UpdateEndpoint, AdminVoucherUpdateRequest>(invalid);
        invalidResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await db.Voucher.AsNoTracking().SingleAsync(x => x.Id == voucher.Id, ct)).Name.ShouldBe(voucher.Name);
    }

    private static AdminVoucherUpdateRequest Request(int facilityId, int id, string name, bool isExtra = false, decimal discount = 25) =>
        new()
        {
            FacilityId = facilityId,
            Id = id,
            Name = name,
            Description = "Changed",
            IsExtra = isExtra,
            RedemptionKind = VoucherRedemptionKind.Discount,
            DiscountMode = VoucherDiscountMode.Percentage,
            DiscountValue = discount,
        };

    private static async Task<Voucher> CreateVoucher(AppDbContext db, int facilityId)
    {
        var voucher = new Voucher
        {
            Name = $"Voucher {Guid.NewGuid()}",
            RedemptionKind = VoucherRedemptionKind.Discount,
            DiscountMode = VoucherDiscountMode.Percentage,
            DiscountValue = 25,
        };
        db.Voucher.Add(voucher);
        db.VoucherFacility.Add(new VoucherFacility { Voucher = voucher, Facility = await db.Facility.SingleAsync(x => x.Id == facilityId) });
        await db.SaveChangesAsync();
        return voucher;
    }

    private static async Task<Wallet> GetWallet(AppDbContext db, User user)
    {
        var wallet = await db.Wallet.SingleOrDefaultAsync(x => x.UserId == user.Id);
        if (wallet is not null)
            return wallet;
        wallet = new Wallet { Id = Guid.NewGuid(), User = user };
        db.Wallet.Add(wallet);
        await db.SaveChangesAsync();
        return wallet;
    }

    private static async Task AssignManagerRole(AppDbContext db, int facilityId)
    {
        var role = await db.Roles.FirstOrDefaultAsync(x => x.NormalizedName == "MANAGER");
        if (role is null)
        {
            role = new Role { Name = Club.Constants.Policy.Manager, NormalizedName = "MANAGER" };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
        }
        if (!await db.UserRoles.AnyAsync(x => x.UserId == TestClaims.UserIdGuid && x.RoleId == role.Id && x.FacilityId == facilityId))
        {
            db.UserRoles.Add(
                new UserRole
                {
                    UserId = TestClaims.UserIdGuid,
                    RoleId = role.Id,
                    FacilityId = facilityId,
                }
            );
            await db.SaveChangesAsync();
        }
    }

    private static async Task<int> CreateFacility(AppDbContext db)
    {
        var business = new Business { Name = $"Business_{Guid.NewGuid()}" };
        var outletType = new OutletType { Name = $"OutletType_{Guid.NewGuid()}" };
        var outlet = new Outlet
        {
            Name = $"Outlet_{Guid.NewGuid()}",
            Slug = $"outlet-{Guid.NewGuid()}",
            Business = business,
            DisplayName = "Test",
            VatNumber = "0",
            OutletType = outletType,
        };
        await db.Database.ExecuteSqlRawAsync("INSERT INTO facility_type (name) VALUES ({0}) ON CONFLICT DO NOTHING", $"FacilityType_{Guid.NewGuid()}");
        var type = await db.Database.SqlQueryRaw<FacilityType>("SELECT id, name FROM facility_type ORDER BY id DESC LIMIT 1").FirstAsync();
        var facility = new Facility
        {
            Name = "Voucher facility",
            Outlet = outlet,
            FacilityTypeId = type.Id,
            IsActive = true,
        };
        db.Facility.Add(facility);
        await db.SaveChangesAsync();
        return facility.Id;
    }
}
