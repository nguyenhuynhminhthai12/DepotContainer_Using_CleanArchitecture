# 🏗️ TechSpherex Container Depot Management System

Production-grade Clean Architecture system for managing **container depots** (Block → Bay → Row → Tier yard layout, Gate In/Out EIR, Delivery Orders, aging reports).

Built on top of the **TechSpherex Clean Architecture Template** (.NET 10, PostgreSQL, gRPC, HybridCache, ELK, Grafana, Multi-Tenancy, AI Skill Agents). The template's full feature set — Postgres + Redis + gRPC + Aspire + OpenTelemetry + Rule Engine + Docker — is wired into real depot workflows, not left as a Todo demo.

---

## 🎯 What's New vs. the Template

| Template (Todos demo) | Depot (this project) |
|---|---|
| `TodoItem` CRUD | `Block` / `YardSlot` / `Container` / `DeliveryOrder` / `ContainerMovement` / `Customer` / `LineOperator` / `Depot` |
| — | Yard Map (Block × Bay × Row × Tier grid, virtual blocks, resize) |
| — | Gate In / Gate Out with ISO 6346 Modulo-11 container-number validation |
| — | Delivery Orders (not-expired + quantity-available business rules) |
| — | Yard Aging Report (0–10 days vs ≥10 days by Line Operator) |
| — | Daily Throughput Report (Gate-In / Gate-Out by Line Operator) |
| `TodoAgentSkill` | `DepotQueryAgentSkill` — natural-language queries ("How many MSC containers stuck >10 days?") |
| `TodoService` gRPC | `ContainerService` + `YardService` gRPC (share same CQRS handlers) |
| — | Angular 19 frontend (`client/`) with JWT + `X-Tenant-Id` interceptor |

---

## 📐 Domain Model (ERD)

```
┌────────────┐        ┌─────────────┐        ┌──────────────┐
│   Depot    │ 1    N │   Block     │ 1    N │  YardSlot    │
│------------│────────│-------------│────────│--------------│
│ Id         │        │ Id          │        │ Id           │
│ Code       │        │ DepotId  FK │        │ BlockId   FK │
│ Name       │        │ Code        │        │ Bay          │
│ Address    │        │ Name        │        │ Row          │
│ TimeZone   │        │ IsVirtual   │        │ Tier         │
│ IsActive   │        │ MaxBay?     │        │ IsOccupied   │
└────┬───────┘        │ MaxRow?     │        │ CurrentCntrId│
     │                │ MaxTier?    │        └──────────────┘
     │ 1              │ DisplayOrder│
     │                └─────────────┘
     │
     │ N
     ▼
┌────────────────┐      ┌─────────────────┐      ┌──────────────────┐
│   Container    │ 1  N │ ContainerMvmt   │ N  1 │   LineOperator   │
│----------------│──────│-----------------│──────│------------------│
│ Id             │      │ Id              │      │ Id               │
│ ContainerNumber│      │ ContainerId  FK │      │ Code (CMA/MSK…)  │
│   (ISO 6346)   │      │ LineOperatorIdFK│      │ Name             │
│ ContainerTypeId│      │ YardSlotId   FK?│      │ Country          │
│ IsoCode        │      │ BlockId      FK?│      │ IsActive         │
│ SizeFeet       │      │ Classification  │      └──────────────────┘
│ MaxWeightKg    │      │ ConditionIn/Out │
│ TareWeightKg   │      │ VehicleIn/Out   │      ┌──────────────────┐
│ ManufactureDate│      │ DriverIn/Out    │ N  1 │   DeliveryOrder  │
│ Owner          │      │ GateInAt        │──────│------------------│
│ Condition      │      │ GateOutAt?      │      │ Id               │
└────────────────┘      │ Status (InYard/ │      │ OrderNumber      │
        ▲               │  GateOut)       │      │ CustomerId    FK │
        │ N             │ DeliveryOrderId?│      │ LineOperatorIdFK │
┌───────┴────────┐      └─────────────────┘      │ ExpiryDate       │
│ ContainerType │                                │ VesselVoyage     │
│---------------│                                │ IsClosed         │
│ Id            │                                └────────┬─────────┘
│ Code (22G1…)  │                                         │ 1
│ Name          │                                         │
│ Family        │                                         │ N
│ Description   │                                ┌────────▼─────────┐
└───────────────┘                                │ DeliveryOrderLine│
                                                │------------------│
┌─────────────┐        ┌────────────────────────│ Id               │
│  Customer   │ 1    N │ DeliveryOrderLine       │ DeliveryOrderIdFK│
│------------─│────────│ (qty per container type)│ ContainerTypeIdFK│
│ Id          │        └─────────────────────────┘ RequestedQty     │
│ TaxCode     │                                  DeliveredQty     │
│ Name        │                                                  │
└─────────────┘                                                  │
                                                                 │ N
                                                 ┌───────────────┴──┐
                                                 │  ContainerType    │
                                                 └───────────────────┘
```

