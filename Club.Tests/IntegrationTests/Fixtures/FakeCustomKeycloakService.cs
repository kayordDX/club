using System.Collections.Concurrent;
using Club.Services;

namespace IntegrationTests.Fixtures;

public sealed class FakeCustomKeycloakService : ICustomKeycloakService
{
    private readonly ConcurrentDictionary<Guid, KeycloakUserResult> _users = new();
    private int _createUserCalls;

    public int CreateUserCalls => Volatile.Read(ref _createUserCalls);

    public void AddUser(KeycloakUserResult user) => _users[user.Id] = user;

    public Task<bool> DisableUserTotpAsync(Guid userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(false);
    }

    public Task<KeycloakUserResult?> FindUserAsync(string query, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = _users.Values.FirstOrDefault(x =>
            string.Equals(x.Email, query.Trim(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.PhoneNumber, query.Trim(), StringComparison.OrdinalIgnoreCase)
        );
        return Task.FromResult(user);
    }

    public Task<Guid> CreateUserAsync(string email, string firstName, string lastName, string? phoneNumber, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Interlocked.Increment(ref _createUserCalls);
        throw new InvalidOperationException("Admin booking tests must not create Keycloak accounts.");
    }

    public Task<KeycloakUserResult?> GetUserAsync(Guid userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _users.TryGetValue(userId, out var user);
        return Task.FromResult(user);
    }
}
