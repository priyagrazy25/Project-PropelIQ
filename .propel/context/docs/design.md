---
post_title: "Unified Patient Access & Clinical Intelligence Platform - Architecture Design"
author1: "AI Solution Architect"
post_slug: "unified-patient-access-design"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Architecture, Design"
tags: "design, architecture, NFR, TR, DR, AIR, HIPAA, clinical-intelligence, modular-monolith"
ai_note: "Generated with AI assistance from spec.md source document"
summary: "Comprehensive architecture design specification covering non-functional, technical, data, and AI requirements for a unified healthcare platform combining patient scheduling with clinical data intelligence."
post_date: "2026-04-15"
---

## Project Overview

The Unified Patient Access & Clinical Intelligence Platform is a standalone healthcare application that bridges patient scheduling and clinical data management into a single, modern, patient-centric system. It serves three user roles (Patient, Staff, Admin) and combines deterministic appointment booking workflows with a Trust-First AI-powered clinical intelligence engine that consolidates unstructured clinical documents into a verified 360-Degree Patient View with ICD-10 and CPT code mappings. The platform targets free/open-source infrastructure and enforces HIPAA-compliant data handling throughout.

## Architecture Goals

- AG-001: Deliver a modular, maintainable architecture that cleanly separates scheduling, clinical intelligence, identity, and notification domains with well-defined interfaces
- AG-002: Enforce HIPAA Technical Safeguards at every layer (transport, storage, application, logging) with zero PHI exposure in logs or AI pipelines
- AG-003: Achieve production-grade reliability (99.9% uptime target) within free-tier infrastructure constraints through aggressive caching, circuit breakers, and graceful degradation
- AG-004: Implement a Trust-First AI architecture where every AI-generated output carries a confidence score and requires human verification before clinical use
- AG-005: Enable future evolution from modular monolith to microservices without rewriting domain logic or data access layers
- AG-006: Minimize no-show rates through real-time scheduling, smart reminders, and AI-driven risk assessment integrated into the booking lifecycle

## Non-Functional Requirements

### Performance

- NFR-001: System MUST respond to all synchronous API requests within 2 seconds at the 95th percentile under normal load (up to 500 concurrent users)
- NFR-002: System MUST propagate appointment slot availability updates to all connected clients within 500 milliseconds via WebSocket (SignalR)
- NFR-003: System MUST complete clinical document OCR and NLP extraction pipeline within 5 minutes per document (up to 20 pages)
- NFR-004: System MUST render the patient dashboard and 360-Degree Patient View within 3 seconds, leveraging cached aggregations

### Security

- NFR-005: System MUST encrypt all Protected Health Information (PHI) at rest using AES-256 encryption (SQL Server Transparent Data Encryption plus column-level encryption for sensitive fields)
- NFR-006: System MUST encrypt all data in transit using TLS 1.2 or higher for every client-server and server-external-service connection
- NFR-007: System MUST hash all user passwords using Argon2id with a minimum work factor of 3 iterations, 64 MB memory, and 1 degree of parallelism
- NFR-008: System MUST enforce automatic session termination after 15 minutes of user inactivity, invalidating both JWT access token and refresh token
- NFR-009: System MUST enforce role-based access control (RBAC) with deny-by-default policy, ensuring patients access only their own data, staff access scoped patient data, and admins follow the minimum necessary standard for PHI
- NFR-010: System MUST comply with OWASP Top 10 security controls including parameterized queries, Content Security Policy headers, HTTP Strict Transport Security, rate limiting on authentication endpoints, and input validation at all API boundaries
- NFR-011: System MUST redact all PHI from application logs, error messages, and stack traces; audit logs containing PHI MUST be stored in the encrypted audit store only

### Availability

- NFR-012: System MUST achieve 99.9% uptime measured monthly, allowing a maximum of 43.8 minutes of unplanned downtime per month
- NFR-013: System MUST implement graceful degradation when AI processing services are unavailable, falling back to manual intake forms and disabling AI-dependent features without affecting core scheduling functionality
- NFR-014: System MUST implement circuit breaker patterns for all external service integrations (Calendar APIs, SMS Gateway, Email Service) with configurable failure thresholds and recovery timeouts

### Scalability

- NFR-015: System MUST support 500 concurrent authenticated users performing booking, intake, and clinical data operations without performance degradation
- NFR-016: System MUST support storage and processing of 10,000 clinical documents per month with linear scaling of extraction pipeline throughput
- NFR-017: System MUST use Upstash Redis for caching frequently accessed data (provider availability, patient profiles, 360-degree views) with configurable TTL to reduce database load by at least 60%

### Reliability

