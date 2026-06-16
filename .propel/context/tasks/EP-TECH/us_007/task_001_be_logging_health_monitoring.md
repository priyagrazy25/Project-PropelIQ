---
post_title: "TASK_001 - Serilog Logging & Health Monitoring"
author1: "AI Senior Developer"
post_slug: "task-001-be-logging-health-monitoring"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-TECH, US_007, backend, Serilog, health-checks, PHI-scrubbing"
ai_note: "Generated with AI assistance from user story US_007"
summary: "Configure Serilog structured logging with PHI-scrubbing enrichers and health check endpoints (/health/live, /health/ready) for monitoring."
post_date: "2026-04-16"
---

# Task - TASK_001_BE_LOGGING_HEALTH_MONITORING

## Requirement Reference
- User Story: us_007
- Story Location: .propel/context/tasks/EP-TECH/us_007/us_007.md
- Acceptance Criteria:
    - AC-1: Serilog configured with structured JSON logging and configurable sinks
    - AC-2: PHI-scrubbing enricher strips sensitive data from all log entries
    - AC-3: /health/live returns 200 when process is running
    - AC-4: /health/ready checks database, Redis, and Ollama connectivity
    - AC-5: Log output includes correlation IDs for request tracing
- Edge Cases:
    - Log sink unavailable → fallback to console sink with buffer
    - Health check dependency timeout → report degraded (not unhealthy)

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
| Logging | Serilog | 3.x |
| Monitoring | ASP.NET Core Health Checks | 8.0 |
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
Configure Serilog structured logging with PHI-scrubbing enrichers that strip Protected Health Information from all application logs, error messages, and stack traces per NFR-011. Implement health check endpoints (/health/live for liveness, /health/ready for readiness) that verify database, Redis, and Ollama AI runtime connectivity. Add correlation ID enrichment for distributed request tracing.

## Dependent Tasks
- task_001_be_modular_monolith_setup (US_002) — Requires ASP.NET Core project
- task_001_be_caching_realtime_setup (US_004) — Redis health check dependency
- task_001_ai_runtime_orchestration_setup (US_005) — Ollama health check dependency

## Impacted Components
- NEW: Serilog configuration with JSON sink and PHI-scrubbing enricher
- NEW: Health check implementations for DB, Redis, Ollama
- NEW: Correlation ID middleware
- MODIFY: Program.cs to register logging and health checks

## Implementation Plan
1. Install Serilog packages (Serilog.AspNetCore, Serilog.Sinks.Console, Serilog.Sinks.File)
2. Create PhiScrubbingEnricher that detects and redacts PHI patterns (SSN, DOB, email, phone, names)
3. Configure Serilog in Program.cs with structured JSON format, correlation ID enrichment, and PHI scrubber
4. Create liveness health check at /health/live (process running)
5. Create readiness health check at /health/ready (DB connectivity, Redis ping, Ollama status)
6. Configure health check timeouts (5s per dependency, degraded on timeout)
7. Add correlation ID middleware extracting from X-Correlation-ID header or generating new GUID
8. Verify PHI scrubbing strips sensitive data and health endpoints respond correctly

## Current Project State
```
[PLACEHOLDER - Updated after US_002, US_004, US_005 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/Shared/SharedKernel/Logging/PhiScrubbingEnricher.cs | PHI detection and redaction enricher |
| CREATE | backend/src/Shared/SharedKernel/Logging/CorrelationIdMiddleware.cs | Request correlation ID extraction/generation |
| CREATE | backend/src/Host/HealthChecks/DatabaseHealthCheck.cs | SQL Server connectivity check |
| CREATE | backend/src/Host/HealthChecks/RedisHealthCheck.cs | Upstash Redis ping check |
| CREATE | backend/src/Host/HealthChecks/OllamaHealthCheck.cs | Ollama AI runtime check |
| MODIFY | backend/src/Host/Program.cs | Register Serilog, health checks, correlation middleware |

## External References
- Serilog ASP.NET Core: https://github.com/serilog/serilog-aspnetcore
- ASP.NET Core Health Checks: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks

## Build Commands
- `dotnet build` — Build solution
- `dotnet run --project src/Host` — Start API and verify health endpoints

## Implementation Validation Strategy
- [x] Serilog produces structured JSON logs on console
- [x] PHI patterns (SSN, email, phone) are scrubbed from log output
- [x] GET /health/live returns 200
- [x] GET /health/ready returns status for DB, Redis, Ollama
- [x] Correlation ID appears in all log entries for a request

## Implementation Checklist
- [x] Install Serilog packages and configure structured JSON logging
- [x] Create PhiScrubbingEnricher for SSN, DOB, email, phone, name patterns
- [x] Configure Serilog pipeline with PHI scrubber and correlation ID enrichment
- [x] Implement /health/live liveness endpoint
- [x] Implement /health/ready with DB, Redis, Ollama dependency checks
- [x] Add correlation ID middleware (X-Correlation-ID header or auto-generated GUID)
- [x] Configure health check timeouts (5s per dependency, degraded on timeout)
- [x] Verify PHI scrubbing and health endpoints in development environment
