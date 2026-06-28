# CommerceSphere Platform

Event-driven e-commerce microservices built on .NET 8. Four domain services communicate through Kafka. A YARP reverse proxy (API Gateway) handles all external traffic and JWT validation.

---

## Services

| Service | Responsibility |
|---|---|
| **Auth** | Registration, JWT login, refresh tokens, SSO via Keycloak (Google / GitHub / Facebook) |
| **Product** | Product catalog — CRUD, SKU, pricing |
| **Inventory** | Stock quantities, reservations per SKU |
| **Cart** | Shopping cart, checkout saga with Inventory |
| **API Gateway** | Routes all traffic, validates JWT, rate limits (200 req/min) |

---

## Documentation

| Guide | Description |
|---|---|
| [Bulk Product Upload — Implementation Guide](docs/Bulk-Product-Upload-Guide.pdf) | Step-by-step walkthrough of how the 30K+ Excel product import works internally (async job, batching, PostgreSQL `COPY`, dedupe & error reporting) with simplified code. |

---

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker Engine + Compose v2)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) — only needed if running services locally outside Docker

---

## Development

### 1. Start the full stack

```bash
docker compose up -d
```

`docker-compose.override.yml` is automatically merged. It sets `ASPNETCORE_ENVIRONMENT=Development` for every service, exposes all ports, and starts Kafka UI and pgAdmin.

That is the only command needed. Wait ~30 seconds for health checks to pass.

### 2. Dev tools

| Tool | URL | Credentials |
|---|---|---|
| API Gateway | http://localhost:5000 | — |
| Auth Swagger | http://localhost:5211/swagger | — |
| Product Swagger | http://localhost:5095/swagger | — |
| Inventory Swagger | http://localhost:5035/swagger | — |
| Cart Swagger | http://localhost:5236/swagger | — |
| Keycloak Admin | http://localhost:8080 | `admin` / `admin_pass` |
| Kafka UI | http://localhost:8090 | — |
| pgAdmin | http://localhost:5050 | `admin@commercesphere.dev` / `admin` |

### 3. Database connections (TablePlus, DataGrip, DBeaver, etc.)

| Database | Host | Port | User | Password |
|---|---|---|---|---|
| auth\_db | localhost | 5433 | commerce | commerce\_pass |
| product\_db | localhost | 5434 | commerce | commerce\_pass |
| inventory\_db | localhost | 5435 | commerce | commerce\_pass |
| cart\_db | localhost | 5436 | commerce | commerce\_pass |
| keycloak\_db | localhost | 5437 | commerce | commerce\_pass |
| Redis | localhost | 6379 | — | — |

### 4. Hybrid mode — infra in Docker, services locally

Use this when iterating on a single service and want fast hot-reload without rebuilding Docker images.

```bash
# Start only infrastructure
docker compose up -d postgres-auth postgres-product postgres-inventory postgres-cart redis kafka keycloak

# Run a service locally (picks up appsettings.Development.json automatically)
dotnet run --project src/Services/Auth/CommerceSphere.AuthService.API
dotnet run --project src/Services/Product/CommerceSphere.ProductService.API
dotnet run --project src/Services/Inventory/CommerceSphere.InventoryService.API
dotnet run --project src/Services/Cart/CommerceSphere.CartService.API
dotnet run --project src/ApiGateway/CommerceSphere.ApiGateway
```

### 5. Build

```bash
# Entire solution
dotnet build CommerceSphere.slnx

# Single service
dotnet build src/Services/Auth/CommerceSphere.AuthService.API/CommerceSphere.AuthService.API.csproj
```

### 6. Stop and clean up

```bash
# Stop containers, keep volumes (data survives)
docker compose down

# Stop and delete all data volumes (fresh start)
docker compose down -v
```

---

## Production

### 1. Set up secrets

Production secrets are never committed. Copy the template and fill in real values:

```bash
cp .env.production.example .env.production
```

Edit `.env.production`:

```env
POSTGRES_USER=commerce
POSTGRES_PASSWORD=<strong-random-password>

# Generate with: openssl rand -base64 48
JWT_SECRET=<generated-secret>
JWT_ISSUER=CommerceSphere
JWT_AUDIENCE=CommerceSphereClients
JWT_EXPIRY_MINUTES=60

KEYCLOAK_ADMIN=admin
KEYCLOAK_ADMIN_PASSWORD=<strong-random-password>
# Get from Keycloak Admin → Clients → commerce-sphere-client → Credentials
KEYCLOAK_CLIENT_SECRET=<from-keycloak-admin>
```