- NFR-018: System MUST maintain a mean time between failures (MTBF) of at least 720 hours for core scheduling operations
- NFR-019: System MUST implement idempotent API endpoints for all booking and cancellation operations to prevent duplicate appointments from retry scenarios
- NFR-020: System MUST implement retry logic with exponential backoff for all external service calls (Calendar API, SMS, Email) with a maximum of 3 retry attempts

### Maintainability

- NFR-021: System MUST follow modular monolith architecture with domain-driven module boundaries (Scheduling, Clinical, Identity, Notification) communicating through well-defined interfaces
- NFR-022: System MUST maintain minimum 80% unit test coverage for business logic layers and 60% integration test coverage for API endpoints
- NFR-023: System MUST use Entity Framework Core migrations for all database schema changes, supporting zero-downtime forward-only migrations

## Data Requirements

### Data Structures

- DR-001: System MUST store user accounts with email as unique identifier, capturing: UserID (GUID), Email, PasswordHash, FullName, DateOfBirth, ContactNumber, Address, Role (Patient/Staff/Admin), Status (Active/Deactivated), CreatedAt, UpdatedAt
- DR-002: System MUST store appointment records with: AppointmentID (GUID), PatientID (FK), ProviderID (FK), DateTime, Duration, Status (Confirmed/Cancelled/Rescheduled/Walk-In/Arrived/In-Progress/Completed/No-Show), AppointmentType, CreatedAt, UpdatedAt
- DR-003: System MUST store provider records with: ProviderID (GUID), Name, Specialty, Location, IsActive; and appointment slots with: SlotID (GUID), ProviderID (FK), DateTime, Duration, Status (Available/Booked/Released)
- DR-004: System MUST store clinical documents with: DocumentID (GUID), PatientID (FK), FileName, FileType, EncryptedFilePath, FileSize, UploadedAt, ProcessingStatus (Queued/Processing/Completed/Failed), ProcessingError
- DR-005: System MUST store extracted clinical data with: ExtractedDataID (GUID), DocumentID (FK), PatientID (FK), DataCategory (Vital/History/Medication/Allergy/LabResult/Diagnosis), DataKey, DataValue, Unit, ConfidenceScore (0.0-1.0), SourcePageReference
- DR-006: System MUST store the 360-Degree Patient View as an aggregated record with: ViewID (GUID), PatientID (FK), Vitals (JSON), MedicalHistory (JSON), Medications (JSON), Allergies (JSON), LabResults (JSON), Diagnoses (JSON), GeneratedAt, LastUpdatedAt
- DR-007: System MUST store medical code mappings with: CodeID (GUID), PatientID (FK), CodeType (ICD10/CPT), Code, Description, ConfidenceScore, Status (Suggested/Verified/Rejected), SuggestedBy (System), VerifiedBy (FK to UserID), VerifiedAt, RejectionReason

### Data Integrity

- DR-008: System MUST enforce referential integrity across all foreign key relationships using database-level constraints with cascading soft-delete (status flag) rather than hard-delete for patient-related records
- DR-009: System MUST enforce unique constraints on: User.Email, Appointment.AppointmentID, Slot.ProviderID+DateTime combination, and prevent double-booking through database-level row locking during slot reservation
- DR-010: System MUST validate all clinical data extraction confidence scores within the range 0.0 to 1.0 and flag data points below 0.7 confidence as "Low Confidence" in the 360-Degree Patient View

### Data Retention

- DR-011: System MUST retain all audit log records for a minimum of 7 years in compliance with HIPAA record retention requirements, stored in append-only tables with no UPDATE or DELETE permissions granted to any application role
- DR-012: System MUST retain patient clinical documents and extracted data for the duration of the patient account lifecycle plus 7 years after account deactivation
- DR-013: System MUST retain appointment history records indefinitely for no-show risk modeling and operational analytics

### Data Backup

- DR-014: System MUST perform automated database backups with a Recovery Point Objective (RPO) of 24 hours and a Recovery Time Objective (RTO) of 4 hours
- DR-015: System MUST store database backups in encrypted format (AES-256) in a separate storage location from the primary database

### Data Migration

- DR-016: System MUST use Entity Framework Core Code-First migrations with sequential versioning for all schema changes, supporting forward-only migration with rollback scripts generated for each migration
- DR-017: System MUST support seeding of predefined dummy insurance records (InsuranceName, ValidMemberIDPattern) through migration scripts for the insurance pre-check feature

### Domain Entities

