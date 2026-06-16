---
post_title: "Unified Patient Access & Clinical Intelligence Platform - Epic Decomposition"
author1: "AI Product Manager"
post_slug: "unified-patient-access-epics"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Product Management, Epics"
tags: "epics, backlog, requirements-mapping, scheduling, clinical-intelligence, HIPAA, AI"
ai_note: "Generated with AI assistance from spec.md, design.md, models.md, and figma_spec.md source documents"
summary: "Comprehensive epic decomposition for the Unified Patient Access & Clinical Intelligence Platform with 13 epics covering project scaffolding, data layer, authentication, scheduling, clinical AI, medical coding, security, and UX accessibility across 143 mapped requirements."
post_date: "2026-04-16"
---

## Epic Summary Table

| Epic ID | Epic Title | Mapped Requirement IDs | Count |
|---------|-----------|------------------------|-------|
| EP-TECH | Project Scaffolding & Development Foundation | TR-001, TR-002, TR-003, TR-004, TR-005, TR-007, TR-010, TR-011, TR-012, TR-013, NFR-021, NFR-022, NFR-023 | 13 |
| EP-DATA | Data Layer & Entity Foundation | DR-001, DR-002, DR-003, DR-004, DR-005, DR-006, DR-007, DR-008, DR-009, DR-010, DR-014, DR-016, DR-017 | 13 |
| EP-001 | User Registration, Authentication & Access Control | FR-001, FR-002, FR-003, FR-004, TR-006, NFR-001, NFR-007, NFR-008, NFR-009, UXR-601, UXR-602, UXR-603 | 12 |
| EP-002 | Appointment Booking & Scheduling | FR-005, FR-006, FR-007, FR-008, FR-009, FR-010, NFR-002, NFR-004, NFR-015, NFR-019, UXR-001, UXR-002, UXR-101, UXR-102, UXR-201, UXR-301, UXR-302, UXR-501, UXR-502 | 19 |
| EP-003 | Staff Operations & Queue Management | FR-011, FR-012, FR-013, UXR-501, UXR-503, UXR-504 | 6 |
| EP-004 | Patient Intake & Insurance Verification | FR-014, FR-015, FR-016, FR-017, FR-018, AIR-003, AIR-008, AIR-Q02, NFR-013, UXR-103, UXR-104, UXR-105, UXR-505, UXR-605 | 14 |
| EP-005 | Notifications, Calendar Sync & PDF Confirmations | FR-019, FR-020, FR-021, FR-022, TR-014, TR-015, TR-016, NFR-014, NFR-020 | 9 |
| EP-006 | Clinical Document Upload & AI Extraction | FR-023, FR-024, TR-008, AIR-001, AIR-R01, AIR-R04, AIR-Q04, AIR-S02, AIR-O02, AIR-O04, NFR-003, NFR-016, UXR-106, UXR-604 | 14 |
| EP-007 | 360-Degree Patient View & Conflict Resolution | FR-025, FR-026, AIR-002, AIR-006, AIR-R02, AIR-R03, AIR-Q03, NFR-004, NFR-017, DR-012, UXR-107, UXR-108 | 12 |
| EP-008 | Medical Coding & AI Verification Workflow | FR-027, FR-028, AIR-004, AIR-005, AIR-Q01, AIR-S04, AIR-O01 | 7 |
| EP-009 | No-Show Risk Assessment & Reminder Escalation | FR-029, AIR-007, TR-009, AIR-O03, DR-013 | 5 |
| EP-010 | Security Hardening & HIPAA Compliance | FR-030, FR-031, FR-032, NFR-005, NFR-006, NFR-010, NFR-011, NFR-012, NFR-018, AIR-S01, AIR-S03, DR-011, DR-015 | 13 |
| EP-011 | Platform UX, Accessibility & Responsive Design | UXR-001, UXR-002, UXR-201, UXR-202, UXR-203, UXR-204, UXR-205, UXR-301, UXR-302, UXR-303, UXR-401, UXR-402, UXR-403 | 13 |

