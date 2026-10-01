# Production deployment

Club uses the same SSH → Docker Compose → `docker rollout` approach as POS.
`Deploy and Build All` runs on a published release or manually from GitHub Actions. It builds both
multi-architecture images, waits for both builds, then rolls out `club-api` followed by
`club`, using the triggering commit's SHA tag (not a potentially stale `latest`).
`Deploy All` runs manually and performs only the same deployment steps, without building.
Both images must already exist with the selected ref's commit SHA tag.
Standalone `Build API` / `Build Client` workflows still build images but do not deploy.
Both deployment workflows share the same concurrency lock; a failed build never reaches the server. Configure the
GitHub `production` environment with required reviewers if releases need approval.

## Install on the server

Use a Linux amd64 or arm64 server. Install:

- Docker Engine and the Docker Compose plugin ([official installation guide](https://docs.docker.com/engine/install/)).
- OpenSSH server, `curl`, and `openssl` (on Ubuntu/Debian: `sudo apt-get install openssh-server curl openssl`).
- [`docker-rollout`](https://github.com/wowu/docker-rollout), installed as the SSH deployment user:

```sh
mkdir -p ~/.docker/cli-plugins
curl -fsSL https://raw.githubusercontent.com/wowu/docker-rollout/v0.14/docker-rollout \
  -o ~/.docker/cli-plugins/docker-rollout
chmod +x ~/.docker/cli-plugins/docker-rollout
docker compose version
docker rollout --version
```

The deployment user must be able to run Docker without `sudo` (membership of the
`docker` group grants root-equivalent access). Install its SSH public key in
`~/.ssh/authorized_keys`. Only allow SSH from trusted sources where practical.
No .NET SDK, Node, pnpm, or Aspire installation is needed on the server.

### Existing services required

`deploy/compose.yml` contains **only Club's two application services**. It reuses the
existing POS infrastructure; it does not install or modify those services:

- Postgres: create a **dedicated `club` database and user**, not the POS database.
  The user must own the database and be permitted to run migrations.
- Redis: configure the correct service hostname/password in `CLUB_REDIS_CONNECTION`.
- Production Keycloak served over HTTPS, reachable by browsers and both app containers.
- Traefik with a TLS entrypoint and certificate resolver. Adjust `TRAEFIK_ENTRYPOINT`
  and `TRAEFIK_CERT_RESOLVER` to match the existing server. Point the Club hostname's DNS
  at this server and attach Traefik, Postgres, Redis, and Club to `DOCKER_NETWORK`.

If deploying on a new server, provision these services first with persistent storage,
backups, and TLS. The sample defaults to the existing `kayord_default` Docker network.
Keep Postgres, Redis, and the API private: expose only the proxy's 80/443 and SSH.
The sample deliberately has **no host `ports` or `container_name`** on the app services,
which are incompatible with rollout's temporary extra replicas.
The API trusts forwarded headers, so do not give untrusted containers access to its network.

Traefik labels expose only the frontend; SvelteKit proxies API requests internally to
`http://club-api:5000`. The API `/health` checks Postgres, Redis, and process memory in
production. The frontend health check verifies that its homepage responds successfully.
Health checks do not validate Keycloak, SMTP, S3, or payment-provider credentials.

## GitHub configuration

Add these **Actions secrets** to the repository or its `production` environment:

| Secret             | Value                                                                      |
| ------------------ | -------------------------------------------------------------------------- |
| `HOST`             | Server hostname/IP                                                         |
| `USERNAME`         | SSH deployment user                                                        |
| `KEY`              | Private SSH key (matching the installed public key)                        |
| `HOST_FINGERPRINT` | Server SSH host key SHA256 fingerprint, verified through a trusted channel |

Get the fingerprint on the server, for example:

```sh
sudo ssh-keygen -lf /etc/ssh/ssh_host_ed25519_key.pub -E sha256
```

Set Actions **variable** `DEPLOY_PATH` to the absolute directory containing the server's
Compose file and env files, e.g. `/home/deploy/club`. If omitted, it uses `~/kayord`, like POS.
When using that existing directory, merge the `club` and `club-api` definitions into its
Compose file rather than overwriting it, and keep its existing environment values.
The workflow grants `packages: write` to image builds; no separate registry push secret is needed.

For **private GHCR images**, authenticate Docker on the server once as the deployment
user with a GitHub PAT (classic) with `read:packages` and access to both packages:

```sh
read -rs GHCR_TOKEN
printf '%s' "$GHCR_TOKEN" | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
unset GHCR_TOKEN
```

Public images do not require a registry login. Do not put the registry token in Compose env files.

## Environment on the server

Copy `deploy/.env.example` to `.env` next to the server's `compose.yml` and replace all
placeholders. Never commit `.env`. Generate the three encryption values independently
with `openssl rand -hex 32`; keep the session secret identical across frontend replicas.

| Variables                                                            | Purpose                                                                                             |
| -------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| `DOCKER_NETWORK`, `TRAEFIK_ENTRYPOINT`, `TRAEFIK_CERT_RESOLVER`      | Existing Docker network and Traefik TLS configuration                                               |
| `CLUB_HOST`                                                          | Public hostname only, e.g. `club.example.com`                                                       |
| `APP_URL`                                                            | Public HTTPS frontend URL, without trailing slash; also sets `ORIGIN`, CORS, and payment return URL |
| `CLUB_DATABASE_CONNECTION`                                           | Npgsql connection string for the dedicated Club database                                            |
| `CLUB_REDIS_CONNECTION`                                              | StackExchange.Redis connection string                                                               |
| `KEYCLOAK_URL`, `KEYCLOAK_REALM`, `IDENTITY_URL`                     | Keycloak HTTPS root URL, realm name, and full realm issuer URL                                      |
| `KEYCLOAK_AUDIENCE`                                                  | Audience present in access tokens accepted by the API                                               |
| `KEYCLOAK_ADMIN_CLIENT_ID`, `KEYCLOAK_ADMIN_CLIENT_SECRET`           | Confidential Keycloak service-account client credentials                                            |
| `SESSION_SECRET`                                                     | At least 32 characters; encrypts frontend session cookies                                           |
| `APP_ENCRYPTION_KEY`, `APP_ENCRYPTION_SALT`                          | Backend encryption material for stored payment-provider settings; retain securely and back up       |
| `TICKERQ_USERNAME`, `TICKERQ_PASSWORD`                               | Scheduler dashboard credentials; replace insecure defaults                                          |
| `SMTP_HOST`, `SMTP_PORT`, `SMTP_EMAIL`, `SMTP_PASSWORD`, `SMTP_NAME` | Booking email SMTP settings (`SMTP_EMAIL` is also the login username)                               |
| `S3_REGION`, `S3_BUCKET`, `S3_ACCESS_KEY_ID`, `S3_SECRET_ACCESS_KEY` | Image-upload bucket and credentials                                                                 |

The Compose file maps these to the API's `Section__Property` configuration keys.
SMTP/S3 values may be omitted only if those features are unused. Configure payment-provider
credentials through the application's stored provider settings; production startup does
not seed demo data or payment credentials. Never rotate the backend encryption key/salt
without migrating the encrypted data first.

### Keycloak configuration

Use the existing `kayord` realm or create a production realm. Do **not** blindly import
`Club.AppHost/keycloak/realms/kayord.json` in production; it is a local-development seed.
The frontend client ID is hardcoded to `public-client` (there is no client-ID env variable).
Configure it as a public client with authorization-code/standard flow and PKCE S256:

- Valid redirect URI: `https://club.example.com/auth/callback`
- Web origin: `https://club.example.com`
- Valid post-logout redirect URI: `https://club.example.com/`
- Audience mapper: include the value of `KEYCLOAK_AUDIENCE` in access tokens.

Enable service accounts on the confidential admin client and grant only the realm-management
permissions needed for user synchronization and session management. Generate a new production
client secret and set `KEYCLOAK_ADMIN_CLIENT_SECRET`. Configure Google federation in Keycloak
if using Google login. Ensure `IDENTITY_URL` matches the tokens' issuer exactly.

## First start

For a dedicated deployment directory (copy these files from this checkout):

```sh
mkdir -p ~/club
cp deploy/compose.yml ~/club/compose.yml
cp deploy/.env.example ~/club/.env
cd ~/club
chmod 600 .env
# Edit .env with production values before continuing.
# Use a SHA tag already built by both image workflows (recommended), or latest.
printf 'CLUB_IMAGE_TAG=latest\n' > .release.env
chmod 600 .release.env
docker compose --env-file .env --env-file .release.env config --quiet
docker compose --env-file .env --env-file .release.env pull club-api club
docker compose --env-file .env --env-file .release.env up -d --wait --wait-timeout 180 club-api club
```

For an existing POS deployment, add the two services to its Compose file and merge the
example env values instead of running the copy commands. Ensure `.release.env` exists.
Set `DEPLOY_PATH` accordingly. The workflow never copies files or runs `git pull` on the
server; update Compose/configuration files manually when they change in this repo.

Verify HTTPS homepage, login/logout, booking, email delivery, uploads, and payment callbacks
before enabling release-triggered production deployments. Back up the database first:
API startup automatically applies both application and TickerQ migrations.
Rollouts overlap old/new API processes, so migrations must be backwards compatible.

## Deployments and recovery

Publish a release or run **Deploy and Build All** manually on the intended ref. Both builds
must succeed before SSH runs. To deploy without rebuilding, run **Deploy All** manually on
a ref whose commit SHA has already been built by both image workflows.
Both SHA-tagged images are pulled before either service changes.
After both rollouts succeed, `.release.env` records the deployed SHA for subsequent manual
Compose commands. Only dangling images are pruned (not volumes or unrelated running services).

A failed health check causes docker-rollout to remove the new replicas and retain the old
ones. This is **not an atomic two-service rollback**: if the API succeeds but the frontend
fails, the API stays upgraded. `.release.env` still records the last fully successful pair.
First deployment should use `up --wait` as above, since rollout's first-start path does not
wait for health. In-flight requests are not guaranteed to drain; see the plugin's
[draining documentation](https://docker-rollout.wowu.dev/container-draining) if required.

To roll both services back to a previously built SHA, after checking database compatibility:

```sh
cd /home/deploy/club
export CLUB_IMAGE_TAG=PREVIOUS_SUCCESSFUL_COMMIT_SHA
docker compose --env-file .env --env-file .release.env pull club-api club
docker rollout --env-file .env --env-file .release.env --timeout 180 club-api
docker rollout --env-file .env --env-file .release.env --timeout 180 club
printf 'CLUB_IMAGE_TAG=%s\n' "$CLUB_IMAGE_TAG" > .release.env
unset CLUB_IMAGE_TAG
```

Database migrations are not reversed by an image rollback. Preserve previous image tags,
back up secrets and data, and monitor `docker compose ... ps` / `logs` during recovery.
