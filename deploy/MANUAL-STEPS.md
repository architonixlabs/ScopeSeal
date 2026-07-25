# ScopeSeal deploy — manual steps checklist

One-time and recurring tasks on the deploy host (**192.168.1.8**) and your workstation.
`.env.dev` / `.env.prod` ship with **sensible dev-friendly defaults** (not production secrets).
You can deploy dev after host setup without hunting for placeholder strings — but you must still
align the database password with `provision.sh` and replace the ArxMail key after gateway provision.

Full runbook: [docs/deployment/ubuntu.md](../docs/deployment/ubuntu.md).

## Workstation (Windows / Git Bash)

- [ ] **SSH access** — `ssh ram@192.168.1.8 docker version` succeeds
- [ ] **`deploy/remote.conf`** — copy from `deploy/remote.conf.example` if missing; adjust `REMOTE_USER` if needed
- [ ] **`.env.dev`** — defaults pre-filled; confirm `DB_PASSWORD` matches provision (see below)
- [ ] **`.env.prod`** — only when preparing prod; override **all** defaults; never reuse dev secrets
- [ ] **Docker context** — created automatically by `deploy.ps1` / `deploy.sh` on first run

## Deploy host (192.168.1.8)

### 1. Shared Postgres network

- [ ] **`arx-dev-db` stack running** — from `DockerSecurity/shared-db`
- [ ] **External network exists** — created by shared-db stack (do not recreate if already present):

```bash
docker network ls | grep arx-dev-db
# Only if missing (normally not needed):
# docker network create arx-dev-db
```

### 2. Provision ScopeSeal database + role

From a machine with access to `DockerSecurity/shared-db`:

```bash
export DOCKER_CONTEXT=avalokh-ubuntu
cd /path/to/DockerSecurity/shared-db
set -a; . ./.env; set +a
scripts/provision.sh postgres scopeseal scopeseal_app 'scopeseal_dev_2026'
```

- [ ] Password in `.env.dev` as `DB_PASSWORD` **matches** provision script (default: `scopeseal_dev_2026`)
- [ ] If you chose a different password at provision time, update `.env.dev` to match — do not leave the default
- [ ] Do **not** copy credentials from `scan.env` or other secret stores into `.env`

### 3. Persistent data directories

```bash
ssh ram@192.168.1.8 'sudo mkdir -p /docker-data/scopeseal/azurite /srv/weblogs/scopeseal && sudo chown -R ram:ram /docker-data/scopeseal /srv/weblogs/scopeseal'
```

- [ ] `/docker-data/scopeseal/azurite` exists (Azurite bind-mount)
- [ ] `/srv/weblogs/scopeseal` exists (nginx → GoAccess at `:7010`)

### 4. ArxMail Gateway site + secret key

On the gateway host (see `arxlabs.com/email/INTEGRATION.md`):

```bash
docker --context avalokh-ubuntu compose -p arxmail-dev exec gateway \
  node dist/cli/admin.js provision \
  --slug scopeseal \
  --name "ScopeSeal" \
  --mailbox contact@architonixlabs.com \
  --origins https://scopeseal.app,http://192.168.1.8:8110,http://192.168.1.8:8112
```

- [ ] Replace `ARXMAIL_SECRET_KEY` in `.env.dev` with the printed `arx_sk_…` (default is a placeholder)
- [ ] Prod: provision a **separate** site/key for `.env.prod` and override the prod placeholder

### 5. JWT secret

- [ ] Dev default is pre-filled (`scopeseal-dev-jwt-signing-key-minimum-32-characters-long`); change if desired
- [ ] Prod: override with a unique secret — never reuse the dev `JWT_SECRET`

### 6. Razorpay (optional — Loop 10)

- [ ] Test keys from Razorpay dashboard → `.env.dev` (`rzp_test_…` placeholders)
- [ ] Set `ScopeSeal__Billing__Mode=Enabled` only when ready to test billing
- [ ] Live keys only after CA/tax review — never in dev `.env`

## Deploy

```powershell
.\deploy.ps1              # dev up (default)
.\deploy.ps1 dev ps
.\deploy.ps1 dev logs
.\deploy.ps1 prod up      # requires typing project slug to confirm
```

Git Bash: `./deploy.sh dev up`

## Verify after deploy

```powershell
docker --context avalokh-ubuntu compose -p scopeseal-dev ps
ssh ram@192.168.1.8 'curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8111/health/live'
ssh ram@192.168.1.8 'curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8110/'
ssh ram@192.168.1.8 'curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8112/'
```

Expected: HTTP `200` on health and frontends.

## SonarQube (optional static analysis)

- [ ] Source credentials at scan time — **never** copy into `.env`:

```powershell
# From DockerSecurity/code-platform/scan.env (do not commit tokens)
$env:SONAR_URL = '...'   # http://192.168.1.8:7001
$env:SONAR_TOKEN = '...'  # from scan.env only
docker run --rm -v "${PWD}:/usr/src" -w /usr/src sonarsource/sonar-scanner-cli `
  -Dsonar.host.url=$env:SONAR_URL -Dsonar.token=$env:SONAR_TOKEN
```

Project key: `architonix-scopeseal` (see `sonar-project.properties`).

## Port reference (block 11)

| Env  | Project          | Service              | Port |
|------|------------------|----------------------|------|
| dev  | `scopeseal-dev`  | nginx (product + api)| 8110 |
| dev  | `scopeseal-dev`  | api direct           | 8111 |
| dev  | `scopeseal-dev`  | marketing            | 8112 |
| dev  | `scopeseal-dev`  | admin portal         | 8113 |
| prod | `scopeseal-prod` | (mirror +1000)       | 9110–9113 |

## Secrets and defaults

| Variable | File | Default / action |
|----------|------|------------------|
| `DB_PASSWORD` | `.env.dev` | `scopeseal_dev_2026` — must match `provision.sh` password |
| `DB_PASSWORD` | `.env.prod` | `scopeseal_prod_2026` placeholder — override before prod |
| `JWT_SECRET` | `.env.dev` | Pre-filled dev signing key (32+ chars); override optional |
| `JWT_SECRET` | `.env.prod` | Pre-filled placeholder — **must** override before prod |
| `ARXMAIL_SECRET_KEY` | `.env.dev`, `.env.prod` | Placeholder — replace after ArxMail `provision` CLI |
| `ScopeSeal__Billing__Razorpay__*` | `.env.dev`, `.env.local` | Obvious test placeholders; swap for dashboard keys when testing |
| `ScopeSeal__Auth__JwtSecret` | `.env.local` | Pre-filled local signing key |

## Local Windows dev (unchanged)

Does not use `.env.dev` — uses `.env.local` + `docker-compose.local.yml`:

```powershell
docker compose -f docker-compose.local.yml --profile local up -d
start.bat
```
