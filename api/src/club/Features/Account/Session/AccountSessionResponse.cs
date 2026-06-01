
using Keycloak.AuthServices.Sdk.Kiota.Admin.Models;

namespace Online.Features.Account.Session;

public class AccountSessionResponse
{
    public AccountSessionResponse()
    {
    }

    public string? Id { get; set; }
    public string? IpAddress { get; set; }
    public long? LastAccess { get; set; }
    public long? Start { get; set; }
    public string? Username { get; set; }

    public AccountSessionResponse(UserSessionRepresentation session)
    {
        Id = session.Id;
        IpAddress = session.IpAddress;
        Start = session.Start;
        LastAccess = session.LastAccess;
        Username = session.Username;
    }
}
