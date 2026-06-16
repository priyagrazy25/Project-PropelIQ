# Unified Patient Access & Clinical Intelligence Platform - Project Plan

---
**Project Name:** Unified Patient Access & Clinical Intelligence Platform  
**Project Manager:** AI Project Manager  
**Document Version:** 1.0  
**Last Updated:** 2025-01-20  
**Status:** Draft  

---

## Executive Summary

The Unified Patient Access & Clinical Intelligence Platform is a standalone healthcare application that consolidates patient scheduling and clinical data management into a single, HIPAA-compliant, patient-centric system. The platform serves three user roles (Patient, Staff, Admin) and integrates deterministic appointment booking workflows with a Trust-First AI-powered clinical intelligence engine. The AI engine consolidates unstructured clinical documents into a verified 360-Degree Patient View with ICD-10 and CPT code mappings, requiring human verification for all AI-generated clinical outputs.

**Business Impact:**
- **No-Show Rate Reduction:** Target measurable decrease from 15% baseline through smart reminders, waitlist management, and dynamic preferred slot swaps
- **Clinical Efficiency:** Eliminate 20-minute manual clinical data extraction bottleneck, freeing clinical staff for patient-facing tasks
- **Market Differentiation:** First unified platform connecting scheduling with clinical data intelligence using Trust-First AI
- **Compliance:** 100% HIPAA-compliant data handling with encrypted storage, transport, and immutable audit logging

**Project Duration:** 24 weeks (6 months)  
**Target Launch:** Q3 2025  
**Infrastructure:** Free/Open-Source only (Vercel, GitHub Codespaces, Upstash Redis, SQL Server Express)

---

## 1. Project Scope

### 1.1 In-Scope Deliverables

#### Phase 1: Core Platform (Weeks 1-24)

**Module 1: Identity & Access Management**
- User registration and authentication (JWT + refresh tokens, Argon2id password hashing)
- Role-based access control (Patient, Staff, Admin)
- 15-minute automatic session timeout
- Immutable audit logging for all PHI-touching operations

**Module 2: Scheduling & Appointment Management**
- Provider search with real-time slot availability (SignalR WebSockets)
- Patient self-service appointment booking with conflict-free slot reservation
- Dynamic Preferred Slot Swap (automatic swap when preferred slot becomes available)
- Waitlist management with automatic notifications
- Walk-in booking (Staff only) with same-day queue management
- Patient arrival marking (Staff only, no patient self-check-in)
- Appointment cancellation and rescheduling

**Module 3: Notifications & Calendar Integration**
- Multi-channel reminders (SMS via Twilio, Email via SendGrid) at 72h, 24h, 2h before appointment
- PDF appointment confirmation generation (QuestPDF)
- Google Calendar sync (OAuth 2.0 + Google Calendar API v3)
- Outlook Calendar sync (OAuth 2.0 + Microsoft Graph API v1.0)
- Circuit breakers and retry logic for external services

**Module 4: Patient Intake**
- AI conversational intake (Ollama + Phi-3-mini via Semantic Kernel) for medical history, symptoms, allergies, medications
- Manual form-based intake (fallback and alternative option)
- Seamless switching between AI and manual intake without data loss
- Insurance pre-check against predefined dummy records

**Module 5: Clinical Intelligence & Document Processing**
- Clinical document upload (PDF only) with encrypted storage (AES-256)
- Asynchronous document processing pipeline (Tesseract OCR + scispaCy NER)
- Structured data extraction (vitals, medical history, medications, allergies, lab results, diagnoses) with confidence scores
- 360-Degree Patient View generation with semantic de-duplication across documents
- Data conflict detection (contradictions flagged with source references and severity)
- ICD-10-CM code mapping with top-3 suggestions and confidence scores
- CPT code mapping with top-3 suggestions and confidence scores
- Staff verification workflow for all AI-generated outputs (Suggested → Verified/Rejected status)

**Module 6: Risk Assessment & Analytics**
- No-show risk scoring (ML.NET classification model) based on patient history, appointment type, time slot
- Risk-based reminder escalation rules

**Module 7: Admin & Operations**
- User account management (create, update, deactivate)
- Role assignment (Patient/Staff/Admin)
- Audit log access with 7-year retention

### 1.2 Out of Scope (Phase 1)

- Provider logins and provider-facing features
- Payment gateway integration or billing workflows
- Family profiles or household account linking
- Patient self-check-in via app, web portal, or QR code
- Bi-directional EHR integration (HL7/FHIR)
- Full claims submission to insurance clearinghouses
- Paid cloud infrastructure (Azure, AWS, GCP)
- Multi-format document support (DOCX, images beyond PDF)
- Mobile native applications (iOS/Android)

### 1.3 Scope Change Management

All scope change requests must be submitted to the Project Manager with:
- **Business justification** (impact on success criteria)
- **Effort estimate** (story points)
- **Risk assessment** (technical, timeline, budget)
- **Approval required from:** Product Owner (business impact) + Tech Lead (technical feasibility)

Changes affecting timeline by >1 week or adding >40 story points require stakeholder approval.

---

## 2. Requirements Summary

### 2.1 Functional Requirements Traceability

