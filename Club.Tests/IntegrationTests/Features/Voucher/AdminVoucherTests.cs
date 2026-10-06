using System.Net;
using System.Net.Http.Json;
using Club.Common.Enums;
using Club.Data;
using Club.DTO;
using Club.Entities;
using Club.Features.Admin.Voucher.Create;
using Club.Features.Admin.Voucher.GetAll;
using IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CreateEndpoint = Club.Features.Admin.Voucher.Create.Endpoint;
using GetAllEndpoint = Club.Features.Admin.Voucher.GetAll.Endpoint;

namespace IntegrationTests.Features.Vouchers;

[Collection("AppFixture collection")]
public class AdminVoucherTests(AppFixture app)
{
    [Fact]
    public async Task CreateAndGetAll_AreFacilityScoped()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        var otherFacilityId = await CreateFacility(db);
        await AssignManagerRole(db, facilityId);

        var (createdResponse, created) = await app.Client.POSTAsync<CreateEndpoint, AdminVoucherCreateRequest, AdminVoucherDTO>(
            new AdminVoucherCreateRequest
            {
                FacilityId = facilityId,
                Name = $"Test {Guid.NewGuid()}",
                Description = "Facility voucher",
                IsExtra = true,
                RedemptionKind = VoucherRedemptionKind.Discount,
                DiscountMode = VoucherDiscountMode.Percentage,
                DiscountValue = 15.25m,
                MaxDiscountAmount = 100m,
            }
        );
        createdResponse.IsSuccessStatusCode.ShouldBeTrue();
        created.ShouldNotBeNull();

        var shared = new Club.Entities.Voucher
        {
            Name = $"Shared {Guid.NewGuid()}",
            Description = "Shared",
            RedemptionKind = VoucherRedemptionKind.Credit,
        };
        db.Voucher.Add(shared);
        db.VoucherFacility.AddRange(
            new VoucherFacility { Voucher = shared, Facility = await db.Facility.FirstAsync(x => x.Id == facilityId) },
            new VoucherFacility { Voucher = shared, Facility = await db.Facility.FirstAsync(x => x.Id == otherFacilityId) }
        );
        await db.SaveChangesAsync();

        var (listResponse, vouchers) = await app.Client.GETAsync<GetAllEndpoint, AdminVoucherGetAllRequest, List<AdminVoucherDTO>>(
            new AdminVoucherGetAllRequest { FacilityId = facilityId }
        );
        listResponse.IsSuccessStatusCode.ShouldBeTrue();
        vouchers.ShouldContain(x => x.Id == created.Id && x.IsExtra && x.DiscountValue == 15.25m);
        vouchers.ShouldNotContain(x => x.Id == shared.Id);
        (await db.VoucherFacility.CountAsync(x => x.VoucherId == created.Id)).ShouldBe(1);
        (await db.WalletVoucherGrant.CountAsync(x => x.VoucherId == created.Id)).ShouldBe(0);