- **User**: Core identity entity representing all platform users. Contains demographics, authentication data, and role assignment. One-to-one relationship with Patient profile (when role is Patient). Referenced by AuditLog for actor tracking.
- **Patient**: Extension of User for patient-specific data including insurance information and intake status. One-to-many relationship with Appointments, ClinicalDocuments, and IntakeRecords.
- **Provider**: Healthcare provider entity with specialty and location. One-to-many relationship with AppointmentSlots. No login capability in Phase 1.
- **Appointment**: Central scheduling entity linking Patient to Provider at a specific DateTime. Status-driven lifecycle. Related to PreferredSlotSwap, Reminders, CalendarSync, IntakeRecord, and NoShowRiskScore.
- **AppointmentSlot**: Granular time-slot entity owned by Provider. Status transitions: Available → Booked → Released. Used for real-time availability display and conflict-free booking.
- **PreferredSlotSwap**: Request entity linking an Appointment to a preferred unavailable slot. FIFO priority queue. Status: Pending → Executed/Expired.
- **Waitlist**: Patient queue entity for fully booked providers. Tracks preferred date ranges. Auto-notifies when slots become available.
- **IntakeRecord**: Patient intake data entity supporting both AI conversational and manual form modes. Stores structured medical history, symptoms, allergies, and medications.
- **ClinicalDocument**: Uploaded PDF entity with encrypted storage path and processing pipeline status tracking. Triggers extraction pipeline on upload.
- **ExtractedData**: Granular clinical data points extracted by AI from ClinicalDocuments. Each entry has a confidence score and source page reference. Feeds into 360-Degree Patient View.
- **PatientView360**: Aggregated, de-duplicated clinical profile consolidating all ExtractedData for a patient. JSON-structured sections for vitals, history, medications, allergies, labs, and diagnoses.
- **DataConflict**: Flagged contradictions across multiple ClinicalDocuments. Tracks severity (Critical/Warning), resolution status, and staff resolution notes with audit trail.
- **MedicalCode**: AI-suggested ICD-10 and CPT codes with confidence scores. Staff verification workflow with accept/reject tracking and accuracy metrics.
- **AuditLog**: Immutable, append-only event record capturing all PHI-touching actions. Stores actor, action, resource, timestamp, and before/after state snapshots.
- **NoShowRiskScore**: AI-calculated risk assessment for each appointment based on patient history, time slot, and appointment type. Drives reminder escalation rules.

## AI Consideration

**Status:** Applicable

The upstream spec.md contains 6 `[AI-CANDIDATE]` tagged requirements (FR-014, FR-023, FR-024, FR-025, FR-027, FR-028) and 2 `[HYBRID]` tagged requirements (FR-026, FR-029). The platform requires AI capabilities for clinical document processing, conversational intake, medical coding, conflict detection, and risk assessment.

### GenAI Fit Assessment

| Feature                             | AI Fit Score (1-5) | Classification | Rationale                                                                                                   |
| ----------------------------------- | ------------------ | -------------- | ----------------------------------------------------------------------------------------------------------- |
| AI Conversational Intake (FR-014)   | 5                  | HIGH-FIT       | Natural language understanding, conversational flow, structured extraction from free-text patient responses |
| Clinical Document Parsing (FR-023)  | 5                  | HIGH-FIT       | OCR for scanned PDFs, NLP pipeline for unstructured clinical text extraction                                |
| Structured Data Extraction (FR-024) | 5                  | HIGH-FIT       | Named Entity Recognition from clinical narratives, confidence-scored entity extraction                      |
| 360-Degree Patient View (FR-025)    | 4                  | HIGH-FIT       | Multi-document data aggregation, de-duplication, semantic matching across unstructured sources              |
| Data Conflict Detection (FR-026)    | 3                  | HYBRID         | AI surfaces contradictions across documents, clinical staff makes final resolution decision                 |
| ICD-10 Code Mapping (FR-027)        | 5                  | HIGH-FIT       | Classification from unstructured narrative to standardized medical codes with confidence scoring            |
| CPT Code Mapping (FR-028)           | 5                  | HIGH-FIT       | Procedure classification from clinical encounter descriptions to billing codes                              |
| No-Show Risk Assessment (FR-029)    | 3                  | HYBRID         | ML model predicts risk from historical patterns, deterministic rules trigger escalation actions             |

**Decision:** Multiple features score 4-5 → Full AI Requirements section generated with Hybrid architecture pattern (RAG + Tool Calling).

## AI Requirements

### AI Functional Requirements