| Category | Requirement Count | Complexity Distribution |
|----------|-------------------|------------------------|
| Authentication & User Management | 4 (FR-001 to FR-004) | Low: 4 |
| Appointment Booking | 9 (FR-005 to FR-013) | Low: 5, Medium: 3, High: 1 (FR-008 Slot Swap) |
| Patient Intake | 4 (FR-014 to FR-017) | Low: 3, High: 1 (FR-014 AI Intake) |
| Insurance Verification | 1 (FR-018) | Low: 1 |
| Notifications & Calendar | 4 (FR-019 to FR-022) | Low: 2, Medium: 2 |
| Clinical Data Aggregation | 4 (FR-023 to FR-026) | High: 4 (all AI-powered) |
| Medical Coding | 2 (FR-027, FR-028) | High: 2 (AI classification) |
| Risk Assessment | 1 (FR-029) | Medium: 1 (Hybrid AI) |
| Security & Compliance | 3 (FR-030 to FR-032) | Medium: 3 |
| **Total** | **32 FRs** | **Low: 15, Medium: 9, High: 8** |

### 2.2 Non-Functional Requirements Summary

| Category | Key Metrics | Requirements |
|----------|-------------|--------------|
| **Performance** | API response <2s (p95), WebSocket updates <500ms, OCR pipeline <5min/doc, Dashboard render <3s | NFR-001 to NFR-004 |
| **Security** | AES-256 encryption (rest), TLS 1.2+ (transit), Argon2id (passwords), 15min session timeout, RBAC, OWASP Top 10, PHI redaction | NFR-005 to NFR-011 |
| **Availability** | 99.9% uptime (43.8min/month max downtime), Graceful degradation, Circuit breakers | NFR-012 to NFR-014 |
| **Scalability** | 500 concurrent users, 10K docs/month, 60% DB load reduction via caching | NFR-015 to NFR-017 |
| **Reliability** | MTBF 720 hours, Idempotent APIs, 3-retry exponential backoff | NFR-018 to NFR-020 |
| **Maintainability** | Modular monolith, 80% unit test coverage, 60% integration test coverage, EF Core migrations | NFR-021 to NFR-023 |

### 2.3 AI Requirements Summary

| AI Feature | Pattern | Quality Target | Safety Controls |
|------------|---------|----------------|-----------------|
| Conversational Intake | Local LLM (Ollama + Phi-3-mini) | p95 latency <5s | No PHI to external services, PII redaction, 4K token budget |
| Clinical Document Parsing | RAG (Tesseract OCR + scispaCy NER) | 90% recall, 99% schema validity | Local processing only, confidence scoring, 8K token budget |
| 360-Degree Patient View | RAG (semantic de-duplication) | Top-5 chunks, cosine similarity >0.75 | Source document citation, recency weighting |
| Conflict Detection | Hybrid (AI flags, human resolves) | Critical/Warning severity classification | Flagged for staff review with source references |
| ICD-10/CPT Code Mapping | Tool Calling (classification + lookup) | >98% AI-Human agreement rate | Suggested status, staff verification required |
| No-Show Risk Scoring | Hybrid (ML.NET classification) | Predictive accuracy monitoring | Risk-based escalation, human override |

---

## 3. AI-Adjusted Effort Estimation

### 3.1 Estimation Methodology

**Base Estimation Approach:**
- **Story Points:** 1 SP = 8 hours (1 developer day)
- **Complexity Factors:**
  - Low: 1-3 SP (deterministic CRUD, simple validation)
  - Medium: 5-8 SP (external API integration, caching, SignalR)
  - High: 13-21 SP (AI/ML pipelines, RAG, complex state machines)
- **AI Multipliers:**
  - AI-CANDIDATE features: +60% effort (model integration, prompt engineering, evaluation)
  - HYBRID features: +40% effort (AI + deterministic logic integration)
  - RAG pipelines: +80% effort (chunking, embeddings, retrieval, re-ranking)

**Historical Data Calibration:**
- Similar healthcare platform (EMR scheduling module): 480 SP across 16 weeks with 3 developers
- AI document extraction project (insurance claims): 240 SP across 12 weeks with 2 AI/ML engineers
- **Velocity Assumption:** 15 SP/week per developer (conservative for healthcare compliance overhead)

### 3.2 Module Effort Breakdown

| Module | Features | Base SP | AI Multiplier | Adjusted SP | Developer Weeks |
|--------|----------|---------|---------------|-------------|-----------------|
| **Identity & Access** | FR-001 to FR-004, NFR-007 to NFR-009, RBAC, Audit Logging | 40 | N/A | 40 | 2.7 weeks |
| **Scheduling & Appointments** | FR-005 to FR-013, SignalR, Slot Swap, Waitlist, Queue | 120 | N/A | 120 | 8.0 weeks |
| **Notifications & Calendar** | FR-019 to FR-022, SendGrid, Twilio, Google/Outlook API, PDF | 60 | N/A | 60 | 4.0 weeks |
| **Patient Intake** | FR-014 (AI), FR-015 to FR-017, FR-018 (Insurance) | 50 | +60% (AI Intake) | 80 | 5.3 weeks |
| **Clinical Intelligence** | FR-023 to FR-026 (OCR, NER, 360-View, Conflicts) | 150 | +80% (RAG) | 270 | 18.0 weeks |
| **Medical Coding** | FR-027, FR-028 (ICD-10, CPT) | 60 | +60% (AI Classification) | 96 | 6.4 weeks |
| **Risk Assessment** | FR-029 (No-Show Risk ML.NET) | 40 | +40% (Hybrid) | 56 | 3.7 weeks |
| **Security Hardening** | NFR-005, NFR-006, NFR-010, NFR-011, OWASP Top 10 | 50 | N/A | 50 | 3.3 weeks |
| **Testing & QA** | Unit (80%), Integration (60%), E2E (Playwright), AI Evaluation | 80 | +40% (AI Testing) | 112 | 7.5 weeks |
| **Frontend (React)** | 7 screens (Login, Dashboard, Booking, Intake, Documents, 360-View, Admin) | 100 | N/A | 100 | 6.7 weeks |
| **DevOps & Deployment** | CI/CD, Free-tier hosting, SQL Server Express, Upstash Redis, Ollama setup | 30 | N/A | 30 | 2.0 weeks |
| **Documentation & Handoff** | API docs (Swagger), User guides, Deployment guides, Training materials | 20 | N/A | 20 | 1.3 weeks |
| **TOTAL** | 32 FRs + NFRs + AIRs | **800 SP** | **+32% average** | **1,034 SP** | **68.9 dev-weeks** |