        await AssignManagerRole(db, otherFacilityId);
        var (otherResponse, otherVouchers) = await app.Client.GETAsync<GetAllEndpoint, AdminVoucherGetAllRequest, List<AdminVoucherDTO>>(
            new AdminVoucherGetAllRequest { FacilityId = otherFacilityId }
        );
        otherResponse.IsSuccessStatusCode.ShouldBeTrue();
        otherVouchers.ShouldNotContain(x => x.Id == created.Id);
    }

    [Fact]
    public async Task Create_WhenManagerForDifferentFacility_ReturnsForbidden()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        var otherFacilityId = await CreateFacility(db);
        await AssignManagerRole(db, otherFacilityId);

        var (response, _) = await app.Client.POSTAsync<CreateEndpoint, AdminVoucherCreateRequest, AdminVoucherDTO>(
            new AdminVoucherCreateRequest
            {
                FacilityId = facilityId,
                Name = "Not allowed",
                RedemptionKind = VoucherRedemptionKind.Entitlement,
            }
        );
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_WhenNotManager_ReturnsForbidden()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);

        var (response, _) = await app.Client.GETAsync<GetAllEndpoint, AdminVoucherGetAllRequest, List<AdminVoucherDTO>>(
            new AdminVoucherGetAllRequest { FacilityId = facilityId }
        );
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(VoucherRedemptionKind.Entitlement, true)]
    [InlineData(VoucherRedemptionKind.Credit, true)]
    [InlineData(VoucherRedemptionKind.Discount, false)]
    public async Task Create_SupportsAllRedemptionKinds(VoucherRedemptionKind kind, bool isExtra)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        await AssignManagerRole(db, facilityId);

        var (response, voucher) = await app.Client.POSTAsync<CreateEndpoint, AdminVoucherCreateRequest, AdminVoucherDTO>(
            new AdminVoucherCreateRequest
            {
                FacilityId = facilityId,
                Name = $"Kind {kind} {Guid.NewGuid()}",
                IsExtra = isExtra,
                RedemptionKind = kind,
                DiscountMode = kind == VoucherRedemptionKind.Discount ? VoucherDiscountMode.FixedAmount : null,
                DiscountValue = kind == VoucherRedemptionKind.Discount ? 150.50m : null,
            }
        );
        response.IsSuccessStatusCode.ShouldBeTrue();
        voucher.ShouldNotBeNull();
        voucher.RedemptionKind.ShouldBe(kind);
        voucher.IsExtra.ShouldBe(isExtra);
        (await db.VoucherFacility.CountAsync(x => x.VoucherId == voucher.Id)).ShouldBe(1);
        (await db.WalletVoucherGrant.CountAsync(x => x.VoucherId == voucher.Id)).ShouldBe(0);
    }

    [Theory]
    [InlineData(VoucherDiscountMode.Percentage, 0, null)]
    [InlineData(VoucherDiscountMode.Percentage, 101, null)]
    [InlineData(VoucherDiscountMode.FixedAmount, 10.123, null)]
    [InlineData(VoucherDiscountMode.Percentage, 10, 0d)]
    [InlineData(VoucherDiscountMode.Percentage, 10, -1d)]
    [InlineData(VoucherDiscountMode.Percentage, 10, 1.234)]
    public async Task Create_WhenDiscountInputIsInvalid_ReturnsBadRequest(VoucherDiscountMode mode, decimal value, double? cap)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        await AssignManagerRole(db, facilityId);

        var (response, _) = await app.Client.POSTAsync<CreateEndpoint, AdminVoucherCreateRequest, AdminVoucherDTO>(
            new AdminVoucherCreateRequest
            {
                FacilityId = facilityId,
                Name = "Invalid discount",
                RedemptionKind = VoucherRedemptionKind.Discount,
                DiscountMode = mode,
                DiscountValue = value,
                MaxDiscountAmount = cap.HasValue ? (decimal)cap.Value : null,
            }
        );
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("unknown-kind")]
    [InlineData("unknown-mode")]
    [InlineData("missing-mode")]
    [InlineData("missing-value")]
    [InlineData("blank-name")]
    [InlineData("long-name")]
    [InlineData("long-description")]
    [InlineData("non-discount-fields")]
    public async Task Create_WhenDefinitionIsInvalid_DoesNotCreateVoucher(string invalid)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        await AssignManagerRole(db, facilityId);
        var request = new AdminVoucherCreateRequest
        {
            FacilityId = facilityId,
            Name = $"Invalid {Guid.NewGuid()}",
            RedemptionKind = VoucherRedemptionKind.Discount,
            DiscountMode = VoucherDiscountMode.Percentage,
            DiscountValue = 10m,
        };
        switch (invalid)
        {
            case "unknown-kind":
                request.RedemptionKind = (VoucherRedemptionKind)99;
                break;
            case "unknown-mode":
                request.DiscountMode = (VoucherDiscountMode)99;
                break;
            case "missing-mode":
                request.DiscountMode = null;
                break;
            case "missing-value":
                request.DiscountValue = null;
                break;
            case "blank-name":
                request.Name = "   ";
                break;
            case "long-name":
                request.Name = new string('x', 251);
                break;
            case "long-description":
                request.Description = new string('x', 2001);
                break;
            case "non-discount-fields":
                request.RedemptionKind = VoucherRedemptionKind.Credit;
                break;
        }
        var (response, _) = await app.Client.POSTAsync<CreateEndpoint, AdminVoucherCreateRequest, AdminVoucherDTO>(request);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await db.VoucherFacility.AnyAsync(x => x.FacilityId == facilityId, app.Context.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task Create_BodyCannotOverrideAuthorizedFacilityRoute()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        var otherFacilityId = await CreateFacility(db);
        await AssignManagerRole(db, facilityId);
        var response = await app.Client.PostAsJsonAsync(
            $"/admin/facility/{facilityId}/voucher",
            new AdminVoucherCreateRequest
            {
                FacilityId = otherFacilityId,
                Name = $"Route scope {Guid.NewGuid()}",
                RedemptionKind = VoucherRedemptionKind.Entitlement,
            },
            app.Context.CancellationToken
        );
        response.IsSuccessStatusCode.ShouldBeTrue();
        var created = await response.Content.ReadFromJsonAsync<AdminVoucherDTO>(app.Context.CancellationToken);
        created.ShouldNotBeNull();
        var link = await db.VoucherFacility.SingleAsync(x => x.VoucherId == created.Id, app.Context.CancellationToken);
        link.FacilityId.ShouldBe(facilityId);
    }

    internal static async Task AssignManagerRole(AppDbContext db, int facilityId)
    {
        const string normalizedName = "MANAGER";
        var role = await db.Roles.FirstOrDefaultAsync(x => x.NormalizedName == normalizedName);
        if (role is null)
        {
            role = new Role { Name = Club.Constants.Policy.Manager, NormalizedName = normalizedName };
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

    internal static async Task<int> CreateFacility(AppDbContext db)
    {
        var business = new Business { Name = $"Business_{Guid.NewGuid()}" };
        db.Business.Add(business);
        await db.SaveChangesAsync();
        var outletType = new OutletType { Name = $"OutletType_{Guid.NewGuid()}" };
        db.OutletType.Add(outletType);
        await db.SaveChangesAsync();
        var outlet = new Outlet
        {
            Name = $"Outlet_{Guid.NewGuid()}",
            Slug = $"outlet-{Guid.NewGuid()}",
            Business = business,
            BusinessId = business.Id,
            VatNumber = "00000000",
            DisplayName = "Test Outlet",
            OutletType = outletType,
            OutletTypeId = outletType.Id,
            IsActive = true,
        };
        db.Outlet.Add(outlet);
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync("INSERT INTO facility_type (name) VALUES ({0}) ON CONFLICT DO NOTHING", $"FacilityType_{Guid.NewGuid()}");
        var facilityType = await db.Database.SqlQueryRaw<FacilityType>("SELECT id, name FROM facility_type ORDER BY id DESC LIMIT 1").FirstAsync();
        var facility = new Facility
        {
            Name = "Admin Voucher Facility",
            Outlet = outlet,
            OutletId = outlet.Id,
            FacilityTypeId = facilityType.Id,
            IsActive = true,
        };
        db.Facility.Add(facility);
        await db.SaveChangesAsync();
        return facility.Id;
    }
}
