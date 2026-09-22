using Club.Data;
using Club.Entities;
using Club.Features.Admin.Contract;
using Club.Features.Admin.Contract.Create;
using Club.Features.Admin.Contract.GetAll;
using Club.Features.Admin.Contract.Update;
using IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AdminContractCreateEndpoint = Club.Features.Admin.Contract.Create.Endpoint;
using AdminContractDeleteEndpoint = Club.Features.Admin.Contract.Delete.Endpoint;
using AdminContractGetAllEndpoint = Club.Features.Admin.Contract.GetAll.Endpoint;
using AdminContractGetEndpoint = Club.Features.Admin.Contract.Get.Endpoint;
using AdminContractUpdateEndpoint = Club.Features.Admin.Contract.Update.Endpoint;

namespace IntegrationTests.Features.Contracts;

[Collection("AppFixture collection")]
public class AdminContractTests(AppFixture app)
{
    [Fact]
    public async Task AdminContractCreate_WhenManager_CreatesContractLinkedToFacility()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        await AssignManagerRole(db, facilityId);

        // Act
        var (response, created) = await app.Client.POSTAsync<AdminContractCreateEndpoint, AdminContractCreateRequest, AdminContractDTO>(
            new AdminContractCreateRequest
            {
                FacilityId = facilityId,
                Name = "Annual Membership",
                Price = 1200m,
                Frequency = 12,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddYears(1),
                IsActive = true,
                IsPublic = true,
            }
        );

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        created.ShouldNotBeNull();
        created.Id.ShouldBeGreaterThan(0);
        created.Name.ShouldBe("Annual Membership");

