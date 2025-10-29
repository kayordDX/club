# Online

The public site to book and login as member.

## Todo

- [ ] Refresh tokens as own table and not part of user table

## API (Backend)

```bash
dotnet tool restore
```

### Secrets

```bash
dotnet user-secrets init --project api/src/online
dotnet user-secrets set "Authentication:Google:ClientId" "secret" --project api/src/online
dotnet user-secrets set "Authentication:Google:ClientSecret" "secret" --project api/src/online
dotnet user-secrets list --project api/src/online
```

## Client (Front End)

Client
