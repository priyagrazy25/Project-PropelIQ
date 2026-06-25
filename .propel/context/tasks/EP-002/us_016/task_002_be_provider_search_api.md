---
post_title: "TASK_002 - Provider Search & Slot API with SignalR"
author1: "AI Senior Developer"
post_slug: "task-002-be-provider-search-api"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-002, US_016, backend, ASP.NET Core, search, SignalR, Redis, caching"
ai_note: "Generated with AI assistance from user story US_016"
summary: "Implement provider search API with specialty/name/date filters, Redis L1 caching, and SignalR real-time slot broadcast."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_PROVIDER_SEARCH_API

## Requirement Reference
- User Story: us_016
- Story Location: .propel/context/tasks/EP-002/us_016/us_016.md
- Acceptance Criteria:
    - AC-1: Search returns matching providers within 2s at p95
    - AC-2: SignalR broadcasts slot changes within 500ms
    - AC-5: Redis L1 cache (30s TTL) checked before database query
- Edge Cases:
    - 100+ results → paginate at 20 per page server-side

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | No |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| Backend | ASP.NET Core | 8.0 LTS |
| ORM | Entity Framework Core | 8.0 |
| Caching | Upstash Redis | 7.x |
| Real-Time | SignalR | 8.x |
| Database | SQL Server Express | 2022 |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | No |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |

## Task Overview
Build the provider search API with specialty, name, and date range filters using EF Core queries against Provider and AppointmentSlot entities. Implement Redis L1 cache (30s TTL) for search results. Broadcast slot availability changes via SignalR AppointmentHub to connected clients. Support server-side pagination at 20 results per page.

## Dependent Tasks
- task_001_be_modular_monolith_setup (US_002) — Requires Scheduling module
- task_001_db_identity_scheduling_entities (US_008) — Requires Provider/AppointmentSlot entities
- task_001_be_caching_realtime_setup (US_004) — Requires Redis and SignalR infrastructure

## Impacted Components
- NEW: ProviderSearchController
- NEW: IProviderSearchService, ProviderSearchService
- NEW: ProviderSearchDto, SlotAvailabilityDto
- MODIFY: AppointmentHub — Add slot-availability broadcast method

## Implementation Plan
1. Create GET /api/scheduling/providers/search endpoint with query params (specialty, name, dateFrom, dateTo, page, pageSize)
2. Implement ProviderSearchService with EF Core query using indexed columns
3. Add Redis L1 cache layer with 30s TTL keyed by search params hash
4. Implement cache invalidation on slot booking/cancellation events
5. Add SignalR broadcast method on AppointmentHub for slot-availability changes
6. Implement event-driven slot change notification (publishes to connected group)
7. Add server-side pagination with total count metadata
8. Add response compression and ETag support for search results

## Current Project State
```
[PLACEHOLDER - Updated after US_002, US_004, US_008 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | src/Modules/Scheduling/Controllers/ProviderSearchController.cs | Search endpoint |
| CREATE | src/Modules/Scheduling/Services/IProviderSearchService.cs | Service interface |
| CREATE | src/Modules/Scheduling/Services/ProviderSearchService.cs | Search logic + cache |
| CREATE | src/Modules/Scheduling/DTOs/ProviderSearchDto.cs | Search response DTOs |
| CREATE | src/Modules/Scheduling/DTOs/SlotAvailabilityDto.cs | Slot availability DTO |
| MODIFY | src/Modules/Scheduling/Hubs/AppointmentHub.cs | Add slot broadcast |
| MODIFY | src/Modules/Scheduling/DependencyInjection.cs | Register search service |

## External References
- Redis caching patterns: https://learn.microsoft.com/aspnet/core/performance/caching/distributed
- SignalR groups: https://learn.microsoft.com/aspnet/core/signalr/groups

## Build Commands
- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy
- [x] GET /api/scheduling/providers/search returns paginated results
- [x] Redis cache hit returns results without DB query
- [x] SignalR broadcasts slot changes within 500ms
- [x] Cache invalidation triggers on booking/cancel events
- [x] 100+ results paginate correctly with total count

## Implementation Checklist
- [x] Create ProviderSearchController with search endpoint and pagination params
- [x] Implement ProviderSearchService with EF Core query on indexed columns
- [x] Add Redis L1 cache with 30s TTL keyed by search params hash
- [x] Implement event-driven cache invalidation on slot mutations
- [x] Add SignalR AppointmentHub broadcast for slot-availability changes
- [x] Implement server-side pagination with total count metadata
- [x] Add response compression and ETag for search results
- [x] Return standardized ProblemDetails for all error responses
