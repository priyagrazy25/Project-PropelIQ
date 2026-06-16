---
post_title: "TASK_003 - AI Conversational Engine"
author1: "AI Senior Developer"
post_slug: "task-003-ai-conversational-engine"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-004, US_024, AI, Semantic Kernel, Ollama, Phi-3, intake"
ai_note: "Generated with AI assistance from user story US_024"
summary: "Implement AI conversational engine using Semantic Kernel + Ollama (Phi-3-mini) with structured output parsing, confidence scoring, and circuit breaker."
post_date: "2026-04-16"
---

# Task - TASK_003_AI_CONVERSATIONAL_ENGINE

## Requirement Reference

- User Story: us_024
- Story Location: .propel/context/tasks/EP-004/us_024/us_024.md
- Acceptance Criteria:
  - AC-1: AI guided flow via Semantic Kernel + Ollama (Phi-3-mini) per AIR-003
  - AC-2: Structured data extracted with confidence scores within 5s p95 per AIR-Q02
  - AC-3: Confidence < 0.5 for 3 consecutive → suggest manual form per AIR-008
- Edge Cases:
  - Non-English input → English-only response
  - Token budget exhaustion mid-conversation → graceful summary

## Design References (Frontend Tasks Only)

| Reference Type | Value |
| -------------- | ----- |
| **UI Impact**  | No    |

## Applicable Technology Stack

| Layer            | Technology      | Version    |
| ---------------- | --------------- | ---------- |
| AI-LLM           | Ollama          | 0.3.x      |
| AI-LLM Model     | Phi-3-mini      | 3.8B       |
| AI-Orchestration | Semantic Kernel | 1.x (.NET) |
| Backend          | ASP.NET Core    | 8.0 LTS    |

## AI References (AI Tasks Only)

| Reference Type           | Value                                                              |
| ------------------------ | ------------------------------------------------------------------ |
| **AI Impact**            | Yes                                                                |
| **AIR Requirements**     | AIR-003, AIR-Q02, AIR-008                                          |
| **AI Pattern**           | Conversational intake with structured output parsing               |
| **Prompt Template Path** | .propel/context/prompts/intake-conversation.txt (to be created)    |
| **Guardrails Config**    | Token budget: 4096 input / 8192 output; temperature 0.3; top-p 0.9 |
| **Model Provider**       | Ollama (Phi-3-mini 3.8B)                                           |

## Mobile References (Mobile Tasks Only)

| Reference Type    | Value |
| ----------------- | ----- |
| **Mobile Impact** | No    |

## Task Overview

Build the conversational engine using Semantic Kernel with OllamaConnector to Phi-3-mini. Design prompt template for medical intake flow. Parse AI responses into structured fields (history, symptoms, allergies, medications) with confidence scoring. Implement circuit breaker via Polly, low-confidence fallback counter, and token budget tracking.

## Dependent Tasks

- task_001_ai_runtime_orchestration_setup (US_005) — Requires Semantic Kernel + Ollama infrastructure

## Impacted Components

- NEW: IConversationalIntakeEngine, ConversationalIntakeEngine
- NEW: IntakePromptTemplate — Semantic Kernel prompt
- NEW: StructuredFieldParser — Parse AI response to typed fields
- NEW: ConfidenceScorer — Score extraction confidence

## Implementation Plan

1. Create intake prompt template with system instructions for medical history collection
2. Implement ConversationalIntakeEngine using Semantic Kernel ChatCompletionService
3. Build StructuredFieldParser extracting typed fields from AI response JSON
4. Implement ConfidenceScorer computing per-field extraction confidence
5. Add token budget tracking (4096 input / 8192 output) with graceful cutoff
6. Implement low-confidence counter (3 consecutive < 0.5 → fallback signal)
7. Add Polly circuit breaker for Ollama connectivity failures
8. Enforce English-only response guard in system prompt

## Current Project State

```
[PLACEHOLDER - Updated after US_005 tasks]
```

## Expected Changes

| Action | File Path                                              | Description           |
| ------ | ------------------------------------------------------ | --------------------- |
| CREATE | src/Modules/Clinical/AI/IConversationalIntakeEngine.cs | Engine interface      |
| CREATE | src/Modules/Clinical/AI/ConversationalIntakeEngine.cs  | Engine implementation |
| CREATE | src/Modules/Clinical/AI/IntakePromptTemplate.cs        | SK prompt template    |
| CREATE | src/Modules/Clinical/AI/StructuredFieldParser.cs       | Response parser       |
| CREATE | src/Modules/Clinical/AI/ConfidenceScorer.cs            | Confidence scoring    |
| MODIFY | src/Modules/Clinical/DependencyInjection.cs            | Register AI services  |

## External References

- Semantic Kernel prompt templates: https://learn.microsoft.com/semantic-kernel/concepts/prompts
- Ollama API: https://github.com/ollama/ollama/blob/main/docs/api.md

## Build Commands

- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy

- [x] AI generates contextual medical questions
- [x] Response parsed into structured fields with confidence scores
- [x] Response latency < 5s at p95
- [x] 3 consecutive low-confidence exchanges return fallback signal
- [x] Circuit breaker opens on Ollama failure

## Implementation Checklist

- [x] Create intake prompt template with medical history collection instructions
- [x] Implement ConversationalIntakeEngine with Semantic Kernel ChatCompletion
- [x] Build StructuredFieldParser extracting typed fields from AI JSON response
- [x] Implement ConfidenceScorer with per-field confidence computation
- [x] Add token budget tracking with graceful cutoff at limits
- [x] Implement low-confidence fallback counter (3 × < 0.5)
- [x] Add Polly circuit breaker for Ollama connectivity
- [x] Enforce English-only guard in system prompt