**Total Requirements Mapped:** 150
**Project Type:** Green-field (EP-TECH included)
**EP-DATA Trigger:** 17 DR-XXX requirements with extensive entity definitions in design.md

## Epic Description

### EP-TECH: Project Scaffolding & Development Foundation

**Business Value**: Enables all subsequent development by establishing the project foundation, technology stack, CI/CD pipeline, and development environment for the green-field platform.

**Description**: Bootstrap the complete development environment for the Unified Patient Access & Clinical Intelligence Platform. This epic establishes the React TypeScript frontend on Vercel, ASP.NET Core 8.0 LTS modular monolith backend, SQL Server Express 2022 database with Entity Framework Core, Upstash Redis caching layer, SignalR real-time communication, Ollama local AI runtime with Semantic Kernel orchestration, OpenAPI documentation, testing frameworks (xUnit, WebApplicationFactory, Playwright), structured logging with Serilog, and health check monitoring endpoints. The modular monolith architecture enforces clean domain boundaries (Scheduling, Clinical, Identity, Notification) with 80% unit and 60% integration test coverage targets.

**UI Impact**: No

**Screen References**: N/A

**Key Deliverables**:
- React 18.x TypeScript SPA project scaffolding deployed to Vercel free tier
- ASP.NET Core 8.0 LTS Web API with modular monolith structure (Scheduling, Clinical, Identity, Notification modules)
- SQL Server Express 2022 database with Entity Framework Core 8.0 ORM configuration
- Upstash Redis free tier integration for distributed caching
- SignalR WebSocket hub configuration for real-time client-server communication
- Ollama local LLM runtime with Phi-3-mini model and Semantic Kernel orchestration layer
- RESTful API design with OpenAPI 3.0 / Swagger documentation (Swashbuckle)
- Testing infrastructure: xUnit unit tests, WebApplicationFactory integration tests, Playwright E2E tests
- Serilog structured logging with PHI-scrubbing enrichers and configurable sinks
- Health check endpoints (/health/live, /health/ready) for uptime monitoring
- EF Core zero-downtime forward-only migration pipeline

**Dependent EPICs**:
- None

**Priority**: Critical

---

### EP-DATA: Data Layer & Entity Foundation

**Business Value**: Enables data operations for all feature epics requiring persistence by establishing the complete database schema, entity relationships, integrity constraints, and seed data.

**Description**: Create the full data layer for the platform including all core domain entities derived from the design.md entity definitions. This epic scaffolds User, Patient, Provider, Appointment, AppointmentSlot, PreferredSlotSwap, Waitlist, IntakeRecord, ClinicalDocument, ExtractedData, PatientView360, DataConflict, MedicalCode, AuditLog, and NoShowRiskScore entities. It enforces referential integrity with cascading soft-delete, unique constraints preventing double-booking, confidence score validation (0.0-1.0), EF Core Code-First migrations with sequential versioning, seed data for predefined dummy insurance records, and automated backup infrastructure with 24-hour RPO and 4-hour RTO targets.

**UI Impact**: No

**Screen References**: N/A

**Key Deliverables**:
- User accounts entity with email uniqueness, role assignment, and status tracking (DR-001)
- Appointment records entity with status-driven lifecycle (DR-002)
- Provider and AppointmentSlot entities with slot availability tracking (DR-003)
- ClinicalDocument entity with encrypted storage path and processing status (DR-004)
- ExtractedData entity with confidence scores and source page references (DR-005)
- PatientView360 aggregated record with JSON-structured clinical sections (DR-006)
- MedicalCode entity with ICD-10/CPT mappings and verification workflow (DR-007)
- Database-level referential integrity with cascading soft-delete (DR-008)
- Unique constraints on User.Email, Appointment.AppointmentID, Slot.ProviderID+DateTime with row-level locking (DR-009)
- Confidence score validation within 0.0-1.0 range with low-confidence flagging below 0.7 (DR-010)
- Automated database backups with RPO 24h / RTO 4h (DR-014)
- EF Core Code-First migrations with sequential versioning and rollback scripts (DR-016)
- Seed migration for predefined dummy insurance records (DR-017)

