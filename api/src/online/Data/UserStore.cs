using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Online.Entities;

namespace Online.Data;

public class UserStore(AppDbContext context, IdentityErrorDescriber? describer = null) : UserStore<User, Role, AppDbContext, Guid, IdentityUserClaim<Guid>, UserRole, IdentityUserLogin<Guid>, IdentityUserToken<Guid>, IdentityRoleClaim<Guid>>(context, describer)
{
    private readonly AppDbContext _dbContext = context;

    public async Task<IdentityResult> AddToRoleAsync(User user, string normalizedName, int? outletId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedName);

        var role = await FindRoleAsync(normalizedName, cancellationToken)
            ?? throw new InvalidOperationException($"Role '{normalizedName}' not found.");

        var existing = await _dbContext.Set<UserRole>()
            .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id && ur.OutletId == outletId, cancellationToken);

        if (existing)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UserAlreadyInRole",
                Description = $"User is already in role '{normalizedName}' for outlet {outletId}."
            });
        }

        _dbContext.Set<UserRole>().Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            OutletId = outletId
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> RemoveFromRoleAsync(User user, string normalizedName, int outletId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedName);

        var role = await FindRoleAsync(normalizedName, cancellationToken)
            ?? throw new InvalidOperationException($"Role '{normalizedName}' not found.");

        var userRole = await _dbContext.Set<UserRole>()
            .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id && ur.OutletId == outletId, cancellationToken);

        if (userRole == null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UserNotInRole",
                Description = $"User is not in role '{normalizedName}' for outlet {outletId}."
            });
        }

        _dbContext.Set<UserRole>().Remove(userRole);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return IdentityResult.Success;
    }

    public async Task<bool> IsInRoleAsync(User user, string normalizedName, int outletId, CancellationToken cancellationToken = default)
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
            .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id && ur.OutletId == outletId, cancellationToken);
    }

    public async Task<IList<string>> GetRolesForOutletAsync(User user, int outletId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return await _dbContext.Set<UserRole>()
            .Where(ur => ur.UserId == user.Id && ur.OutletId == outletId)
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
            .Where(x => x.UserId == user.Id && x.OutletId == null && !string.IsNullOrEmpty(x.Role.Name))
            .Select(ur => ur.Role.Name!)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task<IList<string>> GetOutletRolesAsync(User user, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        return await _dbContext.UserRoles
            .Where(x => x.UserId == user.Id && x.OutletId > 0 && x.Role.Name != null && x.Outlet != null)
            .Select(x => $"{x.Role.Name}:{x.Outlet!.Slug}")
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task<IList<UserRole>> GetUserRolesWithOutletsAsync(User user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(user);

        return await _dbContext.Set<UserRole>()
            .Where(ur => ur.UserId == user.Id)
            .Include(ur => ur.Outlet)
            .ToListAsync(cancellationToken);
    }

    public async Task<IList<User>> GetUsersInRoleForOutletAsync(string normalizedName, int outletId, CancellationToken cancellationToken = default)
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
            .Where(ur => ur.RoleId == role.Id && ur.OutletId == outletId)
            .Join(
                _dbContext.Users,
                ur => ur.UserId,
                u => u.Id,
                (ur, u) => u)
            .ToListAsync(cancellationToken);
    }
}
