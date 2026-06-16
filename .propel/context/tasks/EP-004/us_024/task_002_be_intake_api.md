---
post_title: "TASK_002 - AI Intake API"
author1: "AI Senior Developer"
post_slug: "task-002-be-ai-intake-api"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-004, US_024, backend, ASP.NET Core, intake, session"
ai_note: "Generated with AI assistance from user story US_024"
summary: "Implement intake session API managing conversation state, storing parsed fields, and coordinating with the AI engine."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_AI_INTAKE_API

## Requirement Reference

- User Story: us_024
- Story Location: .propel/context/tasks/EP-004/us_024/us_024.md
- Acceptance Criteria:
  - AC-1: Guided flow collecting history, symptoms, allergies, medications
  - AC-2: Structured data with confidence scores within 5s p95
  - AC-5: Review all extracted fields with direct editing
- Edge Cases:
  - Token budget exhaustion → graceful summary with collected data

## Design References (Frontend Tasks Only)

| Reference Type | Value |
| -------------- | ----- |
| **UI Impact**  | No    |

## Applicable Technology Stack

| Layer    | Technology            | Version |
| -------- | --------------------- | ------- |
| Backend  | ASP.NET Core          | 8.0 LTS |
| ORM      | Entity Framework Core | 8.0     |
| Database | SQL Server Express    | 2022    |
| Caching  | Upstash Redis         | 7.x     |

## AI References (AI Tasks Only)

| Reference Type       | Value                                              |
| -------------------- | -------------------------------------------------- |
| **AI Impact**        | Yes (coordinates with AI engine)                   |
| **AIR Requirements** | AIR-003, AIR-Q02                                   |
| **AI Pattern**       | Conversational intake via Semantic Kernel + Ollama |
| **Model Provider**   | Ollama (Phi-3-mini 3.8B)                           |

## Mobile References (Mobile Tasks Only)

| Reference Type    | Value |
| ----------------- | ----- |
| **Mobile Impact** | No    |

## Task Overview

Build the intake session API: create session, send/receive messages relaying to AI engine, store conversation state in Redis, persist extracted fields to IntakeRecord entity, and support session completion with field-level editing.

## Dependent Tasks

- task_001_be_modular_monolith_setup (US_002) — Requires Clinical module
- task_001_db_identity_scheduling_entities (US_008) — Requires IntakeRecord entity
- task_001_ai_runtime_orchestration_setup (US_005) — Requires Semantic Kernel + Ollama

## Impacted Components

- NEW: IntakeController — Session and message endpoints
- NEW: IIntakeSessionService, IntakeSessionService
- NEW: IntakeSessionDto, IntakeMessageRequest, ParsedFieldDto
- NEW: Redis session state management for conversation

## Implementation Plan

1. Create POST /api/clinical/intake/sessions endpoint to initialize session
2. Create POST /api/clinical/intake/sessions/{id}/messages for patient messages
3. Relay patient message to AI engine and return parsed response with confidence scores
4. Store conversation state in Redis with 1-hour TTL
5. Create PUT /api/clinical/intake/sessions/{id}/fields for field-level edits
6. Create POST /api/clinical/intake/sessions/{id}/complete to finalize intake
7. Persist completed intake to IntakeRecord entity
8. Handle token budget exhaustion by returning summary with collected data

## Current Project State

```
[PLACEHOLDER - Updated after US_002, US_005, US_008 tasks]
```

## Expected Changes

| Action | File Path                                              | Description       |
| ------ | ------------------------------------------------------ | ----------------- |
| CREATE | src/Modules/Clinical/Controllers/IntakeController.cs   | Intake endpoints  |
| CREATE | src/Modules/Clinical/Services/IIntakeSessionService.cs | Service interface |
| CREATE | src/Modules/Clinical/Services/IntakeSessionService.cs  | Session logic     |
| CREATE | src/Modules/Clinical/DTOs/IntakeSessionDto.cs          | Session DTOs      |
| CREATE | src/Modules/Clinical/DTOs/IntakeMessageRequest.cs      | Message DTO       |
| CREATE | src/Modules/Clinical/DTOs/ParsedFieldDto.cs            | Parsed field DTO  |
| MODIFY | src/Modules/Clinical/DependencyInjection.cs            | Register services |

## External References

- Semantic Kernel chat: https://learn.microsoft.com/semantic-kernel/concepts/ai-services/chat-completion

## Build Commands

- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy

- [x] POST /sessions creates intake session
- [x] POST /messages returns AI-parsed response within 5s
- [x] Conversation state persists in Redis across requests
- [x] PUT /fields updates individual parsed fields
- [x] POST /complete persists IntakeRecord to database

## Implementation Checklist

- [x] Create intake session initialization endpoint
- [x] Implement message relay to AI engine with timeout handling
- [x] Store conversation state in Redis with 1-hour TTL
- [x] Return parsed fields with confidence scores in response
- [x] Implement field-level edit endpoint for review corrections
- [x] Create session completion endpoint persisting IntakeRecord
- [x] Handle token budget exhaustion with graceful summary
- [x] Return ProblemDetails for session not found, timeout, and errors
