namespace Club.Common.Config;

public class KeycloakConfig
{
    public const string Key = "Keycloak";

    public string Realm { get; set; } = string.Empty;
    public string AuthServerUrl { get; set; } = string.Empty;
    public string PublicClientId { get; set; } = string.Empty;
    public string AdminClientId { get; set; } = string.Empty;
    public string AdminClientSecret { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    public string KeycloakUrlRealm
    {
        get
        {
            return BuildRealmUrl();
        }
    }

    public string KeycloakTokenEndpoint
    {
        get
        {
            return BuildRealmUrl("protocol/openid-connect/token");
        }
    }

    public string AuthorizationUrl
    {
        get
        {
            return BuildRealmUrl("protocol/openid-connect/auth");
        }
    }

    public string MetadataAddress
    {
        get
        {
            return BuildRealmUrl(".well-known/openid-configuration");
        }
    }

    private string BuildRealmUrl(string relativePath = "")
    {
        var authServerUri = new Uri($"{AuthServerUrl.TrimEnd('/')}/", UriKind.Absolute);
        var realmPath = $"realms/{Realm.Trim('/')}/";

        if (!string.IsNullOrWhiteSpace(relativePath))
        {
            realmPath += relativePath.TrimStart('/');
        }

        return new Uri(authServerUri, realmPath).ToString();
    }
}
