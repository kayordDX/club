---
name: api
description: Use this skill for API and backend work in the Club project. Apply it when creating, editing, reviewing, or debugging FastEndpoints code under `Club.Api/Features/`, and whenever a request mentions the API, backend, endpoints, DTOs, EF, or client generation.
---

# API Skill

Use this skill for Club API and backend work.

Reach for it when the task mentions any of the following:

- API
- backend
- endpoint
- FastEndpoints
- DTO
- EF / Entity Framework
- OpenAPI / Swagger / Scalar
- generated client / `pnpm run api`

## What This Project Uses

- Backend framework: FastEndpoints on .NET 10
- API code location: `Club.Api/Features/`
- Shared DTO location: `Club.Api/DTO/`
- Entities: `Club.Api/Entities/`
- Service registration: `Club.Api/Common/Extensions/`
- Dev API docs: `<API URL from aspire describe>/scalar/v1` (port 5000 is not guaranteed, especially with `--isolated`)

## Core Rules

- Keep API work feature-based under `Club.Api/Features/`
- Use file-scoped namespaces (`Club.Features.[Feature].[Action]`)
- Name endpoint files `Endpoint.cs`
- Put request models next to the endpoint in the same action folder
- Keep DTOs in `Club.Api/DTO/` unless there is a clear local-only reason not to
- Pass `CancellationToken` through async database and service calls
- Add `Description(x => x.WithName("FeatureAction"))` in `Configure()`
- Use SvelteKit remote functions for browser data access; they wrap generated server transports. Regenerate transports, shared types, Zod schemas, and remote functions after API changes.

## Booking Domain (facility/outlet lookup)

Booking summary UIs (edit/view/pay pages) already have what they need — do **not** add facility/outlet to `BookingDTO`.

| Endpoint | Remote function (`$lib/api/remote/booking.remote.ts`) | Returns | Carries |
| --- | --- | --- | --- |
| `GET /booking/{id}` | `bookingGet` | `BookingDTO` | status, `user`, amounts, `extraBookings[].extra` (name/price), `slotContractBookings[].slotContract.slot` (`startDatetime`, `facilityId`) |
| `GET /booking/{id}/path` | `bookingGetPath` | `BookingPathDTO` | `outletName`, `outletSlug`, `facilityName`, `facilityId`, `slotStartDatetime` |

- Facility/outlet names for a booking come from `bookingGetPath` (`/booking/{id}/path`), **not** from `BookingDTO`.
- The path query powers booking navigation: `BookingBreadcrumbs` (`$lib/components/BookingBreadcrumbs.svelte`) and `getBookingPayUrl` (`$lib/booking/payUrl.ts`).
- Frontend pattern for booking detail pages (view/edit/pay):

```ts
import { bookingGetPath } from "$lib/api/remote/booking.remote";

const path = await bookingGetPath(bookingId);
```

- `BookingGetPath` derives facility/outlet from the first `SlotContractBooking → SlotContract → Slot.Facility.Outlet`, so it assumes a booking has at least one slot contract booking.

## Adding a New Endpoint (Quick Start)

One feature/action = one folder. Copy this end-to-end workflow:

1. **Create the folder structure:**

```bash
mkdir -p Club.Api/Features/{FeatureName}/{ActionName}
```

