using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Hybrid;

namespace Online.Data;

public class TicketStore(HybridCache cache) : ITicketStore
{
    private readonly HybridCache _cache = cache;
    private const string KeyPrefix = "auth:";

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var guid = Guid.CreateVersion7().ToString();
        var key = $"{KeyPrefix}{guid}";
        await RenewAsync(key, ticket);
        return key;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var absoluteExpiration = ticket.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.AddHours(1);
        var remainingTime = absoluteExpiration - DateTimeOffset.UtcNow;

        if (remainingTime <= TimeSpan.Zero)
        {
            await _cache.RemoveAsync(key);
            return;
        }

        var options = new HybridCacheEntryOptions
        {
            Expiration = remainingTime,
            LocalCacheExpiration = TimeSpan.FromMinutes(10)
        };

        var userId = ticket.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tags = userId != null ? new[] { $"user:{userId}" } : null;

        var ticketBytes = TicketSerializer.Default.Serialize(ticket);
        await _cache.SetAsync(key, ticketBytes, options, tags);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        try
        {
            var ticketBytes = await _cache.GetOrCreateAsync<byte[]?>(
                key,
                factory: static _ => ValueTask.FromResult<byte[]?>(null));

            if (ticketBytes is null || ticketBytes.Length == 0)
            {
                return null;
            }

            return TicketSerializer.Default.Deserialize(ticketBytes);
        }
        catch (EndOfStreamException)
        {
            await _cache.RemoveAsync(key);
            return null;
        }
        catch (InvalidDataException)
        {
            await _cache.RemoveAsync(key);
            return null;
        }
        catch (FormatException)
        {
            await _cache.RemoveAsync(key);
            return null;
        }
        catch (ArgumentException)
        {
            await _cache.RemoveAsync(key);
            return null;
        }
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }
}