- AIR-001: System MUST extract structured clinical data (vitals, medical history, medications, allergies, lab results, diagnoses) from uploaded PDF documents using an NLP pipeline combining OCR (Tesseract) and biomedical Named Entity Recognition (scispaCy), outputting each data point with a confidence score between 0.0 and 1.0
- AIR-002: System MUST generate a unified 360-Degree Patient View by aggregating extracted data across all uploaded documents for a patient, applying semantic de-duplication to merge equivalent entries (e.g., matching medication names across brand and generic references)
- AIR-003: System MUST provide an AI conversational intake flow using a local LLM (via Semantic Kernel + Ollama) that guides patients through medical history, symptoms, allergies, and medications collection, parsing natural language responses into structured intake data fields
- AIR-004: System MUST map ICD-10-CM codes from diagnoses identified in the 360-Degree Patient View using a combination of NER-based classification and lookup table matching, presenting top-3 candidate codes ranked by confidence score
- AIR-005: System MUST map CPT codes from procedures and encounters identified in aggregated patient data using classification models and reference table lookup, presenting top-3 candidate codes ranked by confidence score
- AIR-006: System MUST detect data conflicts across multiple clinical documents by comparing extracted data points of the same category, flagging contradictions (e.g., conflicting medication dosages, inconsistent allergy records) with source document references and severity classification
- AIR-007: System MUST calculate a no-show risk score (0-100) for each booked appointment using a trained ML.NET classification model analyzing patient appointment history, appointment type, time slot, and historical no-show patterns
- AIR-008: System MUST fall back to manual form intake (FR-015) when the AI conversational engine is unavailable or when confidence in parsed responses drops below 0.5 for three consecutive exchanges

### AI Quality Requirements

- AIR-Q01: System MUST maintain an AI-Human agreement rate above 98% for suggested ICD-10 and CPT codes, measured as the percentage of AI-suggested codes accepted by clinical staff without modification over a rolling 30-day window
- AIR-Q02: System MUST achieve p95 latency of 5 seconds or less for AI conversational intake responses to maintain natural dialogue flow
- AIR-Q03: System MUST enforce structured output schema validity of 99% or higher for all AI extraction outputs, validating against predefined JSON schemas for each data category (vitals, medications, allergies, diagnoses)
- AIR-Q04: System MUST achieve a clinical data extraction recall rate of 90% or higher, verified through periodic evaluation against manually annotated test document sets

### AI Safety Requirements

- AIR-S01: System MUST NOT transmit any PHI to external AI model providers; all AI inference MUST execute locally using Ollama-hosted models to maintain HIPAA compliance
- AIR-S02: System MUST redact patient-identifying information (name, SSN, DOB, contact details) from text before passing content to NLP extraction models, using a pre-processing PII detection step
- AIR-S03: System MUST log all AI model invocations including input token count, output token count, model version, confidence scores, and processing duration in the encrypted audit store with a retention period of 7 years
- AIR-S04: System MUST restrict AI-generated clinical data and medical codes to a "Suggested" status requiring explicit staff verification before inclusion in the patient record

### AI Operational Requirements

- AIR-O01: System MUST enforce a token budget of 4,096 tokens per AI request for conversational intake and 8,192 tokens per request for clinical document extraction to manage local compute resources
- AIR-O02: System MUST implement a circuit breaker for the local AI inference service (Ollama) that opens after 3 consecutive failures within a 60-second window, with automatic recovery attempt after 120 seconds
- AIR-O03: System MUST support AI model version rollback within 15 minutes by maintaining the previous model version alongside the active version in the Ollama model registry
- AIR-O04: System MUST queue clinical document processing jobs and process them sequentially with a maximum of 2 concurrent extraction pipelines to prevent resource exhaustion on free-tier infrastructure

### RAG Pipeline Requirements

- AIR-R01: System MUST chunk clinical document text into segments of 512 tokens with 10% overlap (51 tokens) to preserve context across chunk boundaries during extraction
- AIR-R02: System MUST retrieve top-5 relevant chunks with cosine similarity score of 0.75 or higher when generating the 360-Degree Patient View from multi-document sources
- AIR-R03: System MUST re-rank retrieved chunks using a hybrid strategy combining semantic similarity score with source document recency weighting (newer documents weighted 1.2x)
- AIR-R04: System MUST store document embeddings in SQL Server using a dedicated vector table with indexed float arrays, supporting cosine similarity search via custom SQL functions

### AI Architecture Pattern

**Selected Pattern:** Hybrid (RAG + Tool Calling)

**Rationale:** The platform requires multiple AI patterns working together:

- **RAG** is selected for clinical document parsing (AIR-001), 360-Degree Patient View generation (AIR-002), and conflict detection (AIR-006) because these features require grounding AI responses in specific uploaded patient documents with citation to source material. RAG ensures the AI extraction is traceable to original document content.
- **Tool Calling** is selected for ICD-10/CPT code mapping (AIR-004, AIR-005) and no-show risk assessment (AIR-007) because these features require structured lookups against reference code tables and trained classification models rather than free-form generation.
- **Conversational AI** (a specialized application of the local LLM) is used for patient intake (AIR-003) with structured output parsing.

The Hybrid pattern is justified because no single pattern covers the full spectrum of AI capabilities required: document grounding (RAG), structured classification (Tool Calling), and natural language dialogue (Conversational).

**Pattern Selection Matrix:**