**Dependent EPICs**:
- EP-TECH - Foundational - Requires project scaffolding, SQL Server, and EF Core infrastructure

**Priority**: Critical

---

### EP-001: User Registration, Authentication & Access Control

**Business Value**: Establishes the identity and access management foundation that gates all user interactions with the platform, ensuring HIPAA-compliant authentication with role-based access control for Patient, Staff, and Admin personas.

**Description**: Implement the complete user identity lifecycle including patient self-registration with email validation and demographic capture, email/password authentication with JWT Bearer tokens and Argon2id password hashing, automatic 15-minute session timeout with token invalidation, and admin-driven user account management (create, update, deactivate, role assignment). The RBAC engine enforces deny-by-default policy ensuring patients access only their own data, staff access scoped patient data, and admins follow the minimum necessary standard for PHI. All synchronous API endpoints meet the 2-second p95 response time target. The frontend delivers inline field validation, global error banners with retry, and a pre-expiry session timeout modal with re-authentication.

**UI Impact**: Yes

**Screen References**: SCR-001, SCR-002, SCR-003, SCR-023, SCR-024, SCR-025

**Key Deliverables**:
- Patient registration form with email validation, demographics capture (name, DOB, phone, address)
- ASP.NET Identity integration with JWT Bearer token authentication and refresh token rotation
- Argon2id password hashing (3 iterations, 64 MB memory, 1 parallelism)
- 15-minute session auto-timeout invalidating both JWT access and refresh tokens
- Admin user management CRUD (create, update, deactivate accounts, assign roles)
- RBAC middleware with deny-by-default policy for Patient, Staff, Admin roles
- 2-second p95 API response time enforcement across all endpoints
- Inline field validation with descriptive error messages (UXR-601)
- Global error banner with retry action for API failures (UXR-602)
- Session timeout modal with 60-second countdown and re-authentication option (UXR-603)

**Dependent EPICs**:
- EP-TECH - Foundational - Requires ASP.NET Core backend, React frontend, and database infrastructure

**Priority**: Critical

---

### EP-002: Appointment Booking & Scheduling

**Business Value**: Delivers the core business capability of patient appointment booking with real-time slot management, reducing no-show rates through dynamic preferred slot swaps and waitlist automation, and recovering lost revenue from scheduling inefficiencies.

**Description**: Build the end-to-end appointment booking lifecycle for patients. This includes provider search by specialty, name, and date range with real-time slot availability display via SignalR WebSocket (500ms update latency), appointment selection and confirmation with unique appointment ID generation, the Dynamic Preferred Slot Swap feature (FIFO priority queue for unavailable slot requests with automatic swap execution), waitlist management with auto-notification, and cancel/reschedule operations that release slots back to the availability pool. The Patient Dashboard (SCR-006) serves as the central hub presenting personalized welcome, summary metrics (upcoming/past appointment counts, waitlist entries, total appointments), a tabbed Upcoming/Past appointment table with color-coded status badges, and a Quick Actions panel linking to provider search, waitlist status, and health profile — all reachable within 3 clicks (UXR-001). The system supports 500 concurrent users with idempotent booking and cancellation endpoints preventing duplicate appointments. The frontend provides optimistic UI booking with automatic rollback on failure.

**UI Impact**: Yes

**Screen References**: SCR-004, SCR-005, SCR-006, SCR-007, SCR-008

