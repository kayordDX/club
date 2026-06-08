using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Club.Entities;

namespace Club.Data;

public class UserStore(AppDbContext context, IdentityErrorDescriber? describer = null) : UserStore<User, Role, AppDbContext, Guid, IdentityUserClaim<Guid>, UserRole, IdentityUserLogin<Guid>, IdentityUserToken<Guid>, IdentityRoleClaim<Guid>>(context, describer)
{
    private readonly AppDbContext _dbContext = context;

    public async Task<IdentityResult> AddToRoleAsync(User user, string normalizedName, int? facilityId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedName);

        var role = await FindRoleAsync(normalizedName, cancellationToken)
            ?? throw new InvalidOperationException($"Role '{normalizedName}' not found.");

        var existing = await _dbContext.Set<UserRole>()
            .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id && ur.FacilityId == facilityId, cancellationToken);

        if (existing)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UserAlreadyInRole",
                Description = $"User is already in role '{normalizedName}' for facility {facilityId}."
            });
        }

        _dbContext.Set<UserRole>().Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            FacilityId = facilityId
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> RemoveFromRoleAsync(User user, string normalizedName, int facilityId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedName);

        var role = await FindRoleAsync(normalizedName, cancellationToken)
            ?? throw new InvalidOperationException($"Role '{normalizedName}' not found.");

        var userRole = await _dbContext.Set<UserRole>()
            .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id && ur.FacilityId == facilityId, cancellationToken);

        if (userRole == null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UserNotInRole",
                Description = $"User is not in role '{normalizedName}' for facility {facilityId}."
            });
        }

        _dbContext.Set<UserRole>().Remove(userRole);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return IdentityResult.Success;
    }

    public async Task<bool> IsInRoleAsync(User user, string normalizedName, int facilityId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedName);

        var role = await FindRoleAsync(normalizedName, cancellationToken);
        if (role == null)
        {
            return false;
        }

        return await _dbContext.Set<UserRole>()
            .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id && ur.FacilityId == facilityId, cancellationToken);
    }

    public async Task<IList<string>> GetRolesForFacilityAsync(User user, int facilityId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return await _dbContext.Set<UserRole>()
            .Where(ur => ur.UserId == user.Id && ur.FacilityId == facilityId)
            .Join(
                _dbContext.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => r.Name!)
            .ToListAsync(cancellationToken);
    }

    public override async Task<IList<string>> GetRolesAsync(User user, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        return await _dbContext.UserRoles
            .Where(x => x.UserId == user.Id && !string.IsNullOrEmpty(x.Role.Name))
            .Select(ur => ur.Role.Name!)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task<IList<User>> GetUsersInRoleForFacilityAsync(string normalizedName, int facilityId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedName);

        var role = await FindRoleAsync(normalizedName, cancellationToken);
        if (role == null)
        {
            return [];
        }

        return await _dbContext.Set<UserRole>()
            .Where(ur => ur.RoleId == role.Id && ur.FacilityId == facilityId)
            .Join(
                _dbContext.Users,
                ur => ur.UserId,
                u => u.Id,
                (ur, u) => u)
            .ToListAsync(cancellationToken);
    }
}
