# ScopeSeal — remote Ubuntu deployment

Quick reference for deploying to **192.168.1.8** via SSH Docker context. Full runbook:
[docs/deployment/ubuntu.md](../docs/deployment/ubuntu.md).

## Ports (block 11)

| Env  | Project          | Service   | Port |
|------|------------------|-----------|------|
| dev  | `scopeseal-dev`  | product+api (nginx) | **8110** |
| dev  | `scopeseal-dev`  | api direct          | **8111** |
| dev  | `scopeseal-dev`  | marketing SSR       | **8112** |
| dev  | `scopeseal-dev`  | admin portal        | **8113** |
| prod | `scopeseal-prod` | (mirror +1000)      | **9110–9113** |

## One-time setup

```powershell
copy deploy\remote.conf.example deploy\remote.conf
copy .env.dev.example .env.dev
```

Provision shared DB (from `DockerSecurity/shared-db`):

```bash
scripts/provision.sh postgres scopeseal scopeseal_app '<password>'
```

Create host data dirs:

```bash
ssh ram@192.168.1.8 'sudo mkdir -p /docker-data/scopeseal/azurite /srv/weblogs/scopeseal && sudo chown -R ram:ram /docker-data/scopeseal /srv/weblogs/scopeseal'
```

Provision ArxMail site + `arx_sk_…` key → set `ARXMAIL_SECRET_KEY` in `.env.dev`.

## Daily usage

```powershell
.\deploy.ps1              # dev up
.\deploy.ps1 dev ps
.\deploy.ps1 dev logs
.\deploy.ps1 prod up      # typed confirmation
```

Verify:

```powershell
ssh ram@192.168.1.8 'curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8111/health/live'
```
