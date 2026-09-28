# Architecture Guide

## Clean Architecture Overview

This template implements **Clean Architecture** (a.k.a. Onion Architecture, Hexagonal Architecture) with four distinct layers:

```mermaid
graph TD
    subgraph Api_Layer ["Api Layer (Presentation)"]
        API[Minimal APIs · gRPC Services · Scalar OpenAPI · Middlewares]
    end

    subgraph Infrastructure_Layer ["Infrastructure Layer"]
        INFRA[EF Core 10 · PostgreSQL · Identity JWT · HybridCache · Rule Engine]
    end

    subgraph Application_Layer ["Application Layer"]
        APP[CQRS Handlers · FluentValidators · Skill Agents · Business Rules]
    end

    subgraph Domain_Layer ["Domain Layer"]
        DOMAIN[Entities · Value Objects · Result Pattern · Domain Events]
    end

    API --> INFRA
    API --> APP
    INFRA --> APP
    APP --> DOMAIN
    INFRA --> DOMAIN

    classDef api fill:#dae8fc,stroke:#6c8ebf,stroke-width:2px;
    classDef infra fill:#ffe6cc,stroke:#d79b00,stroke-width:2px;
    classDef app fill:#d5e8d4,stroke:#82b366,stroke-width:2px;
    classDef domain fill:#f8cecc,stroke:#b85450,stroke-width:2px;

    class API api;
    class INFRA infra;
    class APP app;
    class DOMAIN domain;
```

## The Dependency Rule

> **Each layer only depends on the layer below it. Never upward.**

This is enforced by:
1. **Project references** — each `.csproj` only references inner layers
2. **Architecture tests** — automated tests verify no dependency violations at build time

### Domain Layer (`TechSpherex.CleanArchitecture.Domain`)

**Dependencies**: None (zero NuGet packages, except Identity for `ApplicationUser`)

**Contains**:
- `Entities/` — Business entities (`TodoItem`, `ApplicationUser`)
- `Common/` — Shared primitives:
  - `BaseEntity` — Base with `Guid Id`
  - `AuditableEntity` — Adds `CreatedAt`, `LastModifiedAt`, audit fields
  - `ITenantEntity` — Multi-tenant marker interface
  - `Result<T>` — Result pattern for explicit error handling
  - `Error` — Typed error with `Code`, `Message`, `ErrorType`
  - `PagedResult<T>` — Pagination wrapper

### Application Layer (`TechSpherex.CleanArchitecture.Application`)

**Dependencies**: Domain only

**Contains**:
- `Abstractions/` — Interfaces (ports):
  - `Messaging/` — `ICommand`, `IQuery`, `ICommandHandler<T>`, `IQueryHandler<T>`
  - `Data/` — `IAppDbContext`
  - `Identity/` — `ICurrentUser`, `ITokenService`
  - `Tenancy/` — `ITenantProvider`, `TenantInfo`
  - `Agents/` — `ISkillAgent`, `IAgentOrchestrator`, `AgentContext`, `AgentResult`
- `Features/` — Vertical slices per feature:
  - `Todos/` — Create, Get, GetAll, Update, Complete, Delete
  - `Identity/` — Register, Login, RefreshToken
  - `Agents/` — AgentOrchestrator, Skills

### Infrastructure Layer (`TechSpherex.CleanArchitecture.Infrastructure`)

**Dependencies**: Application

**Contains**:
- `Persistence/` — EF Core DbContext, Configurations, Migrations, Seeder
- `Identity/` — JWT TokenService, CurrentUser
- `Caching/` — HybridCache configuration
- `Tenancy/` — TenantProvider, TenantMiddleware

### Api Layer (`TechSpherex.CleanArchitecture.Api`)

**Dependencies**: Infrastructure, ServiceDefaults

**Contains**:
- `Endpoints/` — Minimal API endpoint groups
- `Extensions/` — GlobalExceptionHandler, ResultExtensions, ValidationFilter
- `Program.cs` — Application bootstrap

## CQRS Pattern (Manual)

We use **manual CQRS** — no MediatR, no licensing risk.

### Command Flow
```
Endpoint → ICommandHandler<TCommand, TResult> → DbContext → Database
```

### Query Flow
```
Endpoint → IQueryHandler<TQuery, TResult> → DbContext (no tracking) → Response
```

### Registration
Handlers are **auto-discovered** via assembly scanning in `DependencyInjection.cs`:
```csharp
services.AddHandlersFromAssembly(assembly); // Scans for ICommandHandler<,> and IQueryHandler<,>
```

## Multi-Tenancy Architecture

