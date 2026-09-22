using Club.Data;
using Club.Entities;
using Club.Features.Admin.Contract;
using Club.Features.Admin.Contract.AddMember;
using Club.Features.Admin.Contract.GetMembers;
using Club.Features.Admin.Contract.SearchMember;
using IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AdminContractAddMemberEndpoint = Club.Features.Admin.Contract.AddMember.Endpoint;
using AdminContractGetMembersEndpoint = Club.Features.Admin.Contract.GetMembers.Endpoint;
using AdminContractRemoveMemberEndpoint = Club.Features.Admin.Contract.RemoveMember.Endpoint;
using AdminContractSearchMemberEndpoint = Club.Features.Admin.Contract.SearchMember.Endpoint;
using AdminContractUpdateMemberEndpoint = Club.Features.Admin.Contract.UpdateMember.Endpoint;

namespace IntegrationTests.Features.Contracts;

[Collection("AppFixture collection")]
public class AdminContractMemberTests(AppFixture app)
{
    [Fact]
    public async Task AddMember_WhenManager_LinksExistingUserToContract()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        await AssignManagerRole(db, facilityId);
        var contract = await CreateContract(db, facilityId, "AddMember");
        var user = await CreateUser(db, "jane@example.com");
        var endDate = DateTime.UtcNow.Date.AddMonths(12);

        // Act
        var response = await app.Client.POSTAsync<AdminContractAddMemberEndpoint, AdminContractAddMemberRequest>(
            new AdminContractAddMemberRequest
            {
                FacilityId = facilityId,
                Id = contract.Id,
                UserId = user.Id,
                EndDate = endDate,
            }
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var userContract = await db.UserContract.SingleAsync(uc => uc.ContractId == contract.Id && uc.UserId == user.Id);
        userContract.EndDate.ShouldBe(endDate);
    }

    [Fact]
    public async Task AddMember_WhenAlreadyMember_ReturnsConflict()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        await AssignManagerRole(db, facilityId);
        var contract = await CreateContract(db, facilityId, "Dup");
        var user = await CreateUser(db, "dup@example.com");
        await LinkMember(db, contract, user);
        var endDate = DateTime.UtcNow.Date.AddMonths(12);

        // Act
        var response = await app.Client.POSTAsync<AdminContractAddMemberEndpoint, AdminContractAddMemberRequest>(
            new AdminContractAddMemberRequest
            {
                FacilityId = facilityId,
                Id = contract.Id,
                UserId = user.Id,
                EndDate = endDate,
            }
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateMember_WhenManager_UpdatesMembershipEndDate()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        await AssignManagerRole(db, facilityId);
        var contract = await CreateContract(db, facilityId, "Update");
        var user = await CreateUser(db, "update@example.com");
        var link = await LinkMember(db, contract, user);
        var endDate = DateTime.UtcNow.Date.AddMonths(6);

        // Act
        var response = await app.Client.PUTAsync<AdminContractUpdateMemberEndpoint, Club.Features.Admin.Contract.UpdateMember.AdminContractUpdateMemberRequest>(
            new Club.Features.Admin.Contract.UpdateMember.AdminContractUpdateMemberRequest
            {
                FacilityId = facilityId,
                Id = contract.Id,
                MemberId = link.Id,
                EndDate = endDate,
            }
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await db.UserContract.AsNoTracking().SingleAsync(uc => uc.Id == link.Id)).EndDate.ShouldBe(endDate);
    }

    [Fact]
    public async Task RemoveMember_WhenManager_RemovesTheMembership()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        await AssignManagerRole(db, facilityId);
        var contract = await CreateContract(db, facilityId, "Remove");
        var user = await CreateUser(db, "remove@example.com");
        var link = await LinkMember(db, contract, user);

        // Act
        var response = await app.Client.DELETEAsync<
            AdminContractRemoveMemberEndpoint,
            Club.Features.Admin.Contract.RemoveMember.AdminContractRemoveMemberRequest
        >(
            new Club.Features.Admin.Contract.RemoveMember.AdminContractRemoveMemberRequest
            {
                FacilityId = facilityId,
                Id = contract.Id,
                MemberId = link.Id,
            }
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await db.UserContract.AnyAsync(uc => uc.Id == link.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task RemoveMember_WhenNotFound_Returns404()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        await AssignManagerRole(db, facilityId);
        var contract = await CreateContract(db, facilityId, "Missing");

        // Act
        var response = await app.Client.DELETEAsync<
            AdminContractRemoveMemberEndpoint,
            Club.Features.Admin.Contract.RemoveMember.AdminContractRemoveMemberRequest
        >(
            new Club.Features.Admin.Contract.RemoveMember.AdminContractRemoveMemberRequest
            {
                FacilityId = facilityId,
                Id = contract.Id,
                MemberId = 999999,
            }
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SearchMember_FindsExistingLocalUserByEmail()
    {
        // Arrange
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        await AssignManagerRole(db, facilityId);
        var contract = await CreateContract(db, facilityId, "Search");
        var user = await CreateUser(db, "findme@example.com");

        // Act
        var (response, result) = await app.Client.GETAsync<AdminContractSearchMemberEndpoint, AdminContractSearchMemberRequest, AdminMemberSearchResultDTO>(
            new AdminContractSearchMemberRequest
            {
                FacilityId = facilityId,
                Id = contract.Id,
                Query = "findme@example.com",
            }
        );

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(user.Id);
        result.Email.ShouldBe("findme@example.com");
        result.IsExistingMember.ShouldBeFalse();
    }

    [Fact]
    public async Task AddMember_WhenNotManager_ReturnsForbidden()
    {
        // Arrange - no manager role for this facility
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var facilityId = await CreateFacility(db);
        var contract = await CreateContract(db, facilityId, "NoAuth");
        var user = await CreateUser(db, "noauth@example.com");
        var endDate = DateTime.UtcNow.Date.AddMonths(12);

        // Act
        var response = await app.Client.POSTAsync<AdminContractAddMemberEndpoint, AdminContractAddMemberRequest>(
            new AdminContractAddMemberRequest
            {
                FacilityId = facilityId,
                Id = contract.Id,
                UserId = user.Id,
                EndDate = endDate,
            }
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static async Task<User> CreateUser(AppDbContext db, string email)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = "New",
            LastName = "Member",
            LastSync = DateTime.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<UserContract> LinkMember(AppDbContext db, Club.Entities.Contract contract, User user)
    {
        var link = new UserContract
        {
            ContractId = contract.Id,
            Contract = contract,
            UserId = user.Id,
            User = user,
            StartDate = DateTime.UtcNow,
            Price = contract.Price,
            IsActive = true,
        };
        db.UserContract.Add(link);
        await db.SaveChangesAsync();
        return link;
    }

    private static async Task<Club.Entities.Contract> CreateContract(AppDbContext db, int facilityId, string name)
    {
        var facility = await db.Facility.FirstAsync(f => f.Id == facilityId);
        var contract = new Club.Entities.Contract { Name = $"{name}_{Guid.NewGuid()}", Price = 500m };
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
            Name = "Admin Member Facility",
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