        var link = await db.ContractFacility.FirstOrDefaultAsync(cf => cf.ContractId == created.Id && cf.FacilityId == facilityId);
        link.ShouldNotBeNull();
    }

    [Fact]
    public async Task AdminContractGetAll_OnlyReturnsContractsForTheFacility()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        var otherFacilityId = await CreateFacility(db);
        await AssignManagerRole(db, facilityId);

        var mine = new Club.Entities.Contract { Name = $"Mine_{Guid.NewGuid()}" };
        db.Contract.Add(mine);
        db.ContractFacility.Add(new ContractFacility { Contract = mine, Facility = await db.Facility.FirstAsync(f => f.Id == facilityId) });

        var theirs = new Club.Entities.Contract { Name = $"Theirs_{Guid.NewGuid()}" };
        db.Contract.Add(theirs);
        db.ContractFacility.Add(new ContractFacility { Contract = theirs, Facility = await db.Facility.FirstAsync(f => f.Id == otherFacilityId) });
        await db.SaveChangesAsync();

        // Act
        var (response, result) = await app.Client.GETAsync<AdminContractGetAllEndpoint, AdminContractGetAllRequest, List<AdminContractDTO>>(
            new AdminContractGetAllRequest { FacilityId = facilityId }
        );

        // Assert - only the facility's own contracts are returned
        response.IsSuccessStatusCode.ShouldBeTrue();
        result.ShouldContain(c => c.Id == mine.Id);
        result.ShouldNotContain(c => c.Id == theirs.Id);
    }

    [Fact]
    public async Task AdminContractUpdate_WhenManager_UpdatesFields()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        await AssignManagerRole(db, facilityId);
        var contract = await CreateContract(db, facilityId, "Original");

        // Act
        var response = await app.Client.PUTAsync<AdminContractUpdateEndpoint, AdminContractUpdateRequest>(
            new AdminContractUpdateRequest
            {
                FacilityId = facilityId,
                Id = contract.Id,
                Name = "Updated",
                Price = 999m,
                Frequency = 4,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddMonths(6),
                IsActive = false,
                IsPublic = true,
            }
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var updated = await db.Contract.AsNoTracking().FirstAsync(c => c.Id == contract.Id);
        updated.Name.ShouldBe("Updated");
        updated.Price.ShouldBe(999m);
        updated.Frequency.ShouldBe(4);
        updated.IsActive.ShouldBeFalse();
        updated.IsPublic.ShouldBeTrue();
    }

    [Fact]
    public async Task AdminContractDelete_WhenNotInUse_RemovesContract()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        await AssignManagerRole(db, facilityId);
        var contract = await CreateContract(db, facilityId, "Deletable");

        // Act
        var response = await app.Client.DELETEAsync<AdminContractDeleteEndpoint, Club.Features.Admin.Contract.Delete.AdminContractDeleteRequest>(
            new Club.Features.Admin.Contract.Delete.AdminContractDeleteRequest { FacilityId = facilityId, Id = contract.Id }
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await db.Contract.AnyAsync(c => c.Id == contract.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task AdminContractDelete_WhenReferencedBySlot_ReturnsConflict()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        await AssignManagerRole(db, facilityId);
        var contract = await CreateContract(db, facilityId, "InUse");

        var facility = await db.Facility.FirstAsync(f => f.Id == facilityId);
        var slot = new Club.Entities.Slot
        {
            Id = Guid.NewGuid(),
            FacilityId = facilityId,
            StartDatetime = DateTime.UtcNow.AddDays(1),
            EndDatetime = DateTime.UtcNow.AddDays(1).AddHours(1),
            MaxBookings = 4,
        };
        db.Slot.Add(slot);
        db.SlotContract.Add(
            new SlotContract
            {
                SlotId = slot.Id,
                Slot = slot,
                ContractId = contract.Id,
                Contract = contract,
                Price = 100m,
            }
        );
        await db.SaveChangesAsync();

        // Act
        var response = await app.Client.DELETEAsync<AdminContractDeleteEndpoint, Club.Features.Admin.Contract.Delete.AdminContractDeleteRequest>(
            new Club.Features.Admin.Contract.Delete.AdminContractDeleteRequest { FacilityId = facilityId, Id = contract.Id }
        );

        // Assert - in-use contracts are protected from deletion
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await db.Contract.AnyAsync(c => c.Id == contract.Id)).ShouldBeTrue();
    }

    [Fact]
    public async Task AdminContractGetAll_WhenNotManager_ReturnsForbidden()
    {
        // Arrange - no manager role for this facility
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);

        // Act
        var (response, _) = await app.Client.GETAsync<AdminContractGetAllEndpoint, AdminContractGetAllRequest, List<AdminContractDTO>>(
            new AdminContractGetAllRequest { FacilityId = facilityId }
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static async Task<Club.Entities.Contract> CreateContract(AppDbContext db, int facilityId, string name)
    {
        var facility = await db.Facility.FirstAsync(f => f.Id == facilityId);
        var contract = new Club.Entities.Contract { Name = $"{name}_{Guid.NewGuid()}" };
        db.Contract.Add(contract);
        db.ContractFacility.Add(new ContractFacility { Contract = contract, Facility = facility });
        await db.SaveChangesAsync();
        return contract;
    }

    private static async Task AssignManagerRole(AppDbContext db, int facilityId)
    {
        const string normalizedName = "MANAGER";
        var role = await db.Roles.FirstOrDefaultAsync(r => r.NormalizedName == normalizedName);
        if (role is null)
        {
            role = new Role { Name = Club.Constants.Policy.Manager, NormalizedName = normalizedName };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
        }

        var alreadyAssigned = await db.UserRoles.AnyAsync(ur => ur.UserId == TestClaims.UserIdGuid && ur.RoleId == role.Id && ur.FacilityId == facilityId);
        if (!alreadyAssigned)
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
        var facilityType = await db.Database.SqlQueryRaw<FacilityType>("SELECT id, name FROM facility_type ORDER BY id DESC LIMIT 1").FirstOrDefaultAsync();

        var facility = new Facility
        {
            Name = "Admin Contract Facility",
            Outlet = outlet,
            OutletId = outlet.Id,
            FacilityTypeId = facilityType?.Id ?? 1,
            IsActive = true,
        };
        db.Facility.Add(facility);
        await db.SaveChangesAsync();

        return facility.Id;
    }
}
