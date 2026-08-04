using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Club.Data;
using Club.Entities;
using Club.Features.Outlet.GetAll;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Features.OutletSearch;

[Collection("AppFixture collection")]
public class OutletSearchTests(AppFixture app)
{
    // Minimal view of the paged response. We deserialize manually because
    // FastEndpoints.Testing deserializes to the endpoint's declared response type
    // (PaginatedList<Outlet>), which is not JSON round-trippable.
    private sealed class Paged<T>
    {
        public List<T> Items { get; set; } = [];
    }

    private sealed class OutletItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Address { get; set; }
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private async Task<(HttpResponseMessage rsp, Paged<OutletItem> result)> SearchAsync(OutletGetAllRequest req)
    {
        var (rsp, _) = await app.Client.GETAsync<Endpoint, OutletGetAllRequest, Paged<OutletItem>>(req);
        var body = await rsp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Paged<OutletItem>>(body, JsonOptions) ?? new Paged<OutletItem>();
        return (rsp, result);
    }

    [Fact, Priority(1)]
    public async Task Search_ByName_ReturnsMatchingOutlet()
    {
        // Arrange - use a unique marker so other seeded data can't influence the result
        var marker = Guid.NewGuid().ToString("N");
        await CreateOutlet(app, name: $"Sunset Paddle Club {marker}", tags: null, address: null, description: null);

        // Act
        var (rsp, result) = await SearchAsync(new OutletGetAllRequest { Search = marker });

        // Assert
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        result.Items.ShouldHaveSingleItem();
        result.Items[0].Name.ShouldContain(marker);
    }

    [Fact, Priority(2)]
    public async Task Search_MatchesAcrossFields_AndExcludesNonMatches()
    {
        // Arrange
        var marker = Guid.NewGuid().ToString("N");
        await CreateOutlet(app, name: $"Acme {marker}", tags: null, address: null, description: "Award winning golf course");
        await CreateOutlet(app, name: $"Other {marker}", tags: null, address: null, description: "Nothing relevant here");

        // Act - websearch_to_tsquery ANDs the terms, so only the outlet with both the
        // marker and "golf" in its text is returned
        var (rsp, result) = await SearchAsync(new OutletGetAllRequest { Search = $"golf {marker}" });

        // Assert
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        result.Items.ShouldHaveSingleItem();
        result.Items[0].Name.ShouldStartWith("Acme");
    }

    [Fact, Priority(3)]
    public async Task Search_IsCaseInsensitive()
    {
        // Arrange
        var marker = Guid.NewGuid().ToString("N");
        await CreateOutlet(app, name: $"Riverside {marker}", tags: "paddle", address: null, description: null);

        // Act
        var (rspUpper, resultUpper) = await SearchAsync(new OutletGetAllRequest { Search = $"PADDLE {marker}" });
        var (rspLower, resultLower) = await SearchAsync(new OutletGetAllRequest { Search = $"paddle {marker}" });

        // Assert
        rspUpper.IsSuccessStatusCode.ShouldBeTrue();
        rspLower.IsSuccessStatusCode.ShouldBeTrue();
        resultUpper.Items.ShouldHaveSingleItem();
        resultLower.Items.ShouldHaveSingleItem();
        resultUpper.Items[0].Id.ShouldBe(resultLower.Items[0].Id);
    }

    [Fact, Priority(4)]
    public async Task Search_ByAddress_ReturnsMatches()
    {
        // Arrange
        var marker = Guid.NewGuid().ToString("N");
        await CreateOutlet(app, name: $"Hilltop {marker}", tags: null, address: "42 Mountain View Drive", description: null);

        // Act
        var (rsp, result) = await SearchAsync(new OutletGetAllRequest { Search = $"mountain {marker}" });

        // Assert
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        result.Items.ShouldHaveSingleItem();
        result.Items[0].Address.ShouldStartWith("42 Mountain");
    }

    [Fact, Priority(5)]
    public async Task Search_WithNoMatches_ReturnsEmpty()
    {
        // Act - search for a value that can't exist
        var (rsp, result) = await SearchAsync(new OutletGetAllRequest { Search = $"zzznomatch-{Guid.NewGuid():N}" });

        // Assert
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        result.Items.ShouldBeEmpty();
    }

    [Fact, Priority(6)]
    public async Task Search_WithBlankTerm_ReturnsAllOutlets()
    {
        // Arrange
        var marker = Guid.NewGuid().ToString("N");
        await CreateOutlet(app, name: $"Everything One {marker}", tags: null, address: null, description: null);
        await CreateOutlet(app, name: $"Everything Two {marker}", tags: null, address: null, description: null);

        // Act - a blank search term should not filter (same as the default listing)
        var (rsp, result) = await SearchAsync(new OutletGetAllRequest { Search = "   ", PageSize = 100 });

        // Assert - both seeded outlets are present
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        result.Items.Count(item => item.Name.Contains(marker)).ShouldBe(2);
    }

    [Fact, Priority(7)]
    public async Task Search_WithoutSearchParam_ReturnsAllOutlets()
    {
        // Arrange
        var marker = Guid.NewGuid().ToString("N");
        await CreateOutlet(app, name: $"NoFilter {marker}", tags: null, address: null, description: null);

        // Act
        var (rsp, result) = await SearchAsync(new OutletGetAllRequest { PageSize = 100 });

        // Assert
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        result.Items.ShouldContain(item => item.Name.Contains(marker));
    }

    private static async Task<Outlet> CreateOutlet(AppFixture app, string name, string? tags, string? address, string? description)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var business = new Business { Name = $"Business_{Guid.NewGuid()}" };
        db.Business.Add(business);

        var outletType = new OutletType { Name = $"OutletType_{Guid.NewGuid()}" };
        db.OutletType.Add(outletType);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        var outlet = new Outlet
        {
            Name = name,
            Slug = $"outlet-{Guid.NewGuid()}",
            Business = business,
            BusinessId = business.Id,
            VatNumber = "00000000",
            DisplayName = name,
            OutletType = outletType,
            OutletTypeId = outletType.Id,
            IsActive = true,
            Tags = tags,
            Address = address,
            Description = description,
        };
        db.Outlet.Add(outlet);
        await db.SaveChangesAsync(app.Context.CancellationToken);
        return outlet;
    }
}