**Key Deliverables**:
- Provider search by specialty, name, and date range with real-time slot availability
- SignalR WebSocket integration for 500ms slot availability propagation to all clients
- Appointment booking with unique ID generation, patient/provider linking, and status tracking
- Dynamic Preferred Slot Swap with FIFO priority queue and automatic execution
- Waitlist management with auto-notification when preferred slots become available
- Cancel and reschedule operations with slot release and waitlist triggering
- Idempotent API endpoints for booking and cancellation preventing duplicate appointments
- Support for 500 concurrent authenticated users without performance degradation
- Optimistic UI for slot booking with automatic rollback on API failure (UXR-502)
- Provider search results rendered within 2 seconds (UXR-101)
- Real-time slot updates without page reload (UXR-102)
- Patient Dashboard hub (SCR-006) — persistent AppShell with fixed header (logo, notifications, avatar) and left sidebar navigation (Dashboard, Book Appointment, Intake, Documents, Health Profile, Waitlist), personalized welcome, 4 summary metric cards (Upcoming Appointments, Intake Status, Documents Uploaded, Waitlist Entries), tabbed Upcoming/Past appointments table with Reschedule action, Quick Actions panel (Complete Intake, Upload Documents, Health Profile), error/retry and empty states (US_050)
- AppShell layout route wrapping all patient pages with consistent header and sidebar (UXR-002, UXR-302)
- Dashboard appointment API (GET /api/scheduling/appointments/my) returning patient appointments with provider details ordered by date descending
- Dashboard renders within 2 seconds with skeleton loading states (UXR-201, UXR-301)
- Key appointment metrics visible on dashboard without navigation (UXR-002)
- 3-click max navigation from dashboard to any primary feature (UXR-001)
- Real-time status indicators on appointment badges (UXR-501)
- Responsive dashboard layout: 4-col summary grid (Desktop) → 2-col (Tablet) → 1-col (Mobile)

**Dependent EPICs**:
- EP-TECH - Foundational - Requires SignalR, Redis caching, and API infrastructure

**Priority**: High

---

### EP-003: Staff Operations & Queue Management

**Business Value**: Enables front desk and call center staff to manage walk-in patients, same-day queues, and patient arrivals efficiently, reducing administrative overhead and improving patient throughput for unscheduled visits.

**Description**: Implement staff-exclusive operations including walk-in appointment booking with optional new patient account creation, same-day queue management displaying ordered patients with status indicators (Waiting, In-Progress, Completed), and staff-only patient arrival marking for scheduled appointments. The system explicitly prevents patient self-check-in. Real-time queue status updates are pushed via WebSocket without manual refresh. Skeleton loading states preserve layout during data fetching, and toast notifications provide feedback for async operations like swap execution and upload completion.

**UI Impact**: Yes

**Screen References**: SCR-019, SCR-020, SCR-021

**Key Deliverables**:
- Walk-in booking restricted to Staff users with optional new patient account creation
- Same-day queue display with ordered patient list and status indicators (Waiting, In-Progress, Completed)
- Staff-only patient arrival marking (no self-check-in via app, web, or QR code)
- Skeleton loading screens preserving layout during data-fetch operations (UXR-501)
- Real-time queue status updates via WebSocket without manual refresh (UXR-503)
- Toast notifications for async operations with 5-second auto-dismiss (UXR-504)

**Dependent EPICs**:
- EP-TECH - Foundational - Requires SignalR, API infrastructure, and React frontend

**Priority**: Medium

---

### EP-004: Patient Intake & Insurance Verification

**Business Value**: Streamlines the pre-visit patient intake process through AI-assisted conversational flow and traditional form options, reducing data collection time while ensuring complete and accurate medical history capture with insurance validation.

**Description**: Build the dual-mode patient intake system supporting both AI conversational intake (via Semantic Kernel + Ollama with Phi-3-mini) and traditional manual form entry, with seamless switching between modes at any point without data loss. The AI conversational engine guides patients through medical history, symptoms, allergies, and medications collection, parsing natural language into structured fields with p95 latency under 5 seconds. The system falls back to manual form intake when the AI engine is unavailable or when confidence drops below 0.5 for three consecutive exchanges. Patients can directly edit any intake field without staff assistance. Insurance pre-check validates patient-provided insurance name and member ID against an internal predefined set of dummy records with inline pass/fail feedback. Form autosave runs at 30-second intervals to prevent data loss.