### 3.3 Contingency Buffer

- **Base Estimate:** 1,034 SP = 68.9 developer-weeks
- **Contingency Buffer (20%):** +207 SP = +13.8 developer-weeks
- **Total Adjusted Estimate:** 1,241 SP = 82.7 developer-weeks

**Buffer Allocation Rationale:**
- Free-tier infrastructure unknowns (SQL Server Express hosting, Ollama compute constraints): +5%
- AI model accuracy tuning and prompt engineering iterations: +8%
- HIPAA compliance testing and audit preparation: +4%
- Integration testing with external APIs (Calendar, SMS, Email): +3%

---

## 4. Team Composition (Auto-Derived)

### 4.1 Core Team Structure

| Role | Count | Responsibilities | Key Skills | Allocation |
|------|-------|------------------|------------|------------|
| **Tech Lead / Solution Architect** | 1 | Architecture decisions, modular monolith design, HIPAA compliance oversight, code reviews | .NET Core 8, Clean Architecture, HIPAA, SQL Server | Full-time (24 weeks) |
| **Backend Developer (.NET)** | 2 | API development, EF Core migrations, SignalR, identity/auth, notification module | ASP.NET Core, Entity Framework, JWT, SignalR, xUnit | Full-time (24 weeks) |
| **Frontend Developer (React)** | 1 | React UI, TypeScript, Redux, SignalR client, responsive design | React 18, TypeScript, Redux Toolkit, Playwright | Full-time (24 weeks) |
| **AI/ML Engineer** | 1 | Ollama/Semantic Kernel integration, RAG pipeline, scispaCy NER, ML.NET risk model | Python (scispaCy), .NET (Semantic Kernel, ML.NET), NLP, RAG | Full-time (20 weeks: Weeks 5-24) |
| **QA Engineer** | 1 | Test automation (xUnit, Playwright), AI accuracy evaluation, security testing | xUnit, Moq, Playwright, OWASP, HIPAA testing | Half-time (12 weeks: Weeks 13-24) |
| **DevOps Engineer** | 1 | CI/CD pipelines, free-tier hosting, SQL Server Express, Upstash Redis, Ollama deployment | GitHub Actions, Docker, Vercel, SQL Server, Redis | Part-time (8 weeks: Weeks 1-4, 21-24) |
| **Project Manager** | 1 | Sprint planning, stakeholder communication, risk management, timeline tracking | Agile/Scrum, JIRA, healthcare domain | Full-time (24 weeks) |

**Total Team Size:** 7 FTE (Full-Time Equivalent)  
**Peak Team Size:** 8 people (Weeks 13-24)

### 4.2 Skills Matrix

| Technology | Required Level | Team Coverage |
|------------|----------------|---------------|
| .NET Core 8 / C# | Expert | Tech Lead, 2 Backend Devs |
| React 18 / TypeScript | Expert | 1 Frontend Dev |
| SQL Server 2022 / EF Core | Advanced | Tech Lead, 2 Backend Devs |
| SignalR | Advanced | 1 Backend Dev, 1 Frontend Dev |
| Semantic Kernel / Ollama | Advanced | 1 AI/ML Engineer |
| scispaCy / NLP | Advanced | 1 AI/ML Engineer |
| ML.NET | Intermediate | 1 AI/ML Engineer |
| HIPAA Compliance | Expert | Tech Lead |
| Playwright / E2E Testing | Advanced | 1 QA Engineer, 1 Frontend Dev |
| CI/CD / DevOps | Advanced | 1 DevOps Engineer |

---

## 5. Milestones & Timeline

### 5.1 Major Milestones

