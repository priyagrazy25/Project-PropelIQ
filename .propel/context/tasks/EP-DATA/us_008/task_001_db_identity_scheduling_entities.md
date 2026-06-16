---
post_title: "TASK_001 - Identity & Scheduling Entity Schema"
author1: "AI Senior Developer"
post_slug: "task-001-db-identity-scheduling-entities"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-DATA, US_008, database, entities, EF Core, User, Appointment, Provider"
ai_note: "Generated with AI assistance from user story US_008"
summary: "Create EF Core entity configurations for User, Patient, Provider, Appointment, AppointmentSlot, PreferredSlotSwap, Waitlist, and IntakeRecord."
post_date: "2026-04-16"
---

# Task - TASK_001_DB_IDENTITY_SCHEDULING_ENTITIES

## Requirement Reference
- User Story: us_008
- Story Location: .propel/context/tasks/EP-DATA/us_008/us_008.md
- Acceptance Criteria:
    - AC-1: User entity with GUID PK, Email uniqueness, Role enum, Status enum per DR-001
    - AC-2: Appointment entity with status-driven lifecycle per DR-002
    - AC-3: Provider + AppointmentSlot with ProviderID+DateTime unique constraint per DR-003
    - AC-4: PreferredSlotSwap with FIFO priority queue and status tracking
    - AC-5: Waitlist, IntakeRecord entities with FK relationships
    - AC-6: EF Core migration generates schema without errors
- Edge Cases:
    - Double-booking attempt → database row-level locking rejects second transaction per DR-009
    - Soft-delete cascade → deactivating patient cascades status to related records per DR-008

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
| Database | SQL Server Express | 2022 |
| ORM | Entity Framework Core | 8.0 |

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
Create all identity and scheduling domain entities with EF Core Fluent API configurations. This includes User (with Patient extension), Provider, Appointment (status-driven lifecycle), AppointmentSlot (availability tracking), PreferredSlotSwap (FIFO queue), Waitlist, and IntakeRecord. Configure referential integrity with cascading soft-delete, unique constraints, and row-level locking for double-booking prevention.

## Dependent Tasks
- task_001_db_schema_orm_setup (US_003) — Requires DbContext and migration infrastructure

## Impacted Components
- NEW: Identity module entities (User, Patient)
- NEW: Scheduling module entities (Provider, Appointment, AppointmentSlot, PreferredSlotSwap, Waitlist, IntakeRecord)
- NEW: EF Core entity configurations (Fluent API)
- NEW: EF Core migration V002_IdentitySchedulingEntities

## Implementation Plan
1. Create User entity with GUID PK, Email, PasswordHash, FullName, DOB, ContactNumber, Address, Role, Status, timestamps
2. Create Patient entity extending User with insurance and intake data
3. Create Provider entity with Name, Specialty, Location, IsActive
4. Create AppointmentSlot entity with ProviderID FK, DateTime, Duration, Status (Available/Booked/Released)
5. Create Appointment entity with PatientID, ProviderID, DateTime, Status lifecycle, AppointmentType
6. Create PreferredSlotSwap, Waitlist, IntakeRecord entities with FK relationships
7. Configure Fluent API: unique constraints (User.Email, Slot ProviderID+DateTime), soft-delete cascades
8. Generate and apply EF Core migration V002

## Current Project State
```
[PLACEHOLDER - Updated after US_003 DB setup]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/Modules/Identity/Identity.Domain/Entities/User.cs | User entity with role and status |
| CREATE | backend/src/Modules/Identity/Identity.Domain/Entities/Patient.cs | Patient profile extending User |
| CREATE | backend/src/Modules/Scheduling/Scheduling.Domain/Entities/Provider.cs | Provider entity |
| CREATE | backend/src/Modules/Scheduling/Scheduling.Domain/Entities/Appointment.cs | Appointment with status lifecycle |
| CREATE | backend/src/Modules/Scheduling/Scheduling.Domain/Entities/AppointmentSlot.cs | Slot availability tracking |
| CREATE | backend/src/Modules/Scheduling/Scheduling.Domain/Entities/PreferredSlotSwap.cs | FIFO swap queue entity |
| CREATE | backend/src/Modules/Scheduling/Scheduling.Domain/Entities/Waitlist.cs | Waitlist entity |
| CREATE | backend/src/Modules/Scheduling/Scheduling.Domain/Entities/IntakeRecord.cs | Intake data entity |
| CREATE | backend/src/Modules/Identity/Identity.Infrastructure/Data/Configurations/ | EF Core Fluent API configs |
| CREATE | backend/src/Modules/Scheduling/Scheduling.Infrastructure/Data/Configurations/ | EF Core Fluent API configs |

## External References
- EF Core Entity Configuration: https://learn.microsoft.com/en-us/ef/core/modeling/
- EF Core Concurrency Tokens: https://learn.microsoft.com/en-us/ef/core/saving/concurrency

## Build Commands
- `dotnet ef migrations add V002_IdentitySchedulingEntities` — Generate migration
- `dotnet ef database update` — Apply migration

## Implementation Validation Strategy
- [x] Migration applies without errors
- [x] Unique constraint on User.Email rejects duplicates
- [x] Unique constraint on Slot.ProviderID+DateTime prevents double-booking
- [x] Soft-delete cascade propogates correctly

## Implementation Checklist
- [x] Create User entity (GUID PK, Email, PasswordHash, Role enum, Status enum, timestamps)
- [x] Create Patient entity with insurance info and One-to-One User relationship
- [x] Create Provider, AppointmentSlot entities with availability tracking
- [x] Create Appointment entity with status-driven lifecycle (Confirmed→Cancelled/Arrived→Completed)
- [x] Create PreferredSlotSwap (FIFO queue), Waitlist, IntakeRecord entities
- [x] Configure Fluent API: unique constraints, FK relationships, soft-delete cascades
- [x] Configure row-level locking strategy for slot reservation (DR-009)
- [x] Generate and apply EF Core migration V002_IdentitySchedulingEntities
