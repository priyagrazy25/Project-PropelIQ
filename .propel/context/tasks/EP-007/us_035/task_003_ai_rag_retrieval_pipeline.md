---
post_title: "TASK_003 - RAG Retrieval Pipeline"
author1: "AI Senior Developer"
post_slug: "task-003-ai-rag-retrieval-pipeline"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-007, US_035, AI, RAG, embedding, cosine-similarity, re-ranking"
ai_note: "Generated with AI assistance from user story US_035"
summary: "Implement RAG retrieval pipeline with top-5 cosine similarity, hybrid re-ranking with recency weighting, and context assembly."
post_date: "2026-04-16"
---

# Task - TASK_003_AI_RAG_RETRIEVAL_PIPELINE

## Requirement Reference
- User Story: us_035
- Story Location: .propel/context/tasks/EP-007/us_035/us_035.md
- Acceptance Criteria:
    - AC-2: Top-5 relevant chunks with cosine similarity ≥ 0.75 per AIR-R02
    - AC-3: Hybrid re-ranking: semantic similarity + recency weighting (1.2x newer) per AIR-R03

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | No |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| AI-LLM | Ollama | 0.3.x |
| AI-Orchestration | Semantic Kernel | 1.x (.NET) |
| AI-Vector | SQL Server custom vector table | 2022 |
| Backend | ASP.NET Core | 8.0 LTS |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | Yes |
| **AIR Requirements** | AIR-R02, AIR-R03 |
| **AI Pattern** | RAG retrieval with hybrid re-ranking |
| **Prompt Template Path** | .propel/context/prompts/rag-context-assembly.txt (to be created) |
| **Guardrails Config** | Top-5 retrieval; cosine ≥ 0.75; recency weight 1.2x |
| **Model Provider** | Ollama (Phi-3-mini for embedding + generation) |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |

## Task Overview
Build the RAG retrieval pipeline: generate query embedding, search DocumentEmbedding table with cosine similarity (threshold ≥ 0.75), retrieve top-5 chunks, apply hybrid re-ranking with recency weighting (1.2x for newer documents), assemble context for LLM generation.

## Dependent Tasks
- task_001_ai_embedding_job_queue (US_034) — Provides stored embeddings
- task_001_ai_runtime_orchestration_setup (US_005) — Semantic Kernel infrastructure

## Impacted Components
- NEW: IRagRetrievalService, RagRetrievalService
- NEW: HybridReranker — Semantic + recency scoring
- NEW: ContextAssembler — Build LLM prompt from retrieved chunks

## Implementation Plan
1. Create IRagRetrievalService with query method accepting search text
2. Generate query embedding via Ollama
3. Search DocumentEmbedding with cosine similarity, filter ≥ 0.75
4. Retrieve top-5 chunks sorted by similarity score
5. Implement HybridReranker with 1.2x recency weight for newer documents
6. Build ContextAssembler combining retrieved chunks into LLM prompt
7. Return ranked chunks with scores and source references

## Current Project State
```
[PLACEHOLDER - Updated after US_034, US_005 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | src/Modules/Clinical/Clinical.Application/AI/IRagRetrievalService.cs | Service interface ✅ |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/AI/RagRetrievalService.cs | RAG retrieval ✅ |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/AI/HybridReranker.cs | Re-ranking logic ✅ |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/AI/ContextAssembler.cs | Context assembly ✅ |
| MODIFY | src/Modules/Clinical/Clinical.Infrastructure/ClinicalModuleExtensions.cs | Service registration ✅ |

## External References
- Semantic Kernel RAG: https://learn.microsoft.com/semantic-kernel/concepts/vector-store-connectors

## Build Commands
- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy
- [x] Query generates embedding via Ollama (uses IEmbeddingService.FindSimilarAsync)
- [x] Cosine similarity search returns chunks ≥ 0.75 (SimilarityThreshold = 0.75)
- [x] Top-5 chunks retrieved and ranked (TopK = 5)
- [x] Recency weighting boosts newer documents by 1.2x (RecencyWeight = 1.2)
- [x] Context assembled for LLM consumption (ContextAssembler)

## Implementation Checklist
- [x] Create RagRetrievalService with query embedding generation
- [x] Implement cosine similarity search against DocumentEmbedding table
- [x] Filter results by threshold ≥ 0.75 and take top-5
- [x] Build HybridReranker with semantic + recency weighting (1.2x)
- [x] Create ContextAssembler for LLM prompt construction
- [x] Return ranked chunks with scores and source references
- [x] Handle no-results scenario with empty context