| Milestone | Target Date | Deliverables | Success Criteria |
|-----------|-------------|--------------|------------------|
| **M1: Foundation Complete** | Week 4 | Identity module, DB setup, CI/CD pipeline | User registration/login working, JWT auth, EF migrations deployed |
| **M2: Scheduling Module Live** | Week 12 | Provider search, booking, SignalR updates, waitlist, slot swap | End-to-end appointment booking flow tested, real-time updates <500ms |
| **M3: Notifications & Calendar Sync** | Week 16 | SMS/Email reminders, PDF generation, Google/Outlook sync | Multi-channel reminders sent, calendar events created successfully |
| **M4: AI Intake & Clinical Docs** | Week 20 | Conversational intake, document upload, OCR/NER extraction | AI intake dialogue functional, documents parsed with >90% recall |
| **M5: Clinical Intelligence Complete** | Week 23 | 360-Degree View, conflict detection, ICD-10/CPT mapping | 360-view generated with de-duplication, >98% AI-human agreement on codes |
| **M6: Production-Ready Platform** | Week 24 | Full platform with security hardening, testing complete, docs ready | 80% unit coverage, 60% integration coverage, OWASP Top 10 compliance verified |

### 5.2 Detailed Timeline (Phase 1)

#### **Weeks 1-4: Foundation & Identity**
- Environment setup (SQL Server Express, Upstash Redis, Ollama, Vercel/Railway)
- User registration, authentication (JWT + refresh tokens), RBAC
- Audit logging infrastructure
- EF Core migrations and database seeding
- CI/CD pipeline (GitHub Actions)
- **Deliverable:** M1 - Foundation Complete

#### **Weeks 5-12: Scheduling Module**
- Provider search with filters (specialty, name, date range)
- Slot availability (SignalR WebSocket real-time updates)
- Appointment booking with conflict-free slot reservation (database row locking)
- Dynamic Preferred Slot Swap (auto-swap logic with event-driven notifications)
- Waitlist management (FIFO queue, auto-notify on slot release)
- Walk-in booking (Staff only) and same-day queue
- Patient arrival marking (Staff only)
- Cancellation and rescheduling with slot release
- Upstash Redis caching for provider availability (30s TTL)
- **Deliverable:** M2 - Scheduling Module Live

#### **Weeks 13-16: Notifications & Calendar Integration**
- Reminder scheduler (background service with 72h, 24h, 2h intervals)
- SendGrid email integration (transactional emails)
- Twilio SMS integration (reminder texts)
- QuestPDF appointment confirmation generation
- Google Calendar API OAuth 2.0 + event creation
- Microsoft Graph API OAuth 2.0 + event creation
- Circuit breakers and exponential backoff retry logic
- **Deliverable:** M3 - Notifications & Calendar Sync

#### **Weeks 17-20: Patient Intake & Document Upload**
- AI conversational intake (Semantic Kernel + Ollama Phi-3-mini)
- Manual form-based intake with structured fields
- Seamless intake mode switching with data preservation
- Insurance pre-check against dummy records
- Clinical document upload (encrypted storage, AES-256)
- Tesseract OCR pipeline (async background job queue)
- scispaCy NER extraction (vitals, medications, allergies, diagnoses)
- Confidence scoring (0.0-1.0) for extracted data points
- **Deliverable:** M4 - AI Intake & Clinical Docs

#### **Weeks 21-23: Clinical Intelligence & Medical Coding**
- 360-Degree Patient View aggregation with semantic de-duplication
- SQL Server custom vector table for embeddings
- RAG pipeline (chunking, retrieval, re-ranking with recency weighting)
- Data conflict detection (contradictions flagged with severity: Critical/Warning)
- ICD-10-CM code mapping (Semantic Kernel tool calling + classification)
- CPT code mapping (classification + lookup)
- Staff verification workflow (Suggested → Verified/Rejected)
- No-show risk scoring (ML.NET classification model)
- **Deliverable:** M5 - Clinical Intelligence Complete

#### **Weeks 24: Security, Testing & Launch Prep**
- OWASP Top 10 security review (CSP, HSTS, rate limiting, input validation)
- PHI scrubbing in logs (Serilog enrichers)
- Unit testing (80% coverage target with xUnit + Moq)
- Integration testing (60% coverage with WebApplicationFactory + Testcontainers)
- E2E testing (Playwright cross-browser)
- AI accuracy evaluation (>98% agreement rate verification)
- HIPAA compliance checklist verification
- Performance testing (NFR-001 to NFR-004 validation)
- API documentation (Swagger/OpenAPI)
- User guides and training materials
- **Deliverable:** M6 - Production-Ready Platform

---

## 6. Cost Baseline

### 6.1 Labor Costs

**Assumptions:**
- Industry-standard blended rate: $100/hour (includes salary, benefits, overhead)
- 40-hour work week per FTE

| Role | FTE | Duration (weeks) | Total Hours | Blended Rate | Total Cost |
|------|-----|------------------|-------------|--------------|------------|
| Tech Lead | 1.0 | 24 | 960 | $100/hr | $96,000 |
| Backend Developer | 2.0 | 24 | 1,920 | $100/hr | $192,000 |
| Frontend Developer | 1.0 | 24 | 960 | $100/hr | $96,000 |
| AI/ML Engineer | 1.0 | 20 | 800 | $120/hr | $96,000 |
| QA Engineer | 0.5 | 12 | 240 | $90/hr | $21,600 |
| DevOps Engineer | 0.3 | 8 | 96 | $110/hr | $10,560 |
| Project Manager | 1.0 | 24 | 960 | $95/hr | $91,200 |
| **TOTAL LABOR** | **7.3 FTE** | **24 weeks** | **5,936 hours** | **$101.62/hr avg** | **$603,360** |

### 6.2 Infrastructure Costs (Free Tier)

All infrastructure components leverage free/open-source tiers:

| Service | Provider | Free Tier Limits | Monthly Cost | 6-Month Cost |
|---------|----------|------------------|--------------|--------------|
| Frontend Hosting | Vercel | 100 GB bandwidth, automatic HTTPS, CDN | $0 | $0 |
| Backend Hosting | Railway / GitHub Codespaces | 500 hours/month free compute | $0 | $0 |
| Database | SQL Server Express | 10 GB storage, 1 GB RAM | $0 | $0 |
| Caching | Upstash Redis | 10K commands/day, 256 MB | $0 | $0 |
| Email Service | SendGrid | 100 emails/day | $0 | $0 |
| SMS Gateway | Twilio | Free trial credits | $0* | $0* |
| Logging | Seq | Free tier for single user | $0 | $0 |
| Monitoring | Application Insights | 5 GB/month free | $0 | $0 |
| **TOTAL INFRASTRUCTURE** | | | **$0** | **$0** |

*Twilio free trial credits expire after initial allocation; production deployment will require paid tier (~$50/month for moderate SMS volume).

### 6.3 Total Project Cost Estimate

| Category | Cost | % of Total |
|----------|------|-----------|
| Labor | $603,360 | 100% |
| Infrastructure (Phase 1) | $0 | 0% |
| **TOTAL PROJECT COST** | **$603,360** | **100%** |
| Contingency Reserve (15%) | $90,504 | - |
| **TOTAL WITH CONTINGENCY** | **$693,864** | - |

**Cost per Story Point:** $693,864 ÷ 1,241 SP = **$559/SP**

---

## 7. Risk Register

### 7.1 Technical Risks

| Risk ID | Description | Probability | Impact | Severity | Mitigation Strategy | Owner |
|---------|-------------|-------------|--------|----------|---------------------|-------|
| **TR-01** | SQL Server Express 10 GB limit reached before Phase 1 completion | Medium | High | **HIGH** | Implement document compression, monitor storage weekly, prepare PostgreSQL migration plan | Tech Lead |
| **TR-02** | Upstash Redis free tier (10K commands/day) insufficient under load | Medium | Medium | **MEDIUM** | Implement local in-memory cache fallback (IMemoryCache), prioritize high-value keys only | Backend Dev |
| **TR-03** | Ollama CPU-only inference too slow (>5s p95 latency) on free-tier compute | Medium | High | **HIGH** | Optimize model size (use Phi-3-mini 3.8B max), implement request queuing, async processing | AI/ML Engineer |
| **TR-04** | scispaCy NER recall <90% on real-world clinical documents | High | High | **CRITICAL** | Build evaluation dataset with manual annotations, fine-tune NER model if needed, implement confidence threshold filtering | AI/ML Engineer |
| **TR-05** | Calendar API OAuth token expiration causing sync failures | Low | Medium | **MEDIUM** | Implement refresh token rotation, store encrypted tokens, graceful degradation with user re-auth prompt | Backend Dev |
| **TR-06** | SignalR WebSocket connection failures on free-tier hosting | Medium | Medium | **MEDIUM** | Implement long polling fallback, connection retry logic with exponential backoff, health check monitoring | Backend Dev |
| **TR-07** | Free-tier hosting providers (Vercel, Railway) deprecate .NET support | Low | Critical | **MEDIUM** | Maintain deployment scripts for multiple providers (Railway, Render, Fly.io), containerize backend with Docker | DevOps Engineer |

### 7.2 Compliance Risks

| Risk ID | Description | Probability | Impact | Severity | Mitigation Strategy | Owner |
|---------|-------------|-------------|--------|----------|---------------------|-------|
| **CR-01** | PHI inadvertently logged or transmitted to external AI services | Low | Critical | **HIGH** | Implement PHI scrubbing middleware, pre-production HIPAA audit, penetration testing, local-only AI inference | Tech Lead |
| **CR-02** | Insufficient audit log coverage for HIPAA compliance | Low | High | **MEDIUM** | Maintain comprehensive audit event list, automated coverage testing, mock HIPAA audit review | QA Engineer |
| **CR-03** | Session timeout implementation bypassed or inconsistent | Low | Medium | **MEDIUM** | Implement server-side session validation, automated timeout testing in integration tests | Backend Dev |

### 7.3 Schedule Risks

| Risk ID | Description | Probability | Impact | Severity | Mitigation Strategy | Owner |
|---------|-------------|-------------|--------|----------|---------------------|-------|
| **SR-01** | AI model accuracy tuning takes longer than estimated (+4 weeks) | High | High | **CRITICAL** | Start AI feature development early (Week 5), parallel track with scheduling module, buffer allocation | PM |
| **SR-02** | External API integration delays (Google/Outlook Calendar, Twilio, SendGrid) | Medium | Medium | **MEDIUM** | Prototype integrations in Week 1, maintain fallback manual notification options | Backend Dev |
| **SR-03** | Security hardening uncovers critical vulnerabilities requiring redesign | Low | High | **MEDIUM** | Conduct early security design review (Week 8), incremental OWASP checklist validation | Tech Lead |

### 7.4 Resource Risks