| Pattern      | Select When                                       | Platform Applicability                                         |
| ------------ | ------------------------------------------------- | -------------------------------------------------------------- |
| RAG          | Q&A over docs, dynamic knowledge, citation needed | Clinical doc parsing, 360-view, conflict detection             |
| Tool Calling | API actions, data fetch, system integration       | ICD-10/CPT lookup, risk score calculation                      |
| Fine-tuning  | Consistent terminology, stable domain knowledge   | Not selected: free-tier compute insufficient for fine-tuning   |
| Hybrid       | Complex workflows requiring RAG + actions         | Selected: Platform requires both RAG and Tool Calling patterns |

## Architecture and Design Decisions

- **AD-001 Modular Monolith Architecture**: Selected over microservices to minimize deployment complexity on free-tier infrastructure while maintaining clean domain boundaries. The system is organized into four modules (Scheduling, Clinical, Identity, Notification) communicating through in-process interfaces. This decision is driven by NFR-021 (maintainability) and the free-hosting constraint (C-1) that eliminates the need for container orchestration.

- **AD-002 Clean Architecture (Onion) per Module**: Each module follows Clean Architecture with Domain, Application, Infrastructure, and Presentation layers. Domain entities and business rules have zero external dependencies. This ensures testability (NFR-022) and future migration path (AG-005).

- **AD-003 CQRS-Lite for Scheduling Module**: The Scheduling module separates read models (cached availability views) from write models (booking transactions) to optimize for the high-read, moderate-write pattern of appointment scheduling. Read models are served from Upstash Redis cache (NFR-017). Full event sourcing is not implemented in Phase 1 to avoid complexity.

- **AD-004 Local AI Inference Only**: All AI/ML processing runs locally via Ollama to ensure zero PHI transmission to external services (AIR-S01). This eliminates cloud AI API costs (C-1) and HIPAA data transmission concerns. The trade-off is reduced model capability compared to cloud-hosted LLMs, mitigated by using specialized biomedical models (scispaCy, ClinicalBERT).

- **AD-005 Asynchronous Document Processing Pipeline**: Clinical document extraction is implemented as a background job queue (using .NET BackgroundService) rather than synchronous processing. This prevents API timeout on large documents (NFR-003) and allows controlled resource usage (AIR-O04) on free-tier infrastructure.

- **AD-006 JWT with Refresh Token Rotation**: Authentication uses short-lived JWT access tokens (15-minute expiry matching NFR-008) with single-use refresh tokens. Refresh token rotation prevents token replay attacks. This approach supports the stateless API pattern while enforcing the session timeout requirement.

- **AD-007 Event-Driven Audit Logging**: All PHI-touching operations publish domain events that are consumed by an append-only audit logger. The audit log table has no UPDATE/DELETE permissions granted to any application role (DR-011). This decouples audit concerns from business logic while ensuring completeness.

- **AD-008 SignalR for Real-Time Updates**: WebSocket connections via SignalR provide real-time slot availability updates (NFR-002) and queue status changes. This is preferred over polling to reduce server load and improve responsiveness on free-tier infrastructure.

- **AD-009 Layered Caching Strategy**: Three-tier caching using Upstash Redis: L1 (provider availability, 30-second TTL), L2 (patient profiles, 5-minute TTL), L3 (360-degree views, 15-minute TTL). Cache invalidation is event-driven from write operations. This achieves the 60% database load reduction target (NFR-017).

- **AD-010 Trust-First AI Verification Workflow**: Every AI-generated output (extracted data, medical codes, risk scores) enters the system with a "Suggested" status. Clinical staff must explicitly verify, modify, or reject each suggestion. This implements the >98% AI-Human agreement tracking (AIR-Q01) and addresses the Black Box trust deficit identified in the business justification.

## Technology Stack