**UI Impact**: Yes

**Screen References**: SCR-010, SCR-011, SCR-012, SCR-013

**Key Deliverables**:
- AI conversational intake via Semantic Kernel + Ollama (Phi-3-mini) with structured output parsing
- Manual form intake with structured fields for history, symptoms, allergies, medications
- Seamless AI/manual mode switching preserving all previously entered data (UXR-103)
- Direct field-level editing in intake summary without restarting flow (UXR-104)
- AI fallback to manual form when engine unavailable or confidence below threshold (AIR-008)
- Graceful degradation with banner notification when AI services are down (NFR-013, UXR-605)
- p95 latency under 5 seconds for AI conversational responses (AIR-Q02)
- Insurance pre-check against predefined dummy records with inline validation (UXR-105)
- Form autosave at 30-second intervals preventing data loss (UXR-505)

**Dependent EPICs**:
- EP-TECH - Foundational - Requires Ollama, Semantic Kernel, React frontend, and API infrastructure

**Priority**: High

---

### EP-005: Notifications, Calendar Sync & PDF Confirmations

**Business Value**: Reduces no-show rates through automated multi-channel appointment reminders, enhances patient convenience with calendar synchronization, and provides professional appointment documentation through PDF confirmations.

**Description**: Implement the notification and calendar integration subsystem including automated appointment reminders via SMS (Twilio) and Email (SendGrid) at configurable intervals (72h, 24h, 2h before appointment), Google Calendar sync via Calendar API v3, Outlook Calendar sync via Microsoft Graph API v1.0 with OAuth 2.0 authorization, and PDF appointment confirmation generation (QuestPDF) delivered via email upon booking. Circuit breaker patterns protect against external service failures with configurable thresholds and recovery timeouts. All external service calls implement retry logic with exponential backoff (max 3 attempts).

**UI Impact**: Yes

**Screen References**: SCR-009

**Key Deliverables**:
- Automated SMS reminders via Twilio at 72h, 24h, 2h intervals before appointments
- Automated Email reminders via SendGrid at configurable intervals
- Google Calendar sync with OAuth 2.0 creating calendar events with appointment details
- Outlook Calendar sync via Microsoft Graph API v1.0 with OAuth 2.0
- PDF appointment confirmation (QuestPDF) with provider, date, time, location, prep instructions
- PDF delivery via email upon booking confirmation
- Circuit breaker patterns for Calendar APIs, SMS Gateway, Email Service (NFR-014)
- Retry logic with exponential backoff (max 3 attempts) for all external calls (NFR-020)
- Background reminder scheduler service

**Dependent EPICs**:
- EP-TECH - Foundational - Requires ASP.NET Core background services and API infrastructure

**Priority**: Medium

---

### EP-006: Clinical Document Upload & AI Extraction

**Business Value**: Eliminates the 20-minute manual clinical data extraction bottleneck by automating document parsing and structured data extraction from uploaded PDFs, enabling clinical staff to focus on patient-facing tasks.

**Description**: Build the clinical document processing pipeline supporting patient PDF upload (including scanned/image-based PDFs) with drag-and-drop UX, Tesseract OCR text extraction, scispaCy biomedical NER for structured clinical data identification (vitals, medical history, medications, allergies, lab results, diagnoses), confidence-scored output per data point, and document embedding storage in SQL Server vector tables. The pipeline chunks documents into 512-token segments with 10% overlap for RAG context preservation. PII is redacted before NLP processing. Document processing completes within 5 minutes per document (up to 20 pages) and scales to 10,000 documents per month. Sequential job queuing limits concurrent extraction to 2 pipelines to prevent resource exhaustion on free-tier infrastructure. Circuit breaker protects the Ollama inference service. Token budgets enforce 4,096 tokens per conversational request and 8,192 per document extraction request.