| Risk ID | Description | Probability | Impact | Severity | Mitigation Strategy | Owner |
|---------|-------------|-------------|--------|----------|---------------------|-------|
| **RR-01** | AI/ML Engineer availability delayed or unavailable | Medium | High | **HIGH** | Cross-train Backend Dev on Semantic Kernel basics, maintain vendor contact list for contract AI engineers | PM |
| **RR-02** | Team knowledge gaps in HIPAA compliance requirements | Medium | Medium | **MEDIUM** | Mandatory HIPAA training in Week 1, engage HIPAA compliance consultant for design review | PM |
| **RR-03** | Free-tier compute resources insufficient for parallel development environments | Low | Medium | **MEDIUM** | Use GitHub Codespaces for ephemeral dev environments, share SQL Server Express instance with namespace isolation | DevOps Engineer |

### 7.5 Risk Response Plan

**Critical Severity Risks (TR-04, SR-01):**
- **Trigger:** NER recall drops below 85% after 2 weeks of tuning, OR AI development falls >2 weeks behind schedule
- **Response:** Allocate +1 AI/ML contract engineer for 8 weeks, reduce scope of CPT code mapping to manual-only (descope FR-028)

**High Severity Risks (TR-01, TR-03, CR-01):**
- **Trigger:** SQL Server storage >8 GB by Week 16, OR Ollama latency >7s p95, OR PHI detected in logs during testing
- **Response:** Immediate escalation to Tech Lead, mandatory architecture review within 48 hours, implement mitigation plan before next sprint

---

## 8. Dependencies & Assumptions

### 8.1 External Dependencies

| Dependency | Provider | Required By | Mitigation if Unavailable |
|------------|----------|-------------|---------------------------|
| Vercel Free Tier | Vercel Inc. | Frontend deployment | Migrate to Netlify, GitHub Pages, or Cloudflare Pages |
| Railway/Codespaces Free Tier | Railway/GitHub | Backend deployment | Migrate to Render.com, Fly.io, or local Docker hosting |
| SQL Server Express | Microsoft | Data persistence | Migrate to PostgreSQL (Supabase/Neon free tier) + pgvector |
| Upstash Redis | Upstash | Caching | Use in-memory cache (IMemoryCache) with data loss on restart |
| SendGrid Free Tier | SendGrid/Twilio | Email notifications | Switch to SMTP.com (500 emails/month free) or AWS SES |
| Twilio Free Trial | Twilio | SMS notifications | Remove SMS, email-only notifications |
| Google Calendar API | Google | Calendar sync | Outlook-only sync, or descope calendar feature |
| Outlook Calendar API | Microsoft | Calendar sync | Google-only sync, or descope calendar feature |
| Ollama Runtime | Ollama Community | Local LLM inference | Fallback to manual intake only (descope FR-014) |

### 8.2 Key Assumptions

| Assumption | Impact if Invalid | Validation Plan |
|------------|-------------------|-----------------|
| SQL Server Express 10 GB sufficient for Phase 1 clinical documents | Storage overflow, platform inoperability | Monitor storage weekly, document compression prototyping in Week 8 |
| Upstash Redis 10K commands/day sufficient for 500 concurrent users | Cache misses, performance degradation | Load testing with 500 concurrent users in Week 18 |
| Phi-3-mini 3.8B model adequate for conversational intake and clinical NER | Poor AI accuracy, user dissatisfaction | AI evaluation with test dataset in Week 17, >90% recall required |
| Free-tier hosting supports .NET 8 and SignalR WebSockets | Deployment failure, feature unavailability | Prototype deployment to Railway/Vercel in Week 1 |
| Synthetic appointment data adequate for training no-show risk model | Inaccurate risk predictions | Collect 500+ real patient appointment records by Week 20, retrain model |
| Patients upload PDF documents only (no DOCX, images) | User complaints, adoption friction | User research in Week 12, evaluate DOCX support feasibility |
| No HIPAA BAA required for free-tier providers in Phase 1 development | Legal non-compliance | Legal review with healthcare compliance attorney in Week 2 |

---

## 9. Success Metrics & KPIs

### 9.1 Business Metrics

| Metric | Baseline | Target (Post-Launch) | Measurement Method |
|--------|----------|----------------------|--------------------|
| No-Show Rate | 15% | <12% (-3 percentage points) | (No-shows ÷ Total appointments) × 100, measured monthly |
| Staff Clinical Prep Time | 20 minutes/patient | <10 minutes/patient (-50%) | Time study with 30 patient sample, pre/post 360-view deployment |
| Patient Dashboards Created | 0 | >500 in first 3 months | Count of unique patient accounts with ≥1 login |
| Appointments Booked | 0 | >1,000 in first 3 months | Count of appointments with status "Confirmed" or "Completed" |
| Platform Uptime | N/A | >99.9% (43.8 min downtime/month) | Automated uptime monitoring (Application Insights) |

### 9.2 AI Quality Metrics

| Metric | Target | Measurement Method |
|--------|--------|--------------------|
| AI-Human Agreement Rate (ICD-10/CPT) | >98% | (Codes accepted without modification ÷ Total codes suggested) × 100, rolling 30-day window |
| NER Extraction Recall | >90% | Manual annotation of 100 test documents, measured as (True Positives ÷ (True Positives + False Negatives)) |
| 360-Degree View Generation Success Rate | >95% | (Views generated without fatal errors ÷ Total view requests) × 100 |
| AI Conversational Intake p95 Latency | <5 seconds | Automated latency tracking in production logs |
| Structured Output Schema Validity | >99% | JSON schema validation pass rate on AI extraction outputs |