| Layer                  | Technology                                       | Version      | Justification (NFR/DR/AIR)                                                                                                                                          |
| ---------------------- | ------------------------------------------------ | ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Frontend               | React with TypeScript                            | 18.x         | NFR-001 (performance), NFR-015 (scalability); component-based architecture enables modular UI development; TypeScript adds type safety for complex healthcare forms |
| Frontend State         | Redux Toolkit                                    | 2.x          | NFR-002 (real-time updates); centralized state management for appointment slots, queue status, and patient views                                                    |
| Frontend Hosting       | Vercel (free tier)                               | N/A          | C-1 (free hosting constraint); automatic HTTPS, global CDN, zero-config deployment                                                                                  |
| Real-Time              | SignalR (.NET) + @microsoft/signalr (client)     | 8.x          | NFR-002 (500ms slot update latency); WebSocket-based bidirectional communication                                                                                    |
| Backend                | ASP.NET Core Web API (.NET)                      | 8.0 LTS      | NFR-001 (performance), NFR-010 (OWASP), NFR-021 (modular architecture); high-performance, built-in DI, middleware pipeline                                          |
| Backend Hosting        | GitHub Codespaces / Railway (free tier)          | N/A          | C-1 (free hosting); supports .NET runtime with SQL Server connectivity                                                                                              |
| Authentication         | ASP.NET Identity + JWT Bearer                    | 8.0          | NFR-007 (Argon2id hashing), NFR-008 (session timeout), NFR-009 (RBAC)                                                                                               |
| Database               | SQL Server Express                               | 2022         | DR-001 through DR-009 (data structures, integrity); TDE for encryption at rest (NFR-005); full-text search for clinical data                                        |
| Caching                | Upstash Redis (free tier)                        | 7.x          | NFR-017 (60% DB load reduction); serverless Redis with HTTPS-only access; 10K commands/day free                                                                     |
| ORM                    | Entity Framework Core                            | 8.0          | DR-016 (code-first migrations), DR-008 (referential integrity); LINQ prevents SQL injection (NFR-010)                                                               |
| AI - LLM Runtime       | Ollama                                           | 0.3.x        | AIR-S01 (local inference, no PHI transmission); hosts Phi-3-mini for conversational intake                                                                          |
| AI - LLM Orchestration | Semantic Kernel (.NET)                           | 1.x          | AIR-003 (conversational intake), AIR-004/005 (tool calling for code mapping); .NET-native AI orchestration                                                          |
| AI - NER               | scispaCy (en_ner_bc5cdr_md)                      | 0.5.x        | AIR-001 (clinical NER extraction); biomedical-trained NER for medications, diseases, chemicals                                                                      |
| AI - OCR               | Tesseract OCR (.NET wrapper)                     | 5.x          | AIR-001 (scanned PDF processing); open-source, no API costs                                                                                                         |
| AI - Vector Storage    | SQL Server custom vector table                   | N/A          | AIR-R04 (embedding storage); avoids external vector DB dependency                                                                                                   |
| AI - ML Classification | ML.NET                                           | 3.x          | AIR-007 (no-show risk model); .NET-native, trainable on historical appointment data                                                                                 |
| PDF Generation         | QuestPDF                                         | 2024.x       | FR-022 (appointment PDF confirmation); open-source, fluent .NET API                                                                                                 |
| Email Service          | SendGrid (free tier)                             | N/A          | FR-019, FR-022 (email reminders, PDF delivery); 100 emails/day free                                                                                                 |
| SMS Gateway            | Twilio (free trial)                              | N/A          | FR-019 (SMS reminders); free trial credits for Phase 1                                                                                                              |
| Calendar Integration   | Google Calendar API + Microsoft Graph API        | v3 / v1.0    | FR-020, FR-021 (calendar sync); both offer free API tiers                                                                                                           |
| Testing - Unit         | xUnit + Moq                                      | 2.x / 4.x    | NFR-022 (80% unit test coverage); .NET standard testing framework                                                                                                   |
| Testing - Integration  | WebApplicationFactory + Testcontainers           | 8.0 / 3.x    | NFR-022 (60% integration coverage); in-memory test server with containerized SQL Server                                                                             |
| Testing - E2E          | Playwright                                       | 1.x          | NFR-001 (end-to-end performance validation); cross-browser testing                                                                                                  |
| API Documentation      | Swagger/OpenAPI (Swashbuckle)                    | 6.x          | NFR-021 (maintainability); auto-generated API documentation                                                                                                         |
| Logging                | Serilog + Seq (free tier)                        | 3.x / 2024.x | NFR-011 (PHI-scrubbed logging); structured logging with configurable sinks                                                                                          |
| Monitoring             | Application Insights (free tier) / Health Checks | N/A          | NFR-012 (99.9% uptime monitoring); built-in .NET health check endpoints                                                                                             |

### Alternative Technology Options

- **PostgreSQL** was evaluated as a database alternative scoring higher on free hosting (Neon/Supabase free tiers) but was not selected because the spec mandates SQL Server. If free-tier SQL Server hosting proves untenable, PostgreSQL with pgvector (consolidating vector storage) is the recommended migration target.
- **Node.js/Express** was evaluated as a backend alternative with stronger free hosting options (Vercel serverless, Railway) but was not selected because the spec mandates .NET and C# type safety is critical for PHI handling.
- **Angular** was evaluated as a frontend alternative with built-in accessibility features and form handling but was not selected because the spec mandates React.
- **Pinecone/ChromaDB** were evaluated for vector storage but not selected to avoid introducing external service dependencies (C-1) when SQL Server custom vector tables satisfy the RAG retrieval requirements (AIR-R04).
- **OpenAI/Azure OpenAI** were evaluated for LLM inference but not selected due to the no-paid-cloud constraint (C-1) and HIPAA PHI transmission concerns (AIR-S01). Local Ollama inference eliminates both issues.

### AI Component Stack