**UI Impact**: Yes

**Screen References**: SCR-014, SCR-015

**Key Deliverables**:
- PDF upload with drag-and-drop UX and per-file progress indicators (UXR-106)
- Tesseract OCR 5.x pipeline for scanned/image-based PDF text extraction
- scispaCy NER (en_ner_bc5cdr_md) for biomedical entity recognition
- Structured data extraction with confidence scores (0.0-1.0) per data point
- Document chunking at 512 tokens with 10% overlap (AIR-R01)
- Embedding storage in SQL Server custom vector table with indexed float arrays (AIR-R04)
- PII redaction pre-processing step before NLP models (AIR-S02)
- Circuit breaker for Ollama (3 failures / 60s window, 120s recovery) (AIR-O02)
- Sequential job queue with max 2 concurrent extraction pipelines (AIR-O04)
- Token budget enforcement (4,096 / 8,192 per request) (AIR-O01 via EP-008)
- 5-minute processing target per document up to 20 pages (NFR-003)
- 10,000 documents/month scalability (NFR-016)
- 90% clinical data extraction recall rate (AIR-Q04)
- Per-file retry on upload failure without re-uploading successful files (UXR-604)

**Dependent EPICs**:
- EP-DATA - Foundational - Requires ClinicalDocument, ExtractedData entity schemas and vector table

**Priority**: High

---

### EP-007: 360-Degree Patient View & Conflict Resolution

**Business Value**: Delivers the Trust-First clinical intelligence engine that consolidates unstructured clinical documents into a verified, unified patient profile, addressing the market gap of disconnected clinical data tools and enabling informed clinical decision-making.

**Description**: Build the 360-Degree Patient View aggregation engine and data conflict resolution workflow. The system aggregates extracted clinical data across all uploaded documents for a patient, applying semantic de-duplication to merge equivalent entries (brand vs. generic medication names). A RAG pipeline retrieves the top-5 relevant chunks with cosine similarity >= 0.75 and applies hybrid re-ranking (semantic similarity + 1.2x recency weighting). The system detects and highlights critical data conflicts (contradicting medications, inconsistent allergies) with source document references and severity classification. Clinical staff resolve conflicts through a side-by-side comparison interface. The aggregated view renders within 3 seconds leveraging Upstash Redis caching (15-minute TTL). Structured output schema validity is maintained at 99% or higher. Clinical data is retained for the patient account lifecycle plus 7 years post-deactivation.

**UI Impact**: Yes

**Screen References**: SCR-016, SCR-017

**Key Deliverables**:
- Unified 360-Degree Patient View with tabbed sections (Vitals, History, Medications, Allergies, Labs, Diagnoses)
- Semantic de-duplication merging equivalent entries across brand/generic references
- RAG retrieval of top-5 chunks with cosine similarity >= 0.75 (AIR-R02)
- Hybrid re-ranking combining semantic similarity with source document recency weighting 1.2x (AIR-R03)
- Cross-document data conflict detection with severity classification (Critical/Warning) (AIR-006)
- Staff conflict resolution interface with side-by-side comparison and source document links (UXR-108)
- Color-coded confidence score indicators (green >= 0.7, amber 0.5-0.7, red < 0.5) (UXR-107)
- 99% structured output schema validity validated against predefined JSON schemas (AIR-Q03)
- 3-second patient dashboard and 360-view render time leveraging cached aggregations (NFR-004)
- Upstash Redis caching for aggregated views with 15-minute TTL (NFR-017)
- Clinical data retention for account lifecycle + 7 years after deactivation (DR-012)

**Dependent EPICs**:
- EP-DATA - Foundational - Requires PatientView360, DataConflict, ExtractedData entity schemas

**Priority**: High

---

### EP-008: Medical Coding & AI Verification Workflow

**Business Value**: Automates ICD-10 and CPT code mapping from clinical data with >98% AI-Human agreement, reducing manual coding effort while maintaining clinical accuracy through a mandatory staff verification workflow.

