# ScopeSeal

India-first SaaS that converts fragmented business conversations and supporting material into clear, versioned **Agreement Snapshots** with approval records and a **Change Ledger**.

ScopeSeal is a communication-clarity and scope-organisation utility — not legal advice, not a guarantee of enforceability, and not certified legal evidence.

## Status

**Loop 1 complete** — modular monolith backend scaffold, Angular multi-app workspace, Docker Compose, and CI foundation.

## Documentation

| Area | Path |
|------|------|
| Product vision | [docs/product/product-vision.md](docs/product/product-vision.md) |
| Personas & journeys | [docs/product/](docs/product/) |
| Architecture | [docs/architecture/](docs/architecture/) |
| Privacy | [docs/privacy/](docs/privacy/) |
| Security | [docs/security/](docs/security/) |
| Backlog | [docs/backlog/product-backlog.md](docs/backlog/product-backlog.md) |
| Implementation ledger | [docs/backlog/implementation-ledger.md](docs/backlog/implementation-ledger.md) |

## Technology

- **Backend:** .NET 10, ASP.NET Core, EF Core (PostgreSQL in Loop 2+)
- **Frontend:** Angular 21 multi-project workspace (product, marketing SSR, admin)
- **Architecture:** Modular monolith
- **Billing:** Razorpay (test mode in Loop 10)

## Local development

### Prerequisites

- .NET 10 SDK
- Node.js 22+ and npm
- Docker Desktop (PostgreSQL + Azurite)

### Start infrastructure

```powershell
docker compose up -d
```

### Backend API

```powershell
cd src/backend
dotnet run --project hosts/ScopeSeal.Api
```

Health: `http://localhost:5000/health/live`  
System status: `http://localhost:5000/api/v1/system/status`  
OpenAPI (Development): `http://localhost:5000/openapi/v1.json`

### Frontend

```powershell
cd src/clients
npm install
npm run start:product     # product app
npm run start:marketing   # marketing SSR app
npm run start:admin       # admin portal
```

Build all clients: `npm run build`

### Tests

```powershell
cd src/backend
dotnet test ScopeSeal.slnx
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## Security

See [SECURITY.md](SECURITY.md).

## Agent Instructions

See [AGENTS.md](AGENTS.md) for Cursor delivery loops and engineering standards.

## Legal Disclaimer

All customer-facing legal templates and notices are **DRAFT FOR QUALIFIED INDIAN LEGAL REVIEW BEFORE PUBLICATION**.
