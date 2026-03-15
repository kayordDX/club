# Backend API Skill

This skill provides comprehensive guidance for working with the **Online** backend API built with **FastEndpoints** and .NET 10. Use this skill when creating, editing, or debugging API endpoints in the `api/src/online/Features/` directory.

## Table of Contents

- [FastEndpoints Overview](#fastendpoints-overview)
- [Project Structure](#project-structure)
- [Creating Endpoints](#creating-endpoints)
- [Endpoint Types & Patterns](#endpoint-types--patterns)
- [Folder Organization](#folder-organization)
- [Naming Conventions](#naming-conventions)
- [Request-Endpoint-Response (REPR) Pattern](#request-endpoint-response-repr-pattern)
- [Examples](#examples)
- [Best Practices](#best-practices)
- [API Client Generation](#api-client-generation)
- [Testing Endpoints](#testing-endpoints)

---

## FastEndpoints Overview

**FastEndpoints** is a developer-friendly framework that implements the **REPR Design Pattern** (Request-Endpoint-Response) for building performant .NET APIs.

### Key Benefits

- **Auto-discovery & registration** - Endpoints are automatically discovered in the Features folder
- **Minimal boilerplate** - Clean, declarative endpoint definitions
- **Type-safe** - Strongly-typed request/response DTOs
- **Built-in OpenAPI support** - Automatic Swagger/OpenAPI documentation
- **Dependency injection** - Constructor-based DI with primary constructors
- **Security-first** - Authentication and authorization built-in
- **Performance** - Benchmarks show performance on par with Minimal APIs

### Framework Details

- **NuGet Package**: `FastEndpoints`
- **Auto-registration**: Configured in `Common/Extensions/ApiExtensions.cs`
- **Documentation**: https://fast-endpoints.com/
- **API Docs in Dev**: http://localhost:5000/scalar/v1

---

## Project Structure

```
api/src/online/
├── Features/                    # ✅ All endpoints go here
│   ├── Account/                # Feature grouping
│   │   ├── Login/
│   │   │   ├── Endpoint.cs
│   │   │   └── UserLoginRequest.cs
│   │   ├── Register/
│   │   ├── Me/
│   │   ├── Logout/
│   │   └── Refresh/
│   ├── Outlet/
│   │   ├── Get/
│   │   ├── GetAll/
│   │   └── Admin/
│   ├── Slot/
│   └── Example/
├── Common/
│   ├── Extensions/              # Service configuration
│   ├── Models/                  # Shared models (QueryModel, etc.)
│   └── Helpers/
├── Data/                        # Entity Framework DbContext & Migrations
├── Entities/                    # Database entities
├── DTO/                         # Data Transfer Objects
└── Services/                    # Business logic services
```

---

## Creating Endpoints

### Quick Start: 3 Steps

1. **Create folder** under `Features/[FeatureName]/[Action]/`
2. **Create Request DTO** (if needed) in the action folder
3. **Create Endpoint class** in the action folder

### File Structure for One Endpoint

```
Features/
└── Outlet/
    └── Get/
        ├── Endpoint.cs              # The endpoint handler
        └── OutletGetRequest.cs      # Request DTO (if needed)
```

### Minimum Endpoint Code

**Endpoint with Request and Response:**
```csharp
// Features/Outlet/Get/Endpoint.cs
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

**Endpoint without Request (ReadOnly):**
```csharp
// Features/Account/Me/Endpoint.cs
using Online.Common;
using Online.Data;
using Online.Models;

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

---

## Endpoint Types & Patterns

FastEndpoints provides 4 base classes for different scenarios:

### 1. `Endpoint<TRequest, TResponse>`
**Use when**: You have both a request DTO and response DTO

```csharp
public class Endpoint : Endpoint<OutletGetRequest, OutletDTO>
{
    public override async Task HandleAsync(OutletGetRequest req, CancellationToken ct)
    {
        // Process request, send response
        await Send.OkAsync(new OutletDTO { ... }, ct);
    }
}
```

### 2. `Endpoint<TRequest>`
**Use when**: You only have a request DTO (response can be any serializable object)

```csharp
public class Endpoint : Endpoint<UserLoginRequest>
{
    public override async Task HandleAsync(UserLoginRequest req, CancellationToken ct)
    {
        // You can still send any response
        await Send.OkAsync(new { token = "...", user = new { ... } }, ct);
    }
}
```

### 3. `EndpointWithoutRequest<TResponse>`
**Use when**: There's no request DTO but there is a response DTO

```csharp
public class Endpoint : EndpointWithoutRequest<UserModel?>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(ct);
        await Send.OkAsync(user, ct);
    }
}
```

### 4. `EndpointWithoutRequest`
**Use when**: Neither request nor response DTOs exist

```csharp
public class Endpoint : EndpointWithoutRequest
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.NoContentAsync(ct);
    }
}
```

---

## Folder Organization

### Rule 1: Features First

All endpoints must be organized under the `Features/` folder by feature name:

```
Features/
├── Account/          # Feature: Account management
├── Outlet/           # Feature: Outlet management
├── Slot/             # Feature: Slot management
└── Booking/          # Feature: Booking management (future)
```

### Rule 2: Action Subfolder

Each action gets its own folder within the feature:

```
Features/Outlet/
├── Get/              # Single record
├── GetAll/           # List/paginated
├── Add/              # Create new
└── Edit/             # Update existing
```

### Rule 3: Endpoint Filename

Always name the endpoint class file **`Endpoint.cs`** (not `OutletGetEndpoint.cs`)

```
Features/Outlet/Get/Endpoint.cs          ✅ Correct
Features/Outlet/GetOutletEndpoint.cs     ❌ Wrong
```

### Rule 4: File-Scoped Namespaces

Use file-scoped namespaces (required in this project):

```csharp
// ✅ Correct
namespace Online.Features.Outlet.Get;

public class Endpoint : ...

// ❌ Wrong
namespace Online.Features.Outlet.Get
{
    public class Endpoint : ...
}
```

---

## Naming Conventions

### Endpoint Action Names

Use these standard action names based on HTTP verb and operation:

| HTTP Verb | Operation | Folder Name | DTO Name Prefix | Example Path |
|-----------|-----------|-------------|-----------------|--------------|
| GET | Retrieve one | `Get` | `[Feature]GetRequest` | `/outlet/{id}` |
| GET | Retrieve list | `GetAll` | `[Feature]GetAllRequest` | `/outlets` |
| POST | Create/Insert | `Add` | `[Feature]AddRequest` | `/outlets` |
| PUT/PATCH | Update | `Edit` | `[Feature]EditRequest` | `/outlets/{id}` |
| DELETE | Remove | `Delete` | `[Feature]DeleteRequest` | `/outlets/{id}` |
| POST | Special action | `[ActionName]` | `[ActionName]Request` | `/account/login` |

### Examples

```
Features/Outlet/Get/        → OutletGetRequest      → OutletGet endpoint
Features/Outlet/GetAll/     → OutletGetAllRequest   → OutletGetAll endpoint
Features/Outlet/Add/        → OutletAddRequest      → OutletAdd endpoint
Features/Slot/Edit/         → SlotEditRequest       → SlotEdit endpoint
```

### Endpoint Description Names

Use `Description()` method with `WithName()` to match your folder structure:

```csharp
public override void Configure()
{
    Get("/outlet/{slug}");
    Description(x => x.WithName("OutletGet"));      // ✅ Matches folder
    AllowAnonymous();
}
```

**Description naming pattern**: `[Feature][Action]`
- `OutletGet` - For `Features/Outlet/Get/`
- `OutletGetAll` - For `Features/Outlet/GetAll/`
- `SlotEdit` - For `Features/Slot/Edit/`
- `Logout` - For `Features/Account/Logout/`

---

## Request-Endpoint-Response (REPR) Pattern

The REPR pattern is the foundation of FastEndpoints. It separates concerns into three clear components:

### 1. Request DTO (Input Contract)

```csharp
// Features/Outlet/Add/OutletAddRequest.cs
namespace Online.Features.Outlet.Add;

public class OutletAddRequest
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public required int CityId { get; set; }
}
```

**Characteristics**:
- Placed in the action folder (same as Endpoint.cs)
- Named `[Feature][Action]Request`
- Contains only input fields
- Use `required` keyword for mandatory fields
- Can inherit from `QueryModel` for pagination queries

### 2. Endpoint (Business Logic)

```csharp
// Features/Outlet/Add/Endpoint.cs
namespace Online.Features.Outlet.Add;

public class Endpoint(AppDbContext dbContext) : Endpoint<OutletAddRequest, OutletDTO>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Post("/outlets");
        Description(x => x.WithName("OutletAdd"));
        RequireAuthorization("Admin");  // if needed
    }

    public override async Task HandleAsync(OutletAddRequest req, CancellationToken ct)
    {
        var outlet = new Outlets
        {
            Name = req.Name,
            Slug = req.Slug,
            Description = req.Description,
            CityId = req.CityId
        };

        _dbContext.Outlet.Add(outlet);
        await _dbContext.SaveChangesAsync(ct);

        var response = outlet.ProjectToDto();
        await Send.CreatedAtAsync<Endpoint>(outlet.Id, response, ct);
    }
}
```

**Characteristics**:
- Handles HTTP request routing and business logic
- Uses primary constructor for dependency injection
- Describes HTTP method, route, and security in `Configure()`
- Executes business logic in `HandleAsync()`
- Always passes `CancellationToken` to async operations

### 3. Response DTO (Output Contract)

Response is typically a DTO already defined in your `DTO/` folder:

```csharp
// DTO/OutletDTO.cs
namespace Online.DTO;

public record OutletDTO(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    int CityId,
    DateTime CreatedAt
);
```

**Characteristics**:
- Defined in `DTO/` folder (shared across features)
- Contains only output/display fields
- Should match what the client needs to display
- Can use `record` types for immutability

---

## Examples

### Example 1: Get Single Record

```csharp
// Features/Outlet/Get/Endpoint.cs
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

```csharp
// Features/Outlet/Get/OutletGetRequest.cs
namespace Online.Features.Outlet.Get;

public class OutletGetRequest
{
    public required string Slug { get; set; }
}
```

---

### Example 2: Get List with Pagination

```csharp
// Features/Outlet/GetAll/Endpoint.cs
using Online.Common.Extensions;
using Online.Common.Models;

namespace Online.Features.Outlet.GetAll;

public class Endpoint(AppDbContext dbContext) : Endpoint<OutletGetAllRequest, PaginatedList<Outlets>>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/outlets");
        Description(x => x.WithName("OutletGetAll"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(OutletGetAllRequest req, CancellationToken ct)
    {
        var results = await _dbContext.Outlet
            .OrderBy(x => x.Id)
            .GetPagedAsync(req, ct);

        await Send.OkAsync(results, ct);
    }
}
```

```csharp
// Features/Outlet/GetAll/OutletGetAllRequest.cs
using Online.Common.Models;

namespace Online.Features.Outlet.GetAll;

public class OutletGetAllRequest : QueryModel
{
    // Inherits PageNumber and PageSize from QueryModel
}
```

---

### Example 3: No Request (Read-Only Endpoint)

```csharp
// Features/Account/Me/Endpoint.cs
namespace Online.Features.Account.Me;

public class Endpoint(AppDbContext dbContext) : EndpointWithoutRequest<UserModel?>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/account/me");
        Description(x => x.WithName("AccountMe"));
        // Requires authentication by default
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = Helpers.GetCurrentUserId(HttpContext);
        var user = await Data.Get(userId, _dbContext, ct);
        await Send.OkAsync(user, ct);
    }
}
```

---

### Example 4: Create with POST

```csharp
// Features/Outlet/Add/Endpoint.cs
namespace Online.Features.Outlet.Add;

public class Endpoint(AppDbContext dbContext) : Endpoint<OutletAddRequest, OutletDTO>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Post("/outlets");
        Description(x => x.WithName("OutletAdd"));
        RequireAuthorization("Admin");
    }

    public override async Task HandleAsync(OutletAddRequest req, CancellationToken ct)
    {
        var outlet = new Outlets
        {
            Name = req.Name,
            Slug = req.Slug,
            Description = req.Description
        };

        _dbContext.Outlet.Add(outlet);
        await _dbContext.SaveChangesAsync(ct);

        var response = outlet.ProjectToDto();
        await Send.CreatedAtAsync<Endpoint>(outlet.Id, response, ct);
    }
}
```

```csharp
// Features/Outlet/Add/OutletAddRequest.cs
namespace Online.Features.Outlet.Add;

public class OutletAddRequest
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
}
```

---

### Example 5: Update with PUT

```csharp
// Features/Slot/Edit/Endpoint.cs
namespace Online.Features.Slot.Edit;

public class Endpoint(AppDbContext dbContext) : Endpoint<SlotEditRequest, SlotDTO>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Put("/slots/{id}");
        Description(x => x.WithName("SlotEdit"));
        RequireAuthorization("Admin");
    }

    public override async Task HandleAsync(SlotEditRequest req, CancellationToken ct)
    {
        var slot = await _dbContext.Slot.FindAsync(new object[] { req.Id }, cancellationToken: ct);
        
        if (slot == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        slot.StartTime = req.StartTime;
        slot.EndTime = req.EndTime;
        slot.Capacity = req.Capacity;

        _dbContext.Slot.Update(slot);
        await _dbContext.SaveChangesAsync(ct);

        var response = slot.ProjectToDto();
        await Send.OkAsync(response, ct);
    }
}
```

```csharp
// Features/Slot/Edit/SlotEditRequest.cs
namespace Online.Features.Slot.Edit;

public class SlotEditRequest
{
    public required Guid Id { get; set; }
    public required TimeSpan StartTime { get; set; }
    public required TimeSpan EndTime { get; set; }
    public required int Capacity { get; set; }
}
```

---

## Best Practices

### 1. Use Primary Constructors for Dependency Injection

```csharp
// ✅ Correct: Primary constructor
public class Endpoint(AppDbContext dbContext, ILogger<Endpoint> logger) 
    : Endpoint<MyRequest, MyResponse>
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ILogger<Endpoint> _logger = logger;
}

// ❌ Avoid: Traditional constructor
public class Endpoint : Endpoint<MyRequest, MyResponse>
{
    private readonly AppDbContext _dbContext;
    
    public Endpoint(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
}
```

### 2. Always Use CancellationToken

Pass `CancellationToken` to all async operations:

```csharp
// ✅ Correct
var result = await _dbContext.Outlets
    .FirstOrDefaultAsync(x => x.Id == id, ct);

// ❌ Avoid: Ignoring cancellation token
var result = await _dbContext.Outlets
    .FirstOrDefaultAsync(x => x.Id == id);
```

### 3. Include Description for OpenAPI/Swagger

Always add a description that matches your endpoint name:

```csharp
// ✅ Correct
public override void Configure()
{
    Get("/outlets");
    Description(x => x.WithName("OutletGetAll"));
}

// ❌ Incomplete
public override void Configure()
{
    Get("/outlets");
}
```

### 4. Return Appropriate HTTP Status Codes

```csharp
// GET existing resource
await Send.OkAsync(result, ct);                      // 200

// GET non-existent resource
await Send.NotFoundAsync(ct);                        // 404

// POST successful creation
await Send.CreatedAtAsync<Endpoint>(id, response, ct); // 201

// PUT update
await Send.OkAsync(response, ct);                    // 200

// DELETE
await Send.NoContentAsync(ct);                       // 204

// Validation error
await Send.BadRequestAsync(ct);                      // 400
```

### 5. Validate Requests Early

Use FluentValidation or manual validation:

```csharp
public override async Task HandleAsync(SlotEditRequest req, CancellationToken ct)
{
    // Validate early
    if (req.EndTime <= req.StartTime)
    {
        AddError(r => r.EndTime, "End time must be after start time");
        await Send.BadRequestAsync(ct);
        return;
    }

    // Continue with business logic
    ...
}
```

### 6. Use Async/Await Consistently

```csharp
// ✅ Correct
public override async Task HandleAsync(MyRequest req, CancellationToken ct)
{
    var result = await _dbContext.GetDataAsync(ct);
    await Send.OkAsync(result, ct);
}

// ❌ Avoid: Synchronous blocking
public override async Task HandleAsync(MyRequest req, CancellationToken ct)
{
    var result = _dbContext.GetData();  // Blocking!
    await Send.OkAsync(result, ct);
}
```

### 7. Use `required` Keyword for Mandatory Fields

```csharp
// ✅ Correct: Makes field required
public class OutletAddRequest
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }  // Optional
}

// ❌ Avoid: No compile-time safety
public class OutletAddRequest
{
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Description { get; set; }
}
```

### 8. Separate Concerns: Use Services

Don't put all business logic in the endpoint:

```csharp
// ✅ Correct: Use service layer
public class Endpoint(OutletService outletService) : Endpoint<OutletAddRequest, OutletDTO>
{
    public override async Task HandleAsync(OutletAddRequest req, CancellationToken ct)
    {
        var outlet = await outletService.CreateOutletAsync(req, ct);
        await Send.CreatedAtAsync<Endpoint>(outlet.Id, outlet.ProjectToDto(), ct);
    }
}

// ❌ Avoid: Business logic in endpoint
public class Endpoint(AppDbContext dbContext) : Endpoint<OutletAddRequest, OutletDTO>
{
    public override async Task HandleAsync(OutletAddRequest req, CancellationToken ct)
    {
        // All business logic here...
        var outlet = new Outlets { ... };
        dbContext.Outlet.Add(outlet);
        await dbContext.SaveChangesAsync(ct);
        // More logic...
    }
}
```

### 9. Handle Null Cases Gracefully

```csharp
// ✅ Correct: Check for null
var outlet = await _dbContext.Outlet.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
if (outlet == null)
{
    await Send.NotFoundAsync(ct);
    return;
}

// Use outlet safely
```

### 10. Check for Warnings After API Generation

After running `pnpm run api`, verify no build warnings exist:

```bash
# In client/
pnpm run api
# Check for warnings in generated code
# Fix any issues in your endpoint
```

---

## API Client Generation

### How It Works

1. FastEndpoints exposes an **OpenAPI schema** at `/openapi/v1.json`
2. **Orval** (configured in `client/orval.config.ts`) generates TypeScript types
3. Generated files go to `client/src/lib/api/generated/`

### After Creating an Endpoint

1. Start the API server
2. Run in `client/` directory:
   ```bash
   pnpm run api
   ```
3. New endpoint will be available in `src/lib/api/generated/`

### Using Generated API Client

```typescript
// In your Svelte component
import { createQuery } from '@tanstack/svelte-query';
import { getOutletGetAll } from '$lib/api/generated';

const outletsQuery = createQuery({
    queryKey: ['outlets'],
    queryFn: () => getOutletGetAll()
});
```

### Important

- Always ensure your endpoint has a **Description with WithName()**
- **No warnings** should appear after generation
- If you see warnings, fix the endpoint first, then regenerate

---

## Testing Endpoints

### Manual Testing via Scalar UI

When the API is running in development:

1. Open: http://localhost:5000/scalar/v1
2. Find your endpoint in the list (sorted alphabetically)
3. Click to expand
4. Try it out button
5. Provide request body/parameters
6. See response

### Integration Testing

```csharp
// Using FastEndpoints test helpers
[Fact]
public async Task GetOutlet_ShouldReturnOutlet_WhenSlugExists()
{
    // Arrange
    var client = new TestClient();
    var outlet = new Outlet { Slug = "main-outlet" };
    
    // Act
    var response = await client
        .GetAsync("/outlet/main-outlet");
    
    // Assert
    response.Should().Be(200);
    var result = await response.Content.ReadAsAsync<OutletDTO>();
    result.Slug.Should().Be("main-outlet");
}
```

---

## Common Tasks

### Add New Endpoint

1. Create folder: `Features/[Feature]/[Action]/`
2. Create request DTO: `[Feature][Action]Request.cs`
3. Create endpoint: `Endpoint.cs`
4. Run `pnpm run api` in client/
5. Use in frontend from generated client

### Add Authentication

```csharp
public override void Configure()
{
    Get("/protected-resource");
    Description(x => x.WithName("ProtectedGet"));
    RequireAuthorization();  // Requires auth token
}
```

### Add Role-Based Authorization

```csharp
public override void Configure()
{
    Post("/admin-only");
    Description(x => x.WithName("AdminAction"));
    RequireAuthorization("Admin");  // Only users with Admin role
}
```

### Add Public Endpoint (No Auth)

```csharp
public override void Configure()
{
    Get("/public");
    Description(x => x.WithName("PublicGet"));
    AllowAnonymous();  // No authentication required
}
```

### Handle File Uploads

```csharp
public class Endpoint : Endpoint<FileUploadRequest>
{
    public override void Configure()
    {
        Post("/upload");
        Description(x => x.WithName("FileUpload"));
        AllowFileUploads();
    }

    public override async Task HandleAsync(FileUploadRequest req, CancellationToken ct)
    {
        foreach (var file in req.Files)
        {
            // Process file
            await file.SaveAsAsync($"/path/{file.Name}", ct);
        }
        
        await Send.OkAsync(new { message = "Files uploaded" }, ct);
    }
}
```

---

## Troubleshooting

### Issue: API Generation Fails

**Solution**:
- Run `dotnet build` to check for compilation errors
- Verify `Description(x => x.WithName(...))` exists on all endpoints
- Check that endpoint returns valid serializable objects

---

## Related Documentation

- **FastEndpoints Docs**: https://fast-endpoints.com/
---

## Quick Reference: HTTP Methods

```csharp
Get("/route")       // Retrieve data (no body)
Post("/route")      // Create new (with body)
Put("/route")       // Replace/Update entire resource (with body)
Patch("/route")     // Partial update (with body)
Delete("/route")    // Remove resource (no/minimal body)
```

## Quick Reference: Send Methods

```csharp
await Send.OkAsync(data, ct)                    // 200 OK
await Send.CreatedAtAsync<Endpoint>(id, data, ct)  // 201 Created
await Send.AcceptedAsync(ct)                    // 202 Accepted
await Send.BadRequestAsync(ct)                  // 400 Bad Request
await Send.NotFoundAsync(ct)                    // 404 Not Found
await Send.UnauthorizedAsync(ct)                // 401 Unauthorized
await Send.ForbiddenAsync(ct)                   // 403 Forbidden
await Send.ConflictAsync(ct)                    // 409 Conflict
await Send.NoContentAsync(ct)                   // 204 No Content
await Send.InternalErrorAsync(ct)               // 500 Internal Error
```

---