### Notes

- Every entity implements `ITenantEntity` so EF Core auto-filters by `TenantId`. Resolve tenant from header `X-Tenant-Id`, JWT claim, or default `default`.
- `Container.ContainerNumber` is stored raw (string) but exposed as `ContainerNumber` value object that validates **ISO 6346 Modulo-11**.
- `YardSlot` has a composite unique index `(BlockId, Bay, Row, Tier)`.
- `ContainerMovement` is the **EIR** (Equipment Interchange Receipt) — one lifecycle entry per container through the depot.

---

## 🌐 Business Rules (Domain)

All implemented as `IBusinessRule` in `Domain/Common/Rules/`:

| Rule | Code | Purpose |
|---|---|---|
| `ContainerNumberCheckDigitRule` | `Container.NumberCheckDigit` | ISO 6346 Modulo-11 validation |
| `BayParityMatchesContainerSizeRule` | `Yard.BayParityMatchesContainerSize` | Odd Bays = 20ft, Even Bays = 40ft |
| `YardSlotNotOccupiedRule` | `Yard.SlotNotOccupied` | One container per slot |
| `DeliveryOrderNotExpiredRule` | `DeliveryOrder.NotExpired` | Gate-Out requires non-expired order |
| `DeliveryOrderQuantityAvailableRule` | `DeliveryOrder.QuantityAvailable` | Gate-Out respects remaining qty |

Two rule sets are also exposed via the config-driven `RuleEngine` (`appsettings.json` → `RuleEngine.RuleSets.GateInValidation` and `GateOutValidation`).

---

