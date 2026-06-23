# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

### Full Stack (Docker)
```bash
# Start everything (infra + all services)
docker-compose up -d

# Start infra only for hybrid local dev
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d postgres-auth postgres-product postgres-inventory postgres-cart redis kafka
```

### Individual Services (local)
```bash
dotnet run --project src/Services/Auth/CommerceSphere.AuthService.API
dotnet run --project src/Services/Product/CommerceSphere.ProductService.API
dotnet run --project src/Services/Inventory/CommerceSphere.InventoryService.API
dotnet run --project src/Services/Cart/CommerceSphere.CartService.API
dotnet run --project src/ApiGateway/CommerceSphere.ApiGateway
```

### Build
```bash
dotnet build CommerceSphere.slnx
# Or a single service:
dotnet build src/Services/Auth/CommerceSphere.AuthService.API/CommerceSphere.AuthService.API.csproj
```

### Local Dev Ports
| Component | Port |
|---|---|
| API Gateway | 5000 |
| Auth Service | 5211 (http) / 7044 (https) |
| Product Service | 5095 (http) / 7060 (https) |
| Inventory Service | 5035 (http) / 7267 (https) |
| PostgreSQL (auth) | 5433 |
| PostgreSQL (product) | 5434 |
| PostgreSQL (inventory) | 5435 |
| PostgreSQL (cart) | 5436 |
| Redis | 6379 |
| Kafka | 9094 (external) |

Database credentials: user `commerce`, password `commerce_pass`.

## Architecture

### Overview
Event-driven microservices on .NET 8. Four domain services communicate via Kafka. YARP reverse proxy at `src/ApiGateway/` routes all external traffic. Each service owns its own PostgreSQL database (no shared DB).

### Services
- **Auth** — JWT issuance, refresh tokens, user registration/login
- **Product** — Product catalog (CRUD, SKU, pricing)
- **Inventory** — Stock quantities, reservations per SKU
- **Cart** — Shopping cart, checkout saga orchestration

### Per-Service Layer Structure
Every service follows the same four-project structure:
```
CommerceSphere.[Service]Service.API           ← Controllers, middleware, DI wiring
CommerceSphere.[Service]Service.Application   ← Managers (use cases), DTOs, FluentValidation
CommerceSphere.[Service]Service.Domain        ← Entities (private ctors), repo interfaces, domain events
CommerceSphere.[Service]Service.Infrastructure ← EF Core DbContext, migrations, Kafka producers/consumers, Redis
```

### Shared Libraries
- `src/Shared/CommerceSphere.Shared.Common` — `ApiResponse<T>`, exception types (`NotFoundException`, `ConflictException`, `BusinessException`), correlation ID middleware, global exception handler, Polly resilience policies, Redis idempotency service
- `src/Shared/CommerceSphere.Shared.Contracts` — Kafka event record types shared across services (source of truth for event schemas)

### API Gateway (YARP)
Routes by path prefix to internal Docker DNS names:
- `/api/auth/**` → `auth-service:80`
- `/api/products/**` → `product-service:80`
- `/api/inventory/**` → `inventory-service:80`
- `/api/carts/**` → `cart-service:80`

Features: JWT validation, rate limiting (200 req/min), correlation ID injection, active health checks (10 s interval).

### Kafka Topics
| Topic | Publisher | Consumer |
|---|---|---|
| `user-created` | Auth | (downstream services) |
| `product-created` | Product | Inventory (sync stock record) |
| `inventory-reserved` | Inventory | Cart (saga success path) |
| `inventory-reservation-failed` | Inventory | Cart (saga compensation) |
| `cart-checked-out` | Cart | — |
| `dlq.product-created`, `dlq.cart-checkedout` | — | dead-letter queues |

### Checkout Saga (Cart Service)
Cart publishes a reservation request → Inventory responds with `inventory-reserved` or `inventory-reservation-failed` → Cart either finalises or calls `RollbackAsync` to compensate. Idempotency keys (Redis) prevent duplicate processing on retry.

### Database Migrations
EF Core migrations live in each service's `Infrastructure/Migrations/` folder. Migrations are applied automatically at startup (`MigrateXxxDbAsync()` in `Program.cs`); no manual `dotnet ef database update` step is needed in normal dev.

To add a new migration:
```bash
dotnet ef migrations add <MigrationName> \
  --project src/Services/<Service>/CommerceSphere.<Service>Service.Infrastructure \
  --startup-project src/Services/<Service>/CommerceSphere.<Service>Service.API
```

### Key Conventions
- All API responses use `ApiResponse<T>` from `Shared.Common`.
- Domain entities use private constructors with static factory methods; direct property setters are avoided in domain logic.
- Kafka consumers run as `BackgroundService` in the Infrastructure layer.
- OpenTelemetry tracing and Serilog structured logging (daily rolling files under `logs/`) are wired up in every service via shared helpers.
- C# 12 features enabled: `ImplicitUsings`, `Nullable` on, file-scoped namespaces throughout.