**Description**: Implement AI-driven medical code mapping from the 360-Degree Patient View. The system maps ICD-10-CM codes from extracted diagnoses and CPT codes from procedures/encounters using a combination of NER-based classification, lookup table matching, and Semantic Kernel tool calling. Each code suggestion presents the top-3 candidate codes ranked by confidence score. All AI-generated codes enter with "Suggested" status requiring explicit staff verification (accept, reject with reason, or manual code entry). The AI-Human agreement rate is tracked over a rolling 30-day window targeting >98% acceptance. Token budgets of 4,096 tokens per request govern AI resource consumption.

**UI Impact**: Yes

**Screen References**: SCR-018

**Key Deliverables**:
- ICD-10-CM code mapping from diagnoses with top-3 candidates ranked by confidence (AIR-004)
- CPT code mapping from procedures/encounters with top-3 candidates ranked by confidence (AIR-005)
- Staff verification workflow: accept, reject (with reason), or manual code entry
- All AI suggestions enter as "Suggested" status requiring explicit staff verification (AIR-S04)
- AI-Human agreement rate tracking (>98% target) over 30-day rolling window (AIR-Q01)
- Token budget of 4,096 tokens per AI code mapping request (AIR-O01)
- Inline status updates without page reload
- Confidence score color-coding (green/amber/red) per suggested code

**Dependent EPICs**:
- EP-DATA - Foundational - Requires MedicalCode entity schema and 360-view data

**Priority**: Medium

---

### EP-009: No-Show Risk Assessment & Reminder Escalation

**Business Value**: Reduces appointment no-show rates from the 15% baseline through ML-driven risk prediction that triggers automated reminder escalation for high-risk appointments, directly recovering lost revenue and improving schedule utilization.

**Description**: Train and deploy an ML.NET classification model that calculates a no-show risk score (0-100) for each booked appointment based on patient appointment history, appointment type, time slot, and historical no-show patterns. High-risk appointments trigger automated reminder escalation at closer intervals. The model supports version rollback within 15 minutes by maintaining the previous model version alongside the active version. Appointment history is retained indefinitely to support continuous model retraining and operational analytics. The initial model trains on synthetic appointment data until sufficient real patient history is accumulated.

**UI Impact**: Yes

**Screen References**: SCR-022

**Key Deliverables**:
- ML.NET classification model for no-show risk scoring (0-100 per appointment)
- Risk score calculation based on patient history, appointment type, and time slot
- Automated reminder escalation for high-risk appointments
- Risk level presentation to staff on the No-Show Risk Dashboard
- ML.NET model training and deployment pipeline (TR-009)
- AI model version rollback within 15 minutes (AIR-O03)
- Appointment history retention indefinitely for analytics and model retraining (DR-013)

**Dependent EPICs**:
- EP-DATA - Foundational - Requires Appointment, NoShowRiskScore entity schemas and historical data

**Priority**: Medium

---

### EP-010: Security Hardening & HIPAA Compliance

**Business Value**: Ensures 100% HIPAA-compliant data handling across the platform, protecting patient PHI through encryption, audit logging, and access controls while achieving 99.9% uptime reliability targets required for healthcare operations.

**Description**: Implement comprehensive security controls spanning encryption, audit logging, OWASP compliance, and data retention policies. PHI is encrypted at rest using AES-256 (SQL Server TDE plus column-level encryption) and in transit using TLS 1.2+. Immutable audit logs capture all PHI-touching actions with actor, action, resource, timestamp, and before/after state snapshots in append-only tables with no UPDATE/DELETE permissions. PHI is redacted from application logs, error messages, and stack traces. OWASP Top 10 controls include parameterized queries, CSP headers, HSTS, rate limiting on auth endpoints, and input validation at all API boundaries. No PHI is transmitted to external AI providers. All AI model invocations are logged in the encrypted audit store. Audit logs are retained for 7 years. Database backups are stored in AES-256 encrypted format in a separate storage location. The platform targets 99.9% monthly uptime (max 43.8 min unplanned downtime) and 720-hour MTBF for core scheduling.