| Component         | Technology                  | Purpose                                                                                      |
| ----------------- | --------------------------- | -------------------------------------------------------------------------------------------- |
| Model Provider    | Ollama + Phi-3-mini (3.8B)  | Local LLM inference for conversational intake and text understanding                         |
| Biomedical NER    | scispaCy (en_ner_bc5cdr_md) | Named entity recognition for medications, diseases, chemicals from clinical text             |
| OCR Engine        | Tesseract OCR 5.x           | Text extraction from scanned/image-based PDF documents                                       |
| LLM Orchestration | Semantic Kernel 1.x         | Prompt management, tool calling, conversation memory for .NET                                |
| Vector Store      | SQL Server custom tables    | Document embedding storage with cosine similarity search                                     |
| ML Classification | ML.NET 3.x                  | No-show risk prediction model, trainable on appointment history                              |
| Guardrails        | Custom .NET middleware      | JSON schema validation for AI outputs, confidence threshold enforcement, token budget limits |

### Technology Decision

| Metric (from NFR/DR/AIR)                | React + .NET + SQL Server | Angular + Node.js + PostgreSQL | Rationale                                                                 |
| --------------------------------------- | ------------------------- | ------------------------------ | ------------------------------------------------------------------------- |
| Spec compliance (mandatory)             | 10/10                     | 0/10                           | Spec explicitly mandates React, .NET, SQL Server                          |
| HIPAA security depth (NFR-005, NFR-010) | 9/10                      | 8/10                           | .NET has ASP.NET Identity, built-in encryption; SQL Server TDE            |
| Free-tier hosting (C-1)                 | 6/10                      | 9/10                           | SQL Server Express hosting is limited; PostgreSQL has better free options |
| AI integration (AIR-001 to AIR-008)     | 8/10                      | 7/10                           | Semantic Kernel is .NET-native; ML.NET for classification                 |
| Type safety for PHI (NFR-009)           | 9/10                      | 7/10                           | C# strong typing prevents PHI handling errors                             |
| Real-time capability (NFR-002)          | 9/10                      | 8/10                           | SignalR is mature, production-proven WebSocket solution                   |
| **Weighted Total**                      | **8.5/10**                | **6.5/10**                     | **React + .NET + SQL Server selected**                                    |

## Technical Requirements

- TR-001: System MUST use React 18.x with TypeScript for the frontend single-page application, deployed to Vercel free tier with automatic HTTPS and CDN, justified by NFR-001 (performance) and C-1 (free hosting)
- TR-002: System MUST use ASP.NET Core 8.0 LTS Web API for the backend, implementing the modular monolith pattern with dependency injection for module decoupling, justified by NFR-021 (maintainability) and NFR-010 (OWASP compliance)
- TR-003: System MUST use SQL Server 2022 Express as the primary relational database with Entity Framework Core 8.0 as the ORM, justified by DR-001 through DR-009 (data structures and integrity) and DR-016 (migrations)
- TR-004: System MUST use Upstash Redis (free tier, HTTPS-only) for distributed caching of provider availability, patient profiles, and aggregated views, justified by NFR-017 (60% DB load reduction)
- TR-005: System MUST use SignalR for WebSocket-based real-time communication between server and connected clients for slot availability updates and queue status changes, justified by NFR-002 (500ms update latency)
- TR-006: System MUST use ASP.NET Identity with JWT Bearer token authentication implementing Argon2id password hashing and refresh token rotation, justified by NFR-007 (password security), NFR-008 (session timeout), and NFR-009 (RBAC)
- TR-007: System MUST use Ollama as the local LLM runtime hosting Phi-3-mini for conversational AI and Semantic Kernel as the .NET orchestration layer for prompt management and tool calling, justified by AIR-S01 (local inference) and AIR-003 (conversational intake)
- TR-008: System MUST use Tesseract OCR 5.x for extracting text from scanned PDF documents and scispaCy with the en_ner_bc5cdr_md model for biomedical named entity recognition, justified by AIR-001 (clinical data extraction)
- TR-009: System MUST use ML.NET for training and deploying the no-show risk classification model using historical appointment data, justified by AIR-007 (risk assessment) and C-1 (no paid ML services)
- TR-010: System MUST implement RESTful API design following OpenAPI 3.0 specification with Swashbuckle-generated Swagger documentation, using consistent resource naming, HTTP status codes, and versioned endpoints (v1/), justified by NFR-021 (maintainability)
- TR-011: System MUST use xUnit for unit testing, WebApplicationFactory for integration testing, and Playwright for end-to-end testing, justified by NFR-022 (test coverage targets)
- TR-012: System MUST use Serilog for structured logging with PHI-scrubbing enrichers and configurable sinks (Console, Seq free tier), justified by NFR-011 (PHI protection in logs)
- TR-013: System MUST implement health check endpoints (/health/live, /health/ready) using ASP.NET Core Health Checks for monitoring uptime and dependency status, justified by NFR-012 (99.9% uptime target)
- TR-014: System MUST use QuestPDF for generating appointment confirmation PDF documents with provider details, date, time, location, and preparation instructions, justified by FR-022 (PDF confirmations)
- TR-015: System MUST use SendGrid free tier for transactional email delivery (reminders, confirmations, PDF attachments) and Twilio free trial for SMS delivery, justified by FR-019 (multi-channel reminders)
- TR-016: System MUST use Google Calendar API v3 and Microsoft Graph API v1.0 with OAuth 2.0 authorization for bidirectional calendar synchronization, justified by FR-020 and FR-021 (calendar sync)

