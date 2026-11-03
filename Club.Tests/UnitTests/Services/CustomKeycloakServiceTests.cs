using System.Net;
using System.Reflection;
using Club.Common.Config;
using Club.Services;
using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Requests.Users;
using Microsoft.Extensions.Options;

namespace UnitTests.Services;

public class CustomKeycloakServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateUser_SendsSetupEmailOnlyWhenEnabled(bool enabled)
    {
        var client = DispatchProxy.Create<IKeycloakUserClient, KeycloakClientProxy>();
        var proxy = (KeycloakClientProxy)client;
        var service = new CustomKeycloakService(client, Options.Create(new KeycloakConfig { Realm = "test", SendAccountSetupEmails = enabled }));

        var id = await service.CreateUserAsync("member@example.com", "Jane", "Doe", "0123456789", TestContext.Current.CancellationToken);

        Assert.Equal(proxy.UserId, id);
        Assert.Equal(enabled ? 1 : 0, proxy.EmailCount);
        if (enabled)
            Assert.Equal(new[] { "UPDATE_PASSWORD", "VERIFY_EMAIL" }, proxy.Actions);
    }

    [Fact]
    public void SetupEmails_AreDisabledByDefault()
    {
        Assert.False(new KeycloakConfig().SendAccountSetupEmails);
    }
}

public class KeycloakClientProxy : DispatchProxy
{
    public Guid UserId { get; } = Guid.NewGuid();
    public int EmailCount { get; private set; }
    public ICollection<string>? Actions { get; private set; }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod?.Name == nameof(IKeycloakUserClient.CreateUserWithResponseAsync))
        {
            var response = new HttpResponseMessage(HttpStatusCode.Created);
            response.Headers.Location = new Uri($"https://keycloak.example/users/{UserId}");
            return Task.FromResult(response);
        }

        if (targetMethod?.Name == nameof(IKeycloakUserClient.ExecuteActionsEmailAsync))
        {
            EmailCount++;
            Actions = ((ExecuteActionsEmailRequest)args![2]!).Actions;
            return Task.CompletedTask;
        }

        throw new NotSupportedException(targetMethod?.Name);
    }
}
