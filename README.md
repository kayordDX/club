# Online

The public site to book and login as member.

## Todo

- [ ] Refresh tokens as own table and not part of user table

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
dotnet ef migrations add InitTables --project src/online/online.csproj
dotnet ef migrations remove --project src/online/online.csproj
```

### Secrets

```bash
dotnet user-secrets init --project api/src/online
dotnet user-secrets set "Authentication:Google:ClientId" "secret" --project api/src/online
dotnet user-secrets set "Authentication:Google:ClientSecret" "secret" --project api/src/online
dotnet user-secrets list --project api/src/online
```

## Client (Front End)

### Start

```bash
# cd client
pnpm dev
```
### Other Todo

- [ ] Test google auth flow
- [ ] Refresh Tokens test
- [ ] Test roles
- [ ] Create entities and test endpoints

### Todo Phase 1

- [ ] Register - Google oauth
- [ ] View Account - Profile Details
- [ ] Log in - Google auth
- [ ] View Timesheet
- [ ] Book slots - booking per day
- [ ] Cancel booking
- [ ] View other players in slot
- [ ] Book a guest
- [ ] View Bookings
- [ ] Book Extras
- [ ] Extra Equipment Agreement
- [ ] Notification Email/SMS
- [ ] Payments - Account per club or 1 account managed by aviate - Create accounts. Link accounts to club or multiple clubs.

slot_config_type
slot_config
resource_slot_config