2. **Add `Endpoint.cs`** (template below)
3. **Add a request DTO** if the action takes input: `[Feature][Action]Request.cs`
4. **Reuse or add a response DTO** under `Club.Api/DTO/`
5. **Register services** if needed in `Club.Api/Common/Extensions/`
6. **Build:** `dotnet build Club.Api/Club.Api.csproj`
7. **Regenerate the frontend clients** (needs the API running): follow [API Client Generation](#api-client-generation)
8. **Update frontend usage**: types from `client/src/lib/api/generated/`, browser calls via `client/src/lib/api/remote/`

> Integration tests live in `Club.Tests/IntegrationTests/Features/` (xUnit + FastEndpoints.Testing). Target a single class: `dotnet test Club.Tests/IntegrationTests/IntegrationTests.csproj -- --filter-class <FullyQualifiedClassName>`

## Folder Pattern

Use this structure for new endpoints:

```text
Club.Api/Features/
└── FeatureName/
    └── ActionName/
        ├── Endpoint.cs
        └── FeatureActionRequest.cs
```

Examples:

```text
Club.Api/Features/Outlet/Get/Endpoint.cs
Club.Api/Features/Outlet/Get/OutletGetRequest.cs
Club.Api/Features/Account/Login/Endpoint.cs
Club.Api/Features/Slot/Edit/SlotEditRequest.cs
```

## Naming Conventions

- Feature folder: domain name, PascalCase
- Action folder: `Get`, `GetAll`, `Add`, `Edit`, `Delete`, or a clear action name like `Login`
- Endpoint class file: always `Endpoint.cs`
- Request DTO: `[Feature][Action]Request`
- OpenAPI name: `[Feature][Action]`

Examples:

- `Features/Outlet/Get/` -> `OutletGetRequest` -> `WithName("OutletGet")`
- `Features/Outlet/GetAll/` -> `OutletGetAllRequest` -> `WithName("OutletGetAll")`
- `Features/Account/Login/` -> `LoginRequest` or `AccountLoginRequest` based on local conventions

## Endpoint Types

Choose the smallest FastEndpoints base type that fits:

- `Endpoint<TRequest, TResponse>` - request + typed response
- `Endpoint<TRequest>` - request only
- `EndpointWithoutRequest<TResponse>` - no request, typed response
- `EndpointWithoutRequest` - no request, no response body

## Minimum Endpoint Template

```csharp
using Microsoft.EntityFrameworkCore;
using Club.Data;
using Club.DTO;

namespace Club.Features.Outlet.Get;

public class Endpoint(AppDbContext dbContext) : Endpoint<OutletGetRequest, OutletDTO>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/outlet/{slug}");
        Description(x => x.WithName("OutletGet"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(OutletGetRequest req, CancellationToken ct)
    {
        var result = await _dbContext.Outlet
            .ProjectToDto()
            .FirstOrDefaultAsync(x => x.Slug == req.Slug, ct);

        if (result == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(result, ct);
    }
}
```

## Request DTO Template

```csharp
namespace Club.Features.Outlet.Get;

public class OutletGetRequest
{
    public required string Slug { get; set; }
}
```

## Configure Checklist

Every endpoint should make these decisions explicitly:

- HTTP verb and route (`Get`, `Post`, `Put`, `Patch`, `Delete`)
- OpenAPI name with `Description(...WithName(...))`
- Auth mode: default auth, `AllowAnonymous()`, or `RequireAuthorization(...)`
- File upload support if needed

Example:

```csharp
public override void Configure()
{
    Post("/outlets");
    Description(x => x.WithName("OutletAdd"));
    RequireAuthorization("Admin");
}
```

## Response Guidelines

Use the right send method for the job:

```csharp
await Send.OkAsync(data, ct);                       // 200
await Send.CreatedAtAsync<Endpoint>(id, data, ct); // 201
await Send.NoContentAsync(ct);                     // 204
await Send.BadRequestAsync(ct);                    // 400
await Send.NotFoundAsync(ct);                      // 404
await Send.ConflictAsync(ct);                      // 409
```

## API Client Generation

After changing endpoints or response shapes, first inspect/reuse the running Aspire stack with `aspire describe --non-interactive`. Rebuild a changed API using `aspire resource api rebuild --non-interactive`, then `aspire wait api --non-interactive`.

`pnpm api` fetches Swagger from **hardcoded port 5000**. If the discovered API uses another port, run the following from `client/`, setting `API_BASE_URL` to that resource's actual HTTP URL:

```bash
curl --fail --silent --show-error "$API_BASE_URL/openapi/v1.json" -o swagger.json.tmp &&
  test -s swagger.json.tmp && mv swagger.json.tmp swagger.json &&
  pnpm api:orval && pnpm api:remote && pnpm format
```

Do not regenerate from an empty/failed fetch. Orval emits shared types in `src/lib/api/generated/`, server transports in `src/lib/server/api/generated/`, and Zod schemas in `src/lib/server/api/schemas/`; `api:remote` emits the browser-facing SvelteKit remote functions.

Check generated body schemas before implementing forms: conditional FluentValidation property rules (`NotNull`, percentage bounds, etc.) can be inferred as **unconditional** OpenAPI requirements. Use `RuleFor(x => x).Custom(...)` with field-specific `AddFailure(...)` for cross-field conditions, retain unconditional property rules for schema generation, and verify nullable fields and alternative variants remain accepted.

## Recommended Workflow

For a new endpoint, follow [Adding a New Endpoint (Quick Start)](#adding-a-new-endpoint-quick-start) — it is the single source of truth for the end-to-end flow.

## Testing and Verification

Use one or more of these checks:

- Build backend project
- Run backend tests
- Open Scalar at the discovered API URL + `/scalar/v1`
- Regenerate frontend client and verify it succeeds cleanly

Useful commands:

```bash
dotnet build Club.Api/Club.Api.csproj
dotnet test Club.Tests/IntegrationTests/IntegrationTests.csproj
```

## Common Pitfalls

- Forgetting `Description(x => x.WithName(...))`
- Putting endpoints outside `Features/`
- Naming the endpoint file something other than `Endpoint.cs`
- Returning entities when a DTO should be returned
- Skipping `CancellationToken`
- Changing the API without regenerating the frontend client
- Packing too much business logic into the endpoint instead of a service
- Naming a test namespace after an entity (`IntegrationTests.Features.Voucher` shadows `Club.Entities.Voucher` in sibling namespaces); use `Features.Vouchers` instead
- Supplying numeric `InlineData` to nullable `decimal` parameters; use typed `MemberData`/`TheoryData` to avoid reflection conversion failures

## Quick Examples

Anonymous endpoint:

```csharp
public override void Configure()
{
    Get("/public");
    Description(x => x.WithName("PublicGet"));
    AllowAnonymous();
}
```

Authorized endpoint:

```csharp
public override void Configure()
{
    Post("/admin-only");
    Description(x => x.WithName("AdminAction"));
    RequireAuthorization("Admin");
}
```

No-request endpoint:

```csharp
namespace Club.Features.Account.Me;

public class Endpoint(AppDbContext dbContext) : EndpointWithoutRequest<UserModel?>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/account/me");
        Description(x => x.WithName("AccountMe"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = Helpers.GetCurrentUserId(HttpContext);
        var user = await Data.Get(userId, _dbContext, ct);
        await Send.OkAsync(user, ct);
    }
}
```

## When To Escalate Checks

If a task changes routes, contracts, auth, DTO shapes, or serialization behavior, also verify:

- OpenAPI output still generates cleanly
- Frontend generated client still builds
- Existing endpoint names remain stable unless a breaking change is intentional

## References

- FastEndpoints docs: `https://fast-endpoints.com/`
- Repo API docs in dev: discovered API URL + `/scalar/v1`
