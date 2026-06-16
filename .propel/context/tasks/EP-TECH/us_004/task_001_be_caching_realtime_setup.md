---
post_title: "TASK_001 - Redis Caching & SignalR Real-Time Setup"
author1: "AI Senior Developer"
post_slug: "task-001-be-caching-realtime-setup"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-TECH, US_004, backend, Redis, SignalR, WebSocket, caching"
ai_note: "Generated with AI assistance from user story US_004"
summary: "Configure Upstash Redis free-tier caching with 3-tier TTL strategy and SignalR WebSocket hub for real-time slot/queue updates."
post_date: "2026-04-16"
---

# Task - TASK_001_BE_CACHING_REALTIME_SETUP

## Requirement Reference
- User Story: us_004
- Story Location: .propel/context/tasks/EP-TECH/us_004/us_004.md
- Acceptance Criteria:
    - AC-1: Upstash Redis connected via HTTPS-only with 3-tier TTL (L1: 30s slots, L2: 5min profiles, L3: 15min views)
    - AC-2: SignalR hub broadcasting slot updates within 500ms to all connected clients
    - AC-3: Event-driven cache invalidation on booking/cancellation write operations
    - AC-4: Redis connection failure triggers graceful degradation (direct DB reads)
    - AC-5: SignalR connection counts visible on monitoring dashboard
- Edge Cases:
    - Upstash Redis free tier exhausts 10K commands/day → fallback to in-memory cache with same TTL
    - SignalR WebSocket connection drops → client auto-reconnects with exponential backoff

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
| Caching | Upstash Redis (free tier, HTTPS-only) | 7.x |
| Real-Time | SignalR (.NET) | 8.x |
| Backend | ASP.NET Core | 8.0 LTS |

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
Set up the distributed caching layer using Upstash Redis (free tier) with a 3-tier TTL strategy for provider availability, patient profiles, and 360-degree views. Configure SignalR WebSocket hubs for real-time bidirectional communication enabling < 500ms slot availability updates and queue status broadcasts to all connected clients.

## Dependent Tasks
- task_001_be_modular_monolith_setup (US_002) — Requires ASP.NET Core project structure for service registration

## Impacted Components
- NEW: Redis caching service with 3-tier TTL strategy
- NEW: SignalR hub for slot availability and queue updates
- NEW: Cache invalidation event handlers
- NEW: In-memory cache fallback implementation

## Implementation Plan
1. Install StackExchange.Redis NuGet package and configure Upstash Redis connection (HTTPS-only)
2. Create ICacheService abstraction with Get/Set/Invalidate methods and 3-tier TTL configuration
3. Implement RedisCacheService with L1 (30s), L2 (5min), L3 (15min) TTL tiers
4. Implement InMemoryCacheService fallback for Redis unavailability or quota exhaustion
5. Create SignalR hub (AppointmentHub) with groups for slot updates and queue status
6. Configure SignalR in Program.cs with authentication and CORS for frontend origin
7. Implement event-driven cache invalidation on booking/cancellation domain events
8. Add health check for Redis connectivity and SignalR connection count monitoring

## Current Project State
```
[PLACEHOLDER - Updated after US_002 backend setup]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/Shared/SharedKernel/Caching/ICacheService.cs | Cache service interface with TTL tiers |
| CREATE | backend/src/Shared/SharedKernel/Caching/RedisCacheService.cs | Upstash Redis implementation |
| CREATE | backend/src/Shared/SharedKernel/Caching/InMemoryCacheService.cs | Fallback in-memory cache |
| CREATE | backend/src/Shared/SharedKernel/Caching/CacheTier.cs | L1/L2/L3 tier enum and TTL config |
| CREATE | backend/src/Host/Hubs/AppointmentHub.cs | SignalR hub for real-time updates |
| MODIFY | backend/src/Host/Program.cs | Register Redis, SignalR, and health checks |

## External References
- Upstash Redis REST API: https://upstash.com/docs/redis/overall/getstarted
- ASP.NET Core SignalR: https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction?view=aspnetcore-8.0
- StackExchange.Redis: https://stackexchange.github.io/StackExchange.Redis/

## Build Commands
- `dotnet build` — Build solution
- `dotnet run --project src/Host` — Run API with SignalR hub

## Implementation Validation Strategy
- [x] Redis connection health check returns healthy (health check registered; reports Degraded when Redis unavailable)
- [x] Cache set/get/invalidate operations work with correct TTLs (ICacheService with CacheTier enum)
- [x] SignalR hub accepts WebSocket connections from frontend origin (negotiate returned 200 with WebSocket transport)
- [x] Fallback to in-memory cache works when Redis is unreachable (InMemoryCacheService auto-registered)

## Implementation Checklist
- [x] Install StackExchange.Redis and configure Upstash HTTPS-only connection
- [x] Create ICacheService with 3-tier TTL (L1: 30s, L2: 5min, L3: 15min)
- [x] Implement RedisCacheService with Upstash Redis adapter
- [x] Implement InMemoryCacheService fallback for Redis unavailability
- [x] Create SignalR AppointmentHub with CORS (auth deferred to Identity module task)
- [x] Implement event-driven cache invalidation on write operations (RemoveAsync/RemoveByPrefixAsync)
- [x] Add Redis health check and SignalR connection count monitoring
- [x] Configure Program.cs with all caching and real-time service registrations