## 🔌 REST Endpoints

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/auth/login` | Sign in (returns JWT + refresh) |
| `POST` | `/api/auth/refresh` | Refresh access token |
| `POST` | `/api/blocks` | Create a non-virtual Block |
| `POST` | `/api/blocks/virtual` | Create a virtual Block (no grid) |
| `PATCH` | `/api/blocks/{id}/resize` | Resize Block grid + auto-create slots |
| `GET` | `/api/yard/depots/{id}/map` | Live yard map (Block × Slot occupancy) |
| `POST` | `/api/containers` | Register container (validates Modulo-11) |
| `GET` | `/api/containers?page=&pageSize=` | List containers (search + filter) |
| `GET` | `/api/containers/{number}` | Get by container number |
| `POST` | `/api/gate/in` | Gate-In (opens EIR) |
| `POST` | `/api/gate/out` | Gate-Out (closes EIR, requires Delivery Order) |
| `POST` | `/api/gate/move` | Move container inside yard |
| `GET` | `/api/containers/{number}/movements` | EIR history |
| `POST` | `/api/delivery-orders` | Create release order |
| `GET` | `/api/delivery-orders/active` | List active orders |
| `GET` | `/api/delivery-orders/{id}` | Get by ID |
| `POST` | `/api/delivery-orders/{id}/close` | Close order |
| `GET` | `/api/reports/yard-aging` | 0–10 / ≥10 days by Line Operator |
| `GET` | `/api/reports/daily-throughput` | Gate-In / Gate-Out by day + Line Operator |
| `GET` | `/api/lookups/line-operators` | List line operators |
| `GET` | `/api/lookups/container-types` | List container types |
| `POST` | `/api/agents/execute` | Natural-language depot query (AI Skill) |

All write endpoints require `[Authorize]` with role `YardOperator` or `Admin`.

---

## 📡 gRPC Services

Defined in `src/Api/Protos/{container,yard}.proto` and implemented in `src/Api/GrpcServices/{Container,Yard}GrpcService.cs`. Both services **reuse the same CQRS handlers** as the REST endpoints — zero business-logic duplication.

| gRPC RPC | REST equivalent |
|---|---|
| `ContainerService.GetContainer` | `GET /api/containers/{number}` |
| `ContainerService.GateIn` | `POST /api/gate/in` |
| `ContainerService.GateOut` | `POST /api/gate/out` |
| `YardService.GetYardMap` | `GET /api/yard/depots/{id}/map` |

REST + gRPC share HTTP/1.1 + HTTP/2 on the **same Kestrel port** (no separate server).

---

## 🤖 AI Skill Agent — `DepotQueryAgentSkill`

Built on the template's pluggable `ISkillAgent` interface. Operators ask natural-language questions; the skill routes to the matching CQRS handler:

| Prompt (example) | Routes to |
|---|---|
| "How many MSC containers are stuck >10 days?" | `GetYardAgingReportQuery` |
| "What is the daily throughput for CMA CGM?" | `GetDailyThroughputReportQuery` |
| "How many containers are in the yard?" | LINQ group-by over `ContainerMovements` |

Swap the keyword router for an LLM (OpenAI / Ollama / Semantic Kernel) without touching the handlers — the template's `AgentOrchestrator` accepts any `ISkillAgent`.

---

## 🏢 Multi-Tenancy

Each Depot = one tenant. The template's shared-table strategy with EF Core global query filters applies automatically to every entity implementing `ITenantEntity`.

**Resolution order:**
1. `X-Tenant-Id` HTTP header
2. `tenant_id` JWT claim
3. `MultiTenancy:DefaultTenantId` from `appsettings.json` (default: `"default"`)

**Run with a single depot (most common):** leave `MultiTenancy.Enabled = true` and the default seed creates one `DEFAULT` depot. **Run multiple depots** on one instance: add the `Depot` row, then send the depot code as `X-Tenant-Id` per request.

---

## 🚀 Quick Start

### Option A — Aspire (recommended)

```bash
cd src/TechSpherex.CleanArchitecture.AppHost
dotnet run
```

Auto-starts PostgreSQL, Redis, API, Aspire Dashboard. Seed runs on first boot.

### Option B — Docker Compose (4 profiles preserved)

```bash
# Core stack (API + Postgres + Redis)
docker compose up -d --build

# + ELK
docker compose --profile elk up -d --build

# + Grafana (Loki + Tempo + Prometheus + Grafana)
docker compose --profile grafana up -d --build

# + Tools (pgAdmin + RedisInsight)
docker compose --profile tools up -d
```

Scalar API docs: `http://localhost:8080/scalar/v1`
Default admin: `admin@TechSpherex.dev / Admin@123`

### Frontend

```bash
cd client
npm install
npm start          # http://localhost:4200
```

The dev proxy in `client/proxy.conf.json` rewrites `/api/*` → `http://localhost:8080`.

---

## 🧪 Tests

```bash
dotnet test src/TechSpherex.CleanArchitecture.slnx \
    /p:CopyLocalLockFileAssemblies=true \
    /p:CollectCoverage=true \
    /p:CoverletOutput=TestResults/coverage/ \
    /p:CoverletOutputFormat=cobertura
```

Coverage gate (Jenkinsfile / Cobertura): **Application layer ≥ 80% line coverage**.

Test breakdown:

| Test class | Coverage |
|---|---|
| `ContainerNumberCheckDigitRuleTests` | All cases: valid, invalid check digit, wrong length, invalid characters |
| `DomainRuleTests` | Bay parity, slot occupancy, DO expiry, DO quantity |
| `YardHandlerTests` | CreateBlock, CreateVirtualBlock, ResizeBlock, GetYardMap |
| `ContainerHandlerTests` | Create / get / list containers |
| `GateHandlerTests` | GateIn, GateOut, Move, history |
| `DeliveryOrderHandlerTests` | Create / close / get / active list |
| `ReportHandlerTests` | Yard aging, daily throughput |
| `DepotQueryAgentSkillTests` | Keyword routing + handler invocation |
| `ExtraHandlerTests` | Misc edge cases |

Architecture tests (`Architecture.Tests`) verify dependency rules and add a check that every entity in `Domain/Entities` implements `ITenantEntity`.

---

## 🚢 CI/CD

`Jenkinsfile` runs: **restore → build → test (+ coverage ≥ 80%) → architecture tests → docker build JIT + AOT → smoke test → push to DockerHub**.

Required Jenkins plugins: `Pipeline`, `Docker Pipeline`, `xUnit`, `Cobertura`.

---

## 📁 Project Structure (delta from template)

```
TechSpherex.CleanArchitecture/
├── src/
│   ├── Domain/
│   │   ├── Common/ContainerNumber.cs                  # NEW value object
│   │   ├── Common/Rules/
│   │   │   ├── ContainerNumberCheckDigitRule.cs       # NEW Modulo-11
│   │   │   ├── BayParityMatchesContainerSizeRule.cs   # NEW
│   │   │   ├── DeliveryOrderNotExpiredRule.cs
│   │   │   ├── DeliveryOrderQuantityAvailableRule.cs
│   │   │   └── YardSlotNotOccupiedRule.cs
│   │   └── Entities/
│   │       ├── Depot.cs                               # NEW
│   │       ├── Block.cs                               # NEW
│   │       ├── YardSlot.cs                            # NEW
│   │       ├── ContainerType.cs                       # NEW
│   │       ├── LineOperator.cs                        # NEW
│   │       ├── Customer.cs                            # NEW
│   │       ├── Container.cs                           # NEW
│   │       ├── DeliveryOrder.cs                       # NEW (with DeliveryOrderLine)
│   │       └── ContainerMovement.cs                   # NEW (EIR)
│   ├── Application/Features/
│   │   ├── Yard/                                      # NEW
│   │   ├── Containers/                                # NEW
│   │   ├── Gate/                                      # NEW
│   │   ├── DeliveryOrders/                            # NEW
│   │   ├── Reports/                                   # NEW
│   │   ├── Lookups/                                   # NEW
│   │   └── Agents/Skills/DepotQueryAgentSkill.cs      # NEW AI Skill
│   ├── Infrastructure/Persistence/
│   │   ├── Configurations/{Block,Container,...}Configuration.cs   # NEW (9 configs)
│   │   ├── AppDbContext.cs                            # MOD — 9 new DbSets
│   │   └── AppDbSeeder.cs                             # MOD — seed ContainerType, LineOperator, Depot, Blocks, Slots
│   └── Api/
│       ├── Endpoints/{Yard,Container,Gate,DeliveryOrder,Report}Endpoints.cs   # NEW
│       ├── GrpcServices/{Container,Yard}GrpcService.cs                       # NEW
│       └── Protos/{container,yard}.proto                                     # NEW
├── client/                                            # NEW Angular 19 frontend
│   ├── package.json
│   ├── angular.json
│   ├── tsconfig.json
│   ├── proxy.conf.json
│   └── src/app/
│       ├── core/
│       │   ├── services/{auth,auth.store,container,delivery-order,gate,report,yard}.service.ts
│       │   ├── interceptors/auth.interceptor.ts        # JWT + X-Tenant-Id
│       │   ├── guards/auth.guard.ts
│       │   └── models/api.models.ts
│       └── features/{login,yard-map,containers,gate,delivery-orders,reports}/*.component.ts
├── Jenkinsfile                                         # NEW
├── DEPOT_README.md                                     # NEW (this file)
└── (template README / docs / docker / Dockerfile unchanged)
```

---

## 📜 License

MIT — see [LICENSE](LICENSE).
