---
post_title: "TASK_001 - ASP.NET Core Modular Monolith Setup"
author1: "AI Senior Developer"
post_slug: "task-001-be-modular-monolith-setup"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-TECH, US_002, backend, ASP.NET Core, modular-monolith, DI"
ai_note: "Generated with AI assistance from user story US_002"
summary: "Bootstrap ASP.NET Core 8.0 LTS Web API with modular monolith architecture, Clean Architecture per module, and DI-based module decoupling."
post_date: "2026-04-16"
---

# Task - TASK_001_BE_MODULAR_MONOLITH_SETUP

## Requirement Reference
- User Story: us_002
- Story Location: .propel/context/tasks/EP-TECH/us_002/us_002.md
- Acceptance Criteria:
    - AC-1: ASP.NET Core 8.0 LTS Web API project with 4 domain modules (Scheduling, Clinical, Identity, Notification)
    - AC-2: Clean Architecture layers per module (Domain, Application, Infrastructure, Presentation)
    - AC-3: Inter-module communication via DI-registered interfaces only (no direct cross-module references)
    - AC-4: Solution compiles and runs with Swagger UI accessible
    - AC-5: Module health check endpoints returning status for each module
- Edge Cases:
    - Circular dependency detected at compile time → DI container startup fails with descriptive error
    - Module fails to initialize → other modules continue operating; health check reports degraded state

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | No |
| **Figma URL** | N/A |
| **Wireframe Status** | N/A |
| **Wireframe Type** | N/A |
| **Wireframe Path/URL** | N/A |
| **Screen Spec** | N/A |
| **UXR Requirements** | N/A |
| **Design Tokens** | N/A |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| Backend | ASP.NET Core Web API | 8.0 LTS |
| ORM | Entity Framework Core | 8.0 |
| API Documentation | Swashbuckle (OpenAPI 3.0) | 6.x |
| DI Container | Microsoft.Extensions.DependencyInjection | 8.0 |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | No |
| **AIR Requirements** | N/A |
| **AI Pattern** | N/A |
| **Prompt Template Path** | N/A |
| **Guardrails Config** | N/A |
| **Model Provider** | N/A |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |
| **Platform Target** | N/A |
| **Min OS Version** | N/A |
| **Mobile Framework** | N/A |

## Task Overview
Create the ASP.NET Core 8.0 LTS solution with a modular monolith architecture enforcing clean domain boundaries across Scheduling, Clinical, Identity, and Notification modules. Each module follows Clean Architecture (Onion) with Domain, Application, Infrastructure, and Presentation layers. Inter-module communication uses DI-registered interfaces to prevent direct coupling. Swagger/OpenAPI documentation is auto-generated.

## Dependent Tasks
- None (foundational backend task)

## Impacted Components
- NEW: Solution file with modular monolith project structure
- NEW: 4 domain modules with Clean Architecture layers
- NEW: Shared kernel project for cross-cutting concerns
- NEW: Program.cs with module registration and Swagger configuration

## Implementation Plan
1. Create .NET 8.0 solution with folder-based module organization
2. Create Shared Kernel project for common abstractions (IRepository, IUnitOfWork, Result<T>)
3. For each module (Scheduling, Clinical, Identity, Notification): create Domain, Application, Infrastructure, and API (Presentation) class library projects
4. Define inter-module interfaces in Shared Kernel, implement in each module's Infrastructure layer
5. Register all module services in Program.cs using module-level extension methods (e.g., `services.AddSchedulingModule()`)
6. Configure Swashbuckle for OpenAPI 3.0 documentation with versioned endpoints
7. Add per-module health check implementations
8. Verify no cross-module project references exist (only Shared Kernel references allowed)

## Current Project State
```
backend/
  UnifiedPatientAccess.sln
  src/
    Shared/SharedKernel/
      Domain/ (BaseEntity, IRepository, IUnitOfWork, Result)
      Extensions/ (IModuleInstaller)
    Modules/
      Identity/ (Domain, Application, Infrastructure, API)
      Scheduling/ (Domain, Application, Infrastructure, API)
      Clinical/ (Domain, Application, Infrastructure, API)
      Notification/ (Domain, Application, Infrastructure, API)
    Host/ (Program.cs, Swagger, HealthChecks)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/UnifiedPatientAccess.sln | Solution file with all module projects |
| CREATE | backend/src/Shared/SharedKernel/ | Common abstractions, interfaces, Result types |
| CREATE | backend/src/Modules/Identity/Identity.Domain/ | Identity domain entities and value objects |
| CREATE | backend/src/Modules/Identity/Identity.Application/ | Identity use cases, DTOs, validators |
| CREATE | backend/src/Modules/Identity/Identity.Infrastructure/ | Identity EF Core, external service adapters |
| CREATE | backend/src/Modules/Identity/Identity.API/ | Identity controllers and endpoints |
| CREATE | backend/src/Modules/Scheduling/Scheduling.Domain/ | Scheduling domain entities |
| CREATE | backend/src/Modules/Scheduling/Scheduling.Application/ | Scheduling use cases |
| CREATE | backend/src/Modules/Scheduling/Scheduling.Infrastructure/ | Scheduling data access |
| CREATE | backend/src/Modules/Scheduling/Scheduling.API/ | Scheduling endpoints |
| CREATE | backend/src/Modules/Clinical/Clinical.Domain/ | Clinical domain entities |
| CREATE | backend/src/Modules/Clinical/Clinical.Application/ | Clinical use cases |
| CREATE | backend/src/Modules/Clinical/Clinical.Infrastructure/ | Clinical data access |
| CREATE | backend/src/Modules/Clinical/Clinical.API/ | Clinical endpoints |
| CREATE | backend/src/Modules/Notification/Notification.Domain/ | Notification domain entities |
| CREATE | backend/src/Modules/Notification/Notification.Application/ | Notification use cases |
| CREATE | backend/src/Modules/Notification/Notification.Infrastructure/ | Notification adapters |
| CREATE | backend/src/Modules/Notification/Notification.API/ | Notification endpoints |
| CREATE | backend/src/Host/Program.cs | Application entry point with module registration |

## External References
- ASP.NET Core 8.0 Documentation: https://learn.microsoft.com/en-us/aspnet/core/?view=aspnetcore-8.0
- Modular Monolith Pattern: https://learn.microsoft.com/en-us/azure/architecture/patterns/modular-monolith
- Swashbuckle Docs: https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle

## Build Commands
- `dotnet restore` — Restore NuGet packages
- `dotnet build` — Build entire solution
- `dotnet run --project src/Host` — Run the API host

## Implementation Validation Strategy
- [x] `dotnet build` succeeds with zero warnings
- [x] API starts and Swagger UI accessible at /swagger
- [x] Each module's health check endpoint returns healthy
- [x] No cross-module project references (only SharedKernel)

## Implementation Checklist
- [x] Create .NET 8.0 solution with folder-based module structure
- [x] Create SharedKernel project with common abstractions (IRepository, Result<T>, base entities)
- [x] Create Identity module with Domain/Application/Infrastructure/API layers
- [x] Create Scheduling module with Domain/Application/Infrastructure/API layers
- [x] Create Clinical module with Domain/Application/Infrastructure/API layers
- [x] Create Notification module with Domain/Application/Infrastructure/API layers
- [x] Configure Program.cs with module DI registration and Swagger/OpenAPI
- [x] Add per-module health check implementations and validate no cross-module violations
