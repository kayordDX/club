# Club

The public site to book and login as member.

## Structure

- `Club.AppHost/` - Aspire AppHost for local orchestration
- `Club.Api/` - FastEndpoints backend
- `Club.ServiceDefaults/` - shared Aspire defaults
- `Club.Tests/` - backend integration and unit tests
- `client/` - SvelteKit frontend

## Setup

```bash
mkcert -install
mkdir -p ./container/traefik/certs
# rm -rf ./container/traefik/certs
mkcert -cert-file ./container/traefik/certs/local-cert.pem -key-file ./container/traefik/certs/local-key.pem "localhost" "*.localhost" "auth.localhost"
# copy certs to ./container/traefik/certs
```

## Run with Aspire

Start the full local stack through the AppHost:

```bash
dotnet run --project Club.AppHost/Club.AppHost.csproj
```

This orchestrates the API, frontend, Postgres, Redis, Keycloak, and Mailpit for local development.

Useful local endpoints when the AppHost is running:

- Aspire AppHost: `http://localhost:15283`
- API docs: `http://localhost:5000/scalar/v1`
- Frontend: `http://localhost:5173`
- Keycloak realm: `http://localhost:8088/realms/kayord`

## API (Backend)

```bash
# tool restore and update
dotnet tool restore
dotnet tool update --all

# List updates
dotnet list package --outdated
# Update packages
dotnet package update

# ef
dotnet ef migrations add InitTables --project Club.Api/Club.Api.csproj --startup-project Club.Api/Club.Api.csproj -c AppDbContext -o ./Data/Migrations
dotnet ef migrations remove --project Club.Api/Club.Api.csproj --startup-project Club.Api/Club.Api.csproj

# remove
dotnet ef migrations remove --project Club.Api/Club.Api.csproj --startup-project Club.Api/Club.Api.csproj -c AppDbContext -o ./Data/Migrations

# list dbContexts
dotnet ef dbcontext list --project Club.Api/Club.Api.csproj --startup-project Club.Api/Club.Api.csproj

# TickerQ
dotnet ef migrations add TickerQInit --project Club.Api/Club.Api.csproj --startup-project Club.Api/Club.Api.csproj -c TickerQDbContext -o ./Data/TickerQMigrations

# squash migrations
dotnet steward squash Club.Api/Data/Migrations

# run the API by itself
dotnet run --project Club.Api/Club.Api.csproj
```

### Secrets

```bash
dotnet user-secrets init --project Club.Api/Club.Api.csproj
dotnet user-secrets set "Authentication:Google:ClientId" "secret" --project Club.Api/Club.Api.csproj
dotnet user-secrets set "Authentication:Google:ClientSecret" "secret" --project Club.Api/Club.Api.csproj

dotnet user-secrets set "AWS:AccessKeyId" "secret" --project Club.Api/Club.Api.csproj
dotnet user-secrets set "AWS:SecretAccessKey" "secret" --project Club.Api/Club.Api.csproj

dotnet user-secrets list --project Club.Api/Club.Api.csproj
```

## Client (Front End)

### Start

```bash
pnpm --dir client dev
```

## Principles

- Simplicity
- Guests should be able to book
- Re-use same UI for members guests and staff where possible
- URLs should be deterministic.
- Mobile First
- Code first DB design

### Diagram

```mermaid
architecture-beta
    group club(server)[Club]

    service db(database)[Postgres] in club
    service redis(database)[Redis] in club
    service api(server)[api] in club
    service svelte(internet)[Svelte] in club

    svelte:B -- T:api
    db:L -- R:api
    redis:R -- L:api
```

```mermaid
flowchart TB
  subgraph club [Club]
    direction BT

    %% Nodes with shapes matching their service types
    svelte[Svelte]
    api([api])
    db[(Postgres)]
    redis[(Redis)]

    %% Edge connections mimicking the original layout
    svelte --> api
    db <--> api
    redis <--> api
  end
```

### Temp Booking validation types?

Types of validation checks

- Pre Check (This check happens before a booking is created) - Check if slots are available. Only allow if you lower price if you are part of it.
- Check (This check happens before booking status can become confirmed)

- Logged in
- Has Contract (Needs params, can be comma seperated list?)
- Has Handicap

- Other option can book for guests. All should be members?

Payments can you allow no payment and accept payment on arrival?

Payment Options?

- Pay before
- Pay on arrival
- Deposit %

### Auth Plan

Sync user accounts with identity
Call /users endpoint in identity and sync
Add column lastSync to users table.
Only update if lastUpdate is older than 24 hours.

## Facility Information

Outlet
Description
Address
GPSLink
Tags
Contact Number
Email Address

Facility
Facility Name
Contact Number
Email Address

Operating Hours
Rule

## Clean up

- Remove old code
- UserManager.FindByEmailAsync() remove all these type of references