## Technical Constraints & Assumptions

### Constraints

- C-1: All hosting and infrastructure must use free, open-source-friendly platforms with zero paid cloud services in Phase 1. This limits database to SQL Server Express (10 GB max, 1 GB RAM), caching to Upstash Redis free tier (10K commands/day), and hosting to Vercel/Netlify free tier (100 GB bandwidth/month) or GitHub Codespaces.
- C-2: No provider-facing features, provider logins, or provider-side actions. Provider data is managed by Admin/Staff through backend operations.
- C-3: Insurance pre-check validates against internal dummy records only, not live insurance APIs or clearinghouses.
- C-4: SQL Server Express 10 GB database size limit constrains total clinical document storage. Large-scale document archival requires a blob storage strategy (encrypted file system or future cloud blob storage).
- C-5: Ollama local LLM inference is constrained by available compute resources (CPU-only on free tiers). Model size is limited to 3-4B parameter models (Phi-3-mini). Larger models requiring GPU are not feasible in Phase 1.
- C-6: Upstash Redis free tier limits (10K commands/day, 256 MB) may be insufficient under sustained load. Cache strategy must prioritize high-value keys and implement local in-memory fallback.
- C-7: SendGrid free tier (100 emails/day) and Twilio free trial credits impose hard limits on notification volume. Reminder batching and prioritization are required.

### Assumptions

- A-1: AI/NLP processing will use free, open-source models (Phi-3-mini via Ollama, scispaCy, Tesseract) compatible with free-tier infrastructure. No paid AI API subscriptions.
- A-2: Patients upload clinical documents in PDF format only. Other document formats (DOCX, images) are not supported in Phase 1.
- A-3: The development team has access to GitHub Codespaces or equivalent free cloud development environment that can run .NET 8, SQL Server Express, and Ollama concurrently.
- A-4: The predefined dummy insurance record set is small (fewer than 100 records) and can be seeded via Entity Framework migrations.
- A-5: Initial no-show risk model will be trained on synthetic appointment data until sufficient real patient history is accumulated.
- A-6: Calendar API OAuth tokens will be stored encrypted in the database; patients must re-authorize if tokens expire or are revoked.

## Development Workflow

1. **Environment Setup**: Initialize .NET 8 solution with modular monolith structure (Scheduling, Clinical, Identity, Notification modules). Configure SQL Server Express, Upstash Redis connection, and Ollama installation. Scaffold React TypeScript frontend with project structure mirroring backend modules.
2. **Foundation Layer (Identity + Core)**: Implement user registration, authentication (JWT + refresh tokens), RBAC middleware, audit logging infrastructure, and health check endpoints. Establish Entity Framework Core context, initial migrations, and database seeding for dummy insurance records.
3. **Scheduling Module**: Build provider search, slot availability (SignalR real-time), appointment booking with conflict-free slot reservation, waitlist management, preferred slot swap logic, and walk-in/queue management. Integrate Upstash Redis caching for availability views.
4. **Notification Module**: Implement reminder scheduler (background service), multi-channel delivery (SendGrid email, Twilio SMS), PDF generation (QuestPDF), and calendar sync (Google Calendar API, Microsoft Graph API). Add circuit breakers and retry logic for external services.
5. **Clinical Module**: Build document upload with encryption, Tesseract OCR pipeline, scispaCy NER extraction, 360-Degree Patient View aggregation, conflict detection, ICD-10/CPT code mapping (Semantic Kernel + tool calling), and staff verification workflow. Implement the RAG pipeline with SQL Server vector storage.
6. **AI Integration & Risk Module**: Deploy Ollama with Phi-3-mini, implement conversational intake via Semantic Kernel, train ML.NET no-show risk model, and integrate risk scores into reminder escalation logic.
7. **Security Hardening**: Conduct OWASP Top 10 review, implement CSP/HSTS headers, rate limiting, input validation middleware, PHI scrubbing in logs, and penetration testing against authentication and authorization flows.
8. **Testing & Validation**: Execute unit tests (80% coverage target), integration tests (60% coverage), E2E tests (Playwright), AI accuracy evaluation (98% agreement target), and HIPAA compliance checklist verification. Performance test against NFR-001 and NFR-002 thresholds.