**UI Impact**: Yes

**Screen References**: SCR-025

**Key Deliverables**:
- AES-256 encryption at rest (TDE + column-level) for all PHI (FR-030, NFR-005)
- TLS 1.2+ encryption for all client-server and server-external-service connections (NFR-006)
- Immutable append-only audit logs with no UPDATE/DELETE permissions (FR-031, DR-011)
- RBAC enforcement with minimum necessary standard for PHI access (FR-032)
- PHI redaction from application logs, errors, and stack traces (NFR-011)
- OWASP Top 10 controls: parameterized queries, CSP, HSTS, rate limiting, input validation (NFR-010)
- Zero PHI transmission to external AI providers (AIR-S01)
- AI model invocation logging (input/output tokens, model version, confidence, duration) (AIR-S03)
- 7-year audit log retention in compliance with HIPAA (DR-011)
- Encrypted database backup storage (AES-256) in separate location (DR-015)
- 99.9% uptime monitoring and alerting (NFR-012)
- 720-hour MTBF target for core scheduling operations (NFR-018)

**Dependent EPICs**:
- EP-TECH - Foundational - Requires logging infrastructure, API middleware, and health check endpoints

**Priority**: High

---

### EP-011: Platform UX, Accessibility & Responsive Design

**Business Value**: Ensures the platform meets healthcare accessibility standards and provides a consistent, professional user experience across all devices, building patient trust through WCAG 2.2 AA compliance and responsive design from mobile through desktop.

**Description**: Implement cross-cutting UX, accessibility, and responsive design standards applied to all screens and user flows. The platform provides navigation to any primary feature in a maximum of 3 clicks from the dashboard with consistent header and sidebar layout across all authenticated screens. WCAG 2.2 AA compliance includes color contrast ratios (>= 4.5:1 text, >= 3:1 UI components), keyboard navigation with visible focus indicators, ARIA labels on all form controls, touch targets >= 44x44px on mobile, and error states communicated through icon + text (not color alone). The responsive design adapts across three breakpoints: Mobile (390px), Tablet (768px), Desktop (1440px) with adaptive navigation (desktop fixed sidebar, tablet collapsible sidebar, mobile bottom nav) and table-to-card layout degradation on mobile. The visual design system uses a healthcare-appropriate calming color palette, 8px base grid spacing, and clear typography hierarchy.

**UI Impact**: Yes

**Screen References**: All screens (SCR-001 through SCR-025)

**Key Deliverables**:
- Max 3-click navigation to any primary feature from dashboard (UXR-001)
- Consistent header and sidebar navigation layout across all authenticated screens (UXR-002)
- WCAG 2.2 AA color contrast compliance (>= 4.5:1 text, >= 3:1 UI) (UXR-201)
- Full keyboard navigation with visible focus indicators (2px offset, >= 3:1 contrast) (UXR-202)
- ARIA labels on all form controls, buttons, and dynamic content regions (UXR-203)
- Touch targets >= 44x44px on mobile viewports (UXR-204)
- Error states using icon + text indicators, not color alone (UXR-205)
- Responsive layout at 390px (Mobile), 768px (Tablet), 1440px (Desktop) breakpoints (UXR-301)
- Adaptive navigation: fixed sidebar (Desktop), collapsible sidebar (Tablet), bottom nav (Mobile) (UXR-302)
- Table-to-stacked-card layout transformation on mobile viewports (UXR-303)
- Healthcare-appropriate color palette with calming primary blue and semantic alert colors (UXR-401)
- 8px base grid spacing system for all layout spacings and paddings (UXR-402)
- Clear typography hierarchy from H1 through Caption (UXR-403)

**Dependent EPICs**:
- EP-TECH - Foundational - Requires React frontend scaffolding and design system setup