### 2. Deploy

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

The production overlay:
- Sets `ASPNETCORE_ENVIRONMENT=Production` — Swagger is **off**, error details are minimal
- Exposes **only port 5000** (API Gateway) to the host — all DBs, Redis, and Kafka stay on the internal Docker network
- Runs Keycloak in `start` mode (production-hardened, requires TLS)

### 3. Keycloak TLS in production

In production, Keycloak expects to sit behind a TLS-terminating reverse proxy (Nginx, Caddy, Traefik). Update `docker-compose.prod.yml`:

```yaml
keycloak:
  environment:
    KC_PROXY: edge
    KC_HOSTNAME: auth.your-domain.com
```

Then point your reverse proxy at the Keycloak container on port 8080 and terminate TLS externally.

### 4. CI/CD tip

In automated pipelines (GitHub Actions, GitLab CI) inject the production secrets as environment variables instead of writing `.env.production` to disk:

```yaml
# GitHub Actions example
- run: docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
  env:
    POSTGRES_PASSWORD: ${{ secrets.POSTGRES_PASSWORD }}
    JWT_SECRET: ${{ secrets.JWT_SECRET }}
    KEYCLOAK_CLIENT_SECRET: ${{ secrets.KEYCLOAK_CLIENT_SECRET }}
    KEYCLOAK_ADMIN_PASSWORD: ${{ secrets.KEYCLOAK_ADMIN_PASSWORD }}
```

---

## Environment files

| File | Committed | Purpose |
|---|---|---|
| `.env.development` | Yes | Dev secrets — safe throwaway values |
| `.env.production.example` | Yes | Template — shows what production needs |
| `.env.production` | **No** | Real production secrets — gitignored |

---

## Architecture

```
Browser / Mobile App
        │
        ▼ :5000
  ┌─────────────┐   JWT validation, rate limiting, correlation ID
  │ API Gateway │   Routes by path prefix to internal Docker DNS
  └──────┬──────┘
         │
   ┌─────┼─────────────────────┐
   ▼     ▼                     ▼
Auth   Product            Inventory   Cart
Service Service           Service     Service
   │     │                    │          │
   ▼     ▼                    ▼          ▼
auth_db product_db       inventory_db cart_db
(PG)   (PG)              (PG)         (PG)
   │                          │          │
   └──────────────────────────┴──────────┘
                    │
                  Kafka
          (user-created, product-created,
           inventory-reserved, cart-checked-out)
```

**Kafka topics:**

| Topic | Publisher | Consumer |
|---|---|---|
| `user-created` | Auth | Downstream services |
| `product-created` | Product | Inventory |
| `inventory-reserved` | Inventory | Cart (saga success) |
| `inventory-reservation-failed` | Inventory | Cart (saga compensation) |
| `cart-checked-out` | Cart | — |

**Checkout saga:** Cart publishes a reservation request → Inventory responds with success or failure → Cart either finalises the order or rolls back.

---

## Adding a database migration

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Services/<Service>/CommerceSphere.<Service>Service.Infrastructure \
  --startup-project src/Services/<Service>/CommerceSphere.<Service>Service.API
```

Migrations are applied automatically at container startup — no manual `dotnet ef database update` step needed.

---

## Project structure

```
src/
  ApiGateway/
    CommerceSphere.ApiGateway/
  Services/
    Auth/
      CommerceSphere.AuthService.API           ← Controllers, DI wiring
      CommerceSphere.AuthService.Application   ← Managers, DTOs, validators
      CommerceSphere.AuthService.Domain        ← Entities, repo interfaces
      CommerceSphere.AuthService.Infrastructure ← EF Core, Kafka, Redis, Keycloak
    Product/   (same four-layer structure)
    Inventory/ (same four-layer structure)
    Cart/      (same four-layer structure)
  Shared/
    CommerceSphere.Shared.Common     ← ApiResponse<T>, exceptions, middleware
    CommerceSphere.Shared.Contracts  ← Kafka event record types
keycloak/
  realm-export.json                  ← Auto-imported on first Keycloak boot
docker-compose.yml                   ← Base: infrastructure + services
docker-compose.override.yml          ← Dev overlay (auto-loaded)
docker-compose.prod.yml              ← Production overlay (explicit -f)
.env.development                     ← Dev secrets (committed)
.env.production.example              ← Production template (committed)
```