### 9.3 Technical Performance Metrics

| Metric | Target | Measurement Method |
|--------|--------|--------------------|
| API Response Time (p95) | <2 seconds | Application Insights distributed tracing |
| SignalR Update Latency (p95) | <500 milliseconds | Client-side timestamp logging |
| OCR Pipeline Processing Time (20-page PDF) | <5 minutes | Background job execution time logs |
| Dashboard Render Time | <3 seconds | Lighthouse performance score >90 |
| Database Load Reduction via Caching | >60% | Redis hit rate: (Cache hits ÷ Total requests) × 100 |

### 9.4 Security & Compliance Metrics

| Metric | Target | Measurement Method |
|--------|--------|--------------------|
| OWASP Top 10 Vulnerabilities | 0 critical/high findings | OWASP ZAP automated scan + manual penetration testing |
| PHI Data Breaches | 0 incidents | Manual log review + audit log analysis |
| Audit Log Coverage | 100% of PHI-touching actions | Automated audit event coverage testing |
| Failed Login Rate (brute force detection) | <1% of total logins | Rate limiting effectiveness: (Blocked requests ÷ Total auth requests) × 100 |

---

## 10. Governance & Communication

### 10.1 Sprint Structure

- **Sprint Duration:** 2 weeks
- **Total Sprints:** 12 sprints across 24 weeks
- **Sprint Ceremonies:**
  - Sprint Planning: Monday 9:00 AM (2 hours)
  - Daily Standup: Every day 9:30 AM (15 minutes)
  - Sprint Review: Friday 2:00 PM (1 hour) on Sprint end week
  - Sprint Retrospective: Friday 3:00 PM (1 hour) on Sprint end week
  - Backlog Refinement: Wednesday 2:00 PM (1 hour) mid-sprint

### 10.2 Stakeholder Reporting

| Stakeholder | Report Type | Frequency | Format |
|-------------|-------------|-----------|--------|
| Product Owner | Sprint Progress | Bi-weekly (Sprint Review) | Live demo + metrics dashboard |
| Executive Sponsor | Project Status | Monthly | Executive summary (1-page) + milestone tracker |
| Development Team | Technical Sync | Weekly | Architecture decisions, code review findings, blockers |
| QA/Security Team | Test Results | Bi-weekly | Test coverage report, OWASP scan results |

### 10.3 Decision-Making Framework

| Decision Type | Authority | Escalation Path |
|---------------|-----------|-----------------|
| Technical Architecture | Tech Lead | Product Owner → Executive Sponsor |
| Scope Changes (>1 week impact) | Product Owner | Executive Sponsor |
| Resource Allocation | Project Manager | Product Owner |
| Security/Compliance Exceptions | Tech Lead + HIPAA Compliance Officer | Legal + Executive Sponsor |
| AI Model Selection | AI/ML Engineer + Tech Lead | Product Owner (if cost/timeline impact) |

### 10.4 Change Control Process

1. **Request Submission:** Requester submits change request form (business justification, effort estimate, risk assessment)
2. **Impact Analysis:** Tech Lead evaluates technical feasibility, PM evaluates timeline/cost impact
3. **Approval:** Product Owner approves/rejects based on business value vs. impact
4. **Backlog Integration:** Approved changes added to product backlog, prioritized in next sprint planning
5. **Communication:** All stakeholders notified of approved changes via email + project management tool update

---

## 11. Sprint Planning Bridge

### 11.1 Epic-to-Sprint Mapping Strategy

The project will decompose into **6 Epics** aligned with the module structure:

| Epic ID | Epic Name | Story Points | Sprints Allocated | Dependency |
|---------|-----------|--------------|-------------------|------------|
| **EP-001** | Identity & Access Management | 40 SP | Sprints 1-2 | None |
| **EP-002** | Scheduling & Appointments | 120 SP | Sprints 3-6 | EP-001 |
| **EP-003** | Notifications & Calendar | 60 SP | Sprints 7-8 | EP-002 |
| **EP-004** | Patient Intake & Documents | 80 SP | Sprints 9-10 | EP-001 |
| **EP-005** | Clinical Intelligence | 366 SP (270 + 96) | Sprints 9-12 | EP-004 |
| **EP-006** | Security & Testing | 162 SP (50 + 112) | Sprints 11-12 | All Epics |

### 11.2 Sprint Capacity Planning

**Assumptions:**
- **Team Velocity:** 15 SP/week per developer
- **Sprint Duration:** 2 weeks = 30 SP per developer per sprint
- **Team Ramp-Up:** Sprint 1-2 at 80% velocity (24 SP/dev/sprint), Sprint 3+ at 100% velocity

