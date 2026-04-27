namespace Online.Common.Config;

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
            return AuthServerUrl + "realms/" + Realm + "/";
        }
    }

    public string KeycloakTokenEndpoint
    {
        get
        {
            return KeycloakUrlRealm + "protocol/openid-connect/token";
        }
    }

    public string AuthorizationUrl
    {
        get
        {
            return KeycloakUrlRealm + "protocol/openid-connect/auth";
        }
    }

    public string MetadataAddress
    {
        get
        {
            return KeycloakUrlRealm + ".well-known/openid-configuration";
        }
    }

}