```
Request → TenantMiddleware → Resolve Tenant (Header/JWT/Default)
                                    ↓
                            Set TenantId in scope
                                    ↓
                    AppDbContext.SaveChanges → Auto-set TenantId
                    AppDbContext.Query → Global filter by TenantId
```

See [Multi-Tenancy Guide](multi-tenancy.md) for details.

## Skill Agents Architecture

```
POST /api/agents/execute { prompt: "..." }
         ↓
    AgentOrchestrator → Select Skill (keyword/LLM)
         ↓
    ISkillAgent.ExecuteAsync(context)
         ↓
    AgentResult (Success/Failure/NeedsMoreInfo)
```

See [Skill Agents Guide](skill-agents.md) for details.

## Key Design Decisions

| Decision | Why |
|----------|-----|
| **Manual CQRS** over MediatR | Zero licensing risk. MediatR is commercial since v13. |
| **Scalar** over Swagger UI | Modern, faster, better UX. |
| **HybridCache** over IMemoryCache | Built-in stampede protection, L1+L2 cache. |
| **Result pattern** over exceptions | Explicit error handling, no hidden control flow. |
| **Shared-table multi-tenancy** | Simple, no migration complexity, good for most SaaS apps. |
| **Interface-only agents** | No LLM provider lock-in. Plug in OpenAI, Ollama, etc. |
| **.slnx** over .sln | XML-based, merge-friendly, future .NET standard. |

---

## 📐 System Diagrams & UML Models

### 1. Entity-Relationship Diagram (ERD)

```mermaid
erDiagram
    DEPOT ||--o{ BLOCK : "contains"
    DEPOT ||--o{ CONTAINER_MOVEMENT : "executes_at"
    BLOCK ||--o{ YARD_SLOT : "divided_into"
    
    CONTAINER_TYPE ||--o{ CONTAINER : "classifies"
    CONTAINER_TYPE ||--o{ DELIVERY_ORDER_LINE : "requested_as"
    
    LINE_OPERATOR ||--o{ CONTAINER_MOVEMENT : "owns_operations"
    LINE_OPERATOR ||--o{ DELIVERY_ORDER : "issues_order"
    
    CUSTOMER ||--o{ DELIVERY_ORDER : "places_order"
    DELIVERY_ORDER ||--|{ DELIVERY_ORDER_LINE : "includes"
    
    CONTAINER ||--o{ CONTAINER_MOVEMENT : "tracks_history"
    YARD_SLOT |o--o| CONTAINER_MOVEMENT : "assigned_to"
    DELIVERY_ORDER |o--o{ CONTAINER_MOVEMENT : "fulfills_by"

    DEPOT {
        uuid Id PK
        string Code
        string Name
        string Address
        string TimeZone
        boolean IsActive
        string TenantId
    }

    BLOCK {
        uuid Id PK
        uuid DepotId FK
        string Code
        string Name
        boolean IsVirtual
        int MaxBay
        int MaxRow
        int MaxTier
        int DisplayOrder
    }

    YARD_SLOT {
        uuid Id PK
        uuid BlockId FK
        int Bay
        int Row
        int Tier
        boolean IsOccupied
        uuid CurrentContainerId FK
    }

    CONTAINER {
        uuid Id PK
        string ContainerNumber "ISO 6346 Modulo-11"
        uuid ContainerTypeId FK
        string IsoCode
        int SizeFeet "20ft / 40ft"
        decimal MaxWeightKg
        decimal TareWeightKg
        string Owner
        string Condition
    }

    CONTAINER_TYPE {
        uuid Id PK
        string Code "20GP, 40HC, etc."
        string Name
        string Family
    }

    CONTAINER_MOVEMENT {
        uuid Id PK
        uuid ContainerId FK
        uuid LineOperatorId FK
        uuid YardSlotId FK
        uuid DeliveryOrderId FK
        string Status "InYard | GateOut"
        string Classification
        datetime GateInAt
        datetime GateOutAt
        string VehicleIn
        string DriverIn
    }

    LINE_OPERATOR {
        uuid Id PK
        string Code "MSK, CMA, etc."
        string Name
        string Country
        boolean IsActive
    }

    CUSTOMER {
        uuid Id PK
        string TaxCode
        string Name
        string Email
        string Phone
    }

    DELIVERY_ORDER {
        uuid Id PK
        string OrderNumber
        uuid CustomerId FK
        uuid LineOperatorId FK
        datetime ExpiryDate
        string VesselVoyage
        boolean IsClosed
    }

    DELIVERY_ORDER_LINE {
        uuid Id PK
        uuid DeliveryOrderId FK
        uuid ContainerTypeId FK
        int RequestedQty
        int DeliveredQty
    }
```

---

### 2. UML Use Case Diagram