| Sprint | Weeks | Active Developers | Capacity (SP) | Planned SP | Allocated Epics |
|--------|-------|-------------------|---------------|------------|-----------------|
| Sprint 1 | 1-2 | 4 (TL, 2 Backend, 1 Frontend) | 96 | 40 | EP-001 (Identity) |
| Sprint 2 | 3-4 | 4 | 120 | 0 | Buffer/Testing EP-001 |
| Sprint 3 | 5-6 | 4 | 120 | 60 | EP-002 (Scheduling) Part 1 |
| Sprint 4 | 7-8 | 4 | 120 | 60 | EP-002 (Scheduling) Part 2 |
| Sprint 5 | 9-10 | 5 (+AI/ML) | 150 | 60 | EP-003 (Notifications) |
| Sprint 6 | 11-12 | 5 | 150 | 60 | EP-004 (Intake) + EP-005 (Clinical) Part 1 |
| Sprint 7 | 13-14 | 5 | 150 | 135 | EP-005 (Clinical) Part 2 |
| Sprint 8 | 15-16 | 5 | 150 | 135 | EP-005 (Clinical) Part 3 |
| Sprint 9 | 17-18 | 5.5 (+0.5 QA) | 165 | 96 | EP-005 (Medical Coding) |
| Sprint 10 | 19-20 | 5.5 | 165 | 56 | EP-005 (Risk Assessment) |
| Sprint 11 | 21-22 | 5.5 | 165 | 162 | EP-006 (Security + Testing) Part 1 |
| Sprint 12 | 23-24 | 6 (+DevOps) | 180 | 162 | EP-006 (Security + Testing) Part 2, Launch Prep |

**Total Planned Capacity:** 1,635 SP  
**Total Planned Work:** 1,241 SP (including 20% buffer)  
**Reserve Capacity:** 394 SP (24% buffer for unknowns)

### 11.3 Critical Path Analysis

**Critical Path:** EP-001 → EP-002 → EP-004 → EP-005 → EP-006

- **EP-001 (Identity)** BLOCKS all modules (authentication required)
- **EP-002 (Scheduling)** BLOCKS EP-003 (reminders need appointments)
- **EP-004 (Intake & Documents)** BLOCKS EP-005 (clinical intelligence needs documents)
- **EP-005 (Clinical Intelligence)** is the longest duration (Sprints 9-12) and BLOCKS final testing

**Parallel Tracks:**
- EP-003 (Notifications) can run parallel to early stages of EP-005 (Sprints 9-10)
- Frontend development can run parallel to backend API development across all modules

**Risk Mitigation:** EP-005 (Clinical Intelligence) is the highest risk for schedule slippage due to AI accuracy tuning. Start AI prototyping in Sprint 6 (Week 11) to validate feasibility before Sprint 9 full implementation.

### 11.4 Next Steps: Create Sprint Plan

After this Project Plan is approved, execute:
```
/create-sprint-plan
```

This will generate `.propel/context/docs/sprint_plan.md` with:
- **Dependency-ordered sprint backlog** (Epic → User Story → Task allocation)
- **Sprint goals** (clear, measurable objectives per sprint)
- **Load balance assessment** (developer workload distribution)
- **Coverage report** (requirement traceability per sprint)

---

## 12. Approvals & Sign-Off

| Role | Name | Signature | Date |
|------|------|-----------|------|
| **Product Owner** | [Name] | _____________ | ______ |
| **Tech Lead** | [Name] | _____________ | ______ |
| **Project Manager** | [Name] | _____________ | ______ |
| **Executive Sponsor** | [Name] | _____________ | ______ |

---

## Appendix A: Requirements Traceability Matrix

| Requirement ID | Module | Epic | Story Points | Sprint Allocation |
|----------------|--------|------|--------------|-------------------|
| FR-001, FR-002, FR-003, FR-004 | Identity | EP-001 | 40 | Sprint 1 |
| FR-005, FR-006, FR-007 | Scheduling | EP-002 | 40 | Sprint 3 |
| FR-008 (Slot Swap) | Scheduling | EP-002 | 21 | Sprint 4 |
| FR-009 (Waitlist) | Scheduling | EP-002 | 13 | Sprint 4 |
| FR-010, FR-011, FR-012, FR-013 | Scheduling | EP-002 | 46 | Sprints 5-6 |
| FR-014 (AI Intake) | Intake | EP-004 | 30 | Sprint 9 |
| FR-015, FR-016, FR-017 | Intake | EP-004 | 20 | Sprint 9 |
| FR-018 (Insurance) | Intake | EP-004 | 10 | Sprint 9 |
| FR-019, FR-020, FR-021, FR-022 | Notifications | EP-003 | 60 | Sprints 7-8 |
| FR-023 (Document Parsing) | Clinical | EP-005 | 80 | Sprint 10 |
| FR-024 (Data Extraction) | Clinical | EP-005 | 90 | Sprint 10 |
| FR-025 (360-View) | Clinical | EP-005 | 60 | Sprint 11 |
| FR-026 (Conflict Detection) | Clinical | EP-005 | 40 | Sprint 11 |
| FR-027 (ICD-10) | Medical Coding | EP-005 | 48 | Sprint 9 |
| FR-028 (CPT) | Medical Coding | EP-005 | 48 | Sprint 9 |
| FR-029 (No-Show Risk) | Risk Assessment | EP-005 | 56 | Sprint 10 |
| FR-030, FR-031, FR-032 | Security | EP-006 | 50 | Sprint 11 |
| NFR-001 to NFR-023 | All Modules | All Epics | Distributed | All Sprints |
| AIR-001 to AIR-O04 | Clinical | EP-005 | Distributed | Sprints 9-12 |

---

**Document Status:** Draft for Review  
**Next Review Date:** [Insert Date]  
**Document Owner:** Project Manager  
**Distribution:** Product Owner, Tech Lead, Development Team, QA, Executive Sponsor

---

*End of Project Plan*
