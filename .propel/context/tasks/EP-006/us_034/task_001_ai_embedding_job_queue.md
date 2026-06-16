---
post_title: "TASK_001 - Document Embedding & Job Queue"
author1: "AI Senior Developer"
post_slug: "task-001-ai-embedding-job-queue"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-006, US_034, AI, embedding, vector-storage, job-queue, circuit-breaker"
ai_note: "Generated with AI assistance from user story US_034"
summary: "Implement document embedding storage in SQL Server vector table, sequential job queue with max 2 concurrent pipelines, and circuit breaker."
post_date: "2026-04-16"
---

# Task - TASK_001_AI_EMBEDDING_JOB_QUEUE

## Requirement Reference
- User Story: us_034
- Story Location: .propel/context/tasks/EP-006/us_034/us_034.md
- Acceptance Criteria:
    - AC-1: Embeddings stored in SQL Server vector table with cosine similarity per AIR-R04
    - AC-2: Max 2 concurrent extraction pipelines per AIR-O04
    - AC-3: Circuit breaker: 3 failures / 60s → open 120s per AIR-O02
    - AC-4: 8,192 token budget per extraction request per AIR-O01
    - AC-5: Support 10,000 documents/month per NFR-016

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | No |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| AI-LLM | Ollama | 0.3.x |
| AI-LLM Model | Phi-3-mini | 3.8B |
| AI-Vector | SQL Server custom vector table | 2022 |
| Backend | ASP.NET Core | 8.0 LTS |
| Resilience | Polly | 8.x |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | Yes |
| **AIR Requirements** | AIR-R01, AIR-R04, AIR-O01, AIR-O02, AIR-O04 |
| **AI Pattern** | Document embedding for RAG retrieval |
| **Prompt Template Path** | N/A (embedding API) |
| **Guardrails Config** | 8,192 tokens/request; max 2 concurrent; circuit breaker 3/60s→120s |
| **Model Provider** | Ollama (Phi-3-mini embedding) |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |

## Task Overview
Build the embedding generation pipeline and job queue manager. Generate embeddings via Ollama, store in SQL Server custom vector table with indexed float arrays. Enforce max 2 concurrent pipelines via SemaphoreSlim. Implement circuit breaker. Track token budget per document at 8,192 max.

## Dependent Tasks
- task_001_ai_ner_clinical_extraction (US_033) — Produces extracted data for embedding
- task_001_ai_runtime_orchestration_setup (US_005) — Ollama embedding infrastructure
- task_001_db_clinical_ai_entities (US_009) — DocumentEmbedding vector table

## Impacted Components
- NEW: IEmbeddingService, EmbeddingService — Generate and store embeddings
- NEW: IExtractionJobQueue, ExtractionJobQueue — Sequential queue manager
- NEW: ExtractionJobWorker — Background worker with SemaphoreSlim(2)
- MODIFY: OcrProcessingWorker — Integrate with job queue

## Implementation Plan
1. Create ExtractionJobQueue with SemaphoreSlim(2) concurrency limit
2. Build ExtractionJobWorker as IHostedService dequeuing jobs sequentially
3. Implement EmbeddingService generating embeddings via Ollama API
4. Store embeddings in DocumentEmbedding table with indexed float arrays
5. Implement cosine similarity search method for RAG retrieval
6. Add Polly circuit breaker (3 failures / 60s → 120s recovery)
7. Enforce 8,192 token budget per document extraction request
8. Track processing metrics for 10,000 docs/month scaling

## Current Project State
```
backend/src/Modules/Clinical/
├── Clinical.Application/AI/
│   ├── IEmbeddingService.cs (new)
│   └── IExtractionJobQueue.cs (new)
├── Clinical.Infrastructure/AI/
│   ├── EmbeddingService.cs (new) - Ollama embedding with cosine similarity
│   ├── EmbeddingCircuitBreaker.cs (new) - Polly circuit breaker
│   ├── ExtractionJobQueue.cs (new) - SemaphoreSlim(2) concurrency
│   ├── ExtractionJobWorker.cs (new) - Full pipeline orchestration
│   └── ExtractionMetricsTracker.cs (new) - 10K docs/month tracking
└── Clinical.Infrastructure/ClinicalModuleExtensions.cs (updated - service registration)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | src/Modules/Clinical/Clinical.Application/AI/IEmbeddingService.cs | Service interface with cosine similarity |
| CREATE | src/Modules/Clinical/Clinical.Application/AI/IExtractionJobQueue.cs | Queue interface with job tracking |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/AI/EmbeddingService.cs | Embedding generation via Ollama |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/AI/EmbeddingCircuitBreaker.cs | Polly circuit breaker (3/60s→120s) |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/AI/ExtractionJobQueue.cs | Queue with SemaphoreSlim(2) |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/AI/ExtractionJobWorker.cs | Background worker orchestrating pipeline |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/AI/ExtractionMetricsTracker.cs | Scalability metrics tracking |
| MODIFY | src/Modules/Clinical/Clinical.Infrastructure/ClinicalModuleExtensions.cs | Service registration |

## External References
- Ollama embedding API: https://github.com/ollama/ollama/blob/main/docs/api.md#generate-embeddings
- SQL Server vector extensions: Custom float array implementation

## Build Commands
- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy
- [x] Embeddings generated and stored in vector table
- [x] Max 2 concurrent pipelines enforced via SemaphoreSlim
- [x] Circuit breaker opens on 3 failures in 60s
- [x] Cosine similarity search returns relevant results
- [x] Token budget limited to 8,192 per request

## Implementation Checklist
- [x] Create ExtractionJobQueue with SemaphoreSlim(2) concurrency
- [x] Build ExtractionJobWorker as IHostedService
- [x] Implement EmbeddingService generating embeddings via Ollama
- [x] Store embeddings in DocumentEmbedding with indexed float arrays
- [x] Implement cosine similarity search for RAG retrieval
- [x] Add Polly circuit breaker (3 failures/60s → 120s recovery)
- [x] Enforce 8,192 token budget per document extraction
- [x] Track processing metrics for scalability monitoring