```mermaid
flowchart LR
    subgraph Actors
        GO((Gate Operator))
        YP((Yard Planner))
        DM((Depot Manager))
        AI((AI Skill Agent))
    end

    subgraph "TechSpherex Container Depot Management System"
        subgraph "Gate Operations (EIR)"
            UC1([Gate-In Container / Validate ISO 6346])
            UC2([Gate-Out Container with Delivery Order])
            UC3([Print EIR Equipment Receipt])
        end

        subgraph "Yard Management"
            UC4([View Live 3D/2D Yard Map])
            UC5([Create / Resize Block Grid])
            UC6([Assign & Shift Container Slot])
        end

        subgraph "Order & Customer"
            UC7([Create Delivery Order])
            UC8([Validate DO Expiry & Quantity Rules])
        end

        subgraph "Reports & Intelligence"
            UC9([Yard Aging Report 0-10d vs >=10d])
            UC10([Daily Gate In/Out Throughput])
            UC11([Natural Language Query Depot Analytics])
        end
    end

    GO --> UC1
    GO --> UC2
    GO --> UC3
    
    YP --> UC4
    YP --> UC5
    YP --> UC6

    DM --> UC7
    DM --> UC9
    DM --> UC10

    UC2 -.->|includes| UC8
    
    AI --> UC11
    UC11 -.->|queries| UC9
    UC11 -.->|queries| UC4
```

---

### 3. UML Class Diagram (Domain & Application CQRS)

```mermaid
classDiagram
    direction TB

    namespace Domain_Common {
        class Entity~TId~ {
            +TId Id
            +DateTime CreatedAt
            +DateTime? UpdatedAt
        }
        class ITenantEntity {
            <<interface>>
            +string TenantId
        }
        class IBusinessRule {
            <<interface>>
            +string Message
            +string Code
            +bool IsBroken()
        }
        class ValueObject
    }

    namespace Domain_Entities {
        class Container {
            +ContainerNumber Number
            +Guid ContainerTypeId
            +int SizeFeet
            +decimal MaxWeightKg
            +UpdateCondition(string condition)
        }
        class ContainerNumber {
            <<ValueObject>>
            +string Value
            +string OwnerCode
            +string SerialNumber
            +int CheckDigit
            +ValidateModulo11()
        }
        class YardSlot {
            +Guid BlockId
            +int Bay
            +int Row
            +int Tier
            +bool IsOccupied
            +Occupy(Guid containerId)
            +Vacate()
        }
        class ContainerMovement {
            +Guid ContainerId
            +Guid LineOperatorId
            +Guid? YardSlotId
            +MovementStatus Status
            +DateTime GateInAt
            +DateTime? GateOutAt
            +CompleteGateOut(Guid doId)
        }
    }

    namespace Application_CQRS {
        class ICommandHandler~TCommand, TResult~ {
            <<interface>>
            +Handle(TCommand command, CancellationToken ct)
        }
        class GateInContainerCommand {
            +string ContainerNo
            +Guid LineOperatorId
            +Guid YardSlotId
            +string DriverIn
        }
        class GateInContainerCommandHandler {
            -IAppDbContext _db
            -IRuleEngine _ruleEngine
            +Handle(GateInContainerCommand)
        }
    }

    Entity <|-- Container
    Entity <|-- YardSlot
    Entity <|-- ContainerMovement
    ITenantEntity <|.. Container
    ITenantEntity <|.. YardSlot
    ITenantEntity <|.. ContainerMovement
    
    Container *-- ContainerNumber : has
    ValueObject <|-- ContainerNumber
    
    GateInContainerCommandHandler ..> GateInContainerCommand : processes
    GateInContainerCommandHandler ..> ContainerMovement : creates
```

---

## 📥 Export & Visual Tools Integration

All diagrams are available in raw source files ready to be imported into **Draw.io**, **Lucidchart**, or **Microsoft Visio**:

- 🎨 **Draw.io / Visio XML**: [`docs/diagrams/depot-full-architecture.drawio`](diagrams/depot-full-architecture.drawio) *(Multi-tab diagram: ERD, Use Case, Class Diagram)*
- 📄 **Structured JSON Specification**: [`docs/diagrams/diagrams.json`](diagrams/diagrams.json) *(Entities, Attributes, Use Cases, Actors schema)*
- 💡 **How to open in Draw.io / Lucidchart**:
  1. Open [draw.io](https://app.diagrams.net) or [lucidchart.com](https://www.lucidchart.com).
  2. Choose **File** → **Open From** / **Import** → select `depot-full-architecture.drawio`.
  3. Or copy the Mermaid snippets above and go to `Arrange` → `Insert` → `Advanced` → `Mermaid`.

