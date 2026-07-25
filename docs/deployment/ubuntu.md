# Deploying ScopeSeal to Ubuntu (192.168.1.8)

ScopeSeal runs on the shared Ubuntu deploy host via an **SSH Docker context** — builds and
containers execute on the server; your Windows machine only runs the Docker CLI.

## Stack (dev — block 11)

| Service   | Host port | URL / notes |
|-----------|-----------|-------------|
| nginx     | **8110**  | Product app + `/api/` proxy → http://192.168.1.8:8110 |
| api       | **8111**  | Direct API / health → http://192.168.1.8:8111/health/live |
| marketing | **8112**  | Marketing SSR site → http://192.168.1.8:8112 |
| admin     | **8113**  | Admin portal → http://192.168.1.8:8113 |
| worker    | internal  | Background extraction/processing jobs |
| azurite   | internal  | Blob storage; data on `/docker-data/scopeseal/azurite` |

Prod mirrors ports in the **911x** band (+1000) via `docker-compose.prod.yml`.

**Database:** dev uses the shared **`arx-dev-db`** Postgres — no Postgres container in this stack.
Connect via Docker network service name `postgres:5432`, database `scopeseal`, role `scopeseal_app`.

**Analytics:** nginx access logs bind-mount to `/srv/weblogs/scopeseal` for the centralized
GoAccess stack at http://192.168.1.8:7010.

**Email:** outbound mail via **ArxMail Gateway** (`POST https://mail.architonixlabs.com/v1/submit`,
Bearer `arx_sk_…` in server env only).

## One-time setup on the deploy host

### 1. SSH + Docker context

```powershell
ssh ram@192.168.1.8 docker version
copy deploy\remote.conf.example deploy\remote.conf
copy .env.dev.example .env.dev
```

Edit `.env.dev` with real secrets (never commit). `deploy.ps1` auto-creates the `avalokh-ubuntu`
Docker context if missing.

### 2. Ensure `arx-dev-db` network exists

The shared DB stack must already be running (`DockerSecurity/shared-db`). The external network
`arx-dev-db` is created by that stack.

### 3. Provision ScopeSeal database + role

On a machine that can reach the shared-db scripts (from `J:\Projects\Architonix\DockerSecurity\shared-db`):

```bash
export DOCKER_CONTEXT=avalokh-ubuntu
cd /j/Projects/Architonix/DockerSecurity/shared-db
set -a; . ./.env; set +a
scripts/provision.sh postgres scopeseal scopeseal_app '<choose-a-strong-password>'
```

Put the same password in `.env.dev` as `DB_PASSWORD`.

### 4. Create persistent data directories

```bash
ssh ram@192.168.1.8 'sudo mkdir -p /docker-data/scopeseal/azurite /srv/weblogs/scopeseal && sudo chown -R ram:ram /docker-data/scopeseal /srv/weblogs/scopeseal'
```

### 5. Provision ArxMail site + secret key

On the ArxMail gateway host (see `arxlabs.com/email/INTEGRATION.md`):

```bash
docker --context avalokh-ubuntu compose -p arxmail-dev exec gateway \
  node dist/cli/admin.js provision \
  --slug scopeseal \
  --name "ScopeSeal" \
  --mailbox contact@architonixlabs.com \
  --origins https://scopeseal.app,http://192.168.1.8:8110,http://192.168.1.8:8112
```

Store the printed **`arx_sk_…`** secret in `.env.dev` as `ARXMAIL_SECRET_KEY` (server env only).

## Daily usage (from repo root)

```powershell
.\deploy.ps1              # dev up (build + start) — default
.\deploy.ps1 dev ps       # status
.\deploy.ps1 dev logs     # tail logs
.\deploy.ps1 dev down     # stop dev stack
.\deploy.ps1 prod up      # prod (typed slug confirmation)
```

Git Bash twin: `./deploy.sh dev up`

Actions: `up` · `down` · `logs` · `ps` · `restart` · `build` · `config`

## Verify after deploy

```powershell
docker --context avalokh-ubuntu compose -p scopeseal-dev ps
ssh ram@192.168.1.8 'curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8111/health/live'
ssh ram@192.168.1.8 'curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8110/'
ssh ram@192.168.1.8 'curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8112/'
```

EF Core migrations apply automatically when `ASPNETCORE_ENVIRONMENT=Development` (dev stack default).

## Local Windows dev (unchanged workflow)

Isolated Postgres + Azurite for local-only development:

```powershell
docker compose -f docker-compose.local.yml --profile local up -d
start.bat
```

Or run `start.bat` directly — it starts the local profile automatically.

## SonarQube

Project key: `architonix-scopeseal`. Scan from a machine that can reach `http://192.168.1.8:7001`:

```powershell
# Source SONAR_URL + SONAR_TOKEN from DockerSecurity/code-platform/scan.env — never copy tokens into chat.
docker run --rm -v "${PWD}:/usr/src" -w /usr/src `
  sonarsource/sonar-scanner-cli `
  -Dsonar.host.url=$env:SONAR_URL `
  -Dsonar.token=$env:SONAR_TOKEN
```

## Dual-mode summary

| Mode | Compose file | Database | Use case |
|------|--------------|----------|----------|
| Local Windows | `docker-compose.local.yml --profile local` | Container Postgres | `start.bat` daily dev |
| Deploy dev | `docker-compose.yml` via `deploy.ps1` | Shared `arx-dev-db` | LAN testing on 192.168.1.8 |
| Deploy prod | `docker-compose.yml` + `docker-compose.prod.yml` | Dedicated prod DB (TBD) | Production |
