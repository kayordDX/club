---
name: api
description: Use this skill for API and backend work in the Online project. Apply it when creating, editing, reviewing, or debugging FastEndpoints code under `api/src/online/Features/`, and whenever a request mentions the API, backend, endpoints, DTOs, EF, or client generation.
---

# API Skill

Use this skill for Online API and backend work.

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
- API code location: `api/src/online/Features/`
- Shared DTO location: `api/src/online/DTO/`
- Entities: `api/src/online/Entities/`
- Service registration: `api/src/online/Common/Extensions/`
- Dev API docs: `http://localhost:5000/scalar/v1`

## Core Rules

- Keep API work feature-based under `api/src/online/Features/`
- Use file-scoped namespaces
- Name endpoint files `Endpoint.cs`
- Put request models next to the endpoint in the same action folder
- Keep DTOs in `api/src/online/DTO/` unless there is a clear local-only reason not to
- Pass `CancellationToken` through async database and service calls
- Add `Description(x => x.WithName("FeatureAction"))` in `Configure()`
- Prefer generated frontend clients over manual HTTP calls; regenerate after API changes

## Folder Pattern

Use this structure for new endpoints:

```text
api/src/online/Features/
└── FeatureName/
    └── ActionName/
        ├── Endpoint.cs
        └── FeatureActionRequest.cs
```

Examples:

```text
Features/Outlet/Get/Endpoint.cs
Features/Outlet/Get/OutletGetRequest.cs
Features/Account/Login/Endpoint.cs
Features/Slot/Edit/SlotEditRequest.cs
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
using Online.Data;
using Online.DTO;

namespace Online.Features.Outlet.Get;

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
namespace Online.Features.Outlet.Get;

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

After changing endpoints or response shapes:

1. Start the API
2. Regenerate the frontend client from `client/`
3. Fix warnings before moving on

```bash
pnpm run api
```

Generated client output lives in:

- `client/src/lib/api/generated/`

## Recommended Workflow

For a new endpoint:

1. Create `Features/[Feature]/[Action]/`
2. Add `Endpoint.cs`
3. Add request DTO if needed
4. Reuse or add DTOs under `DTO/`
5. Register services if needed in `Common/Extensions/`
6. Build or test the API
7. Run `pnpm run api` in `client/`
8. Update frontend usage to the generated client

## Testing and Verification

Use one or more of these checks:

- Build backend project
- Run backend tests
- Open Scalar at `http://localhost:5000/scalar/v1`
- Regenerate frontend client and verify it succeeds cleanly

Useful commands:

```bash
dotnet build api/src/online/online.csproj
dotnet test api/tests/
```

## Common Pitfalls

- Forgetting `Description(x => x.WithName(...))`
- Putting endpoints outside `Features/`
- Naming the endpoint file something other than `Endpoint.cs`
- Returning entities when a DTO should be returned
- Skipping `CancellationToken`
- Changing the API without regenerating the frontend client
- Packing too much business logic into the endpoint instead of a service

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
namespace Online.Features.Account.Me;

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
- Repo API docs in dev: `http://localhost:5000/scalar/v1`
