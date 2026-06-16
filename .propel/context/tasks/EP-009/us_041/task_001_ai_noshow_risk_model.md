---
post_title: "TASK_001 - No-Show Risk Model Training & Deployment"
author1: "AI Senior Developer"
post_slug: "task-001-ai-noshow-risk-model"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-009, US_041, AI, ML.NET, classification, no-show, risk-model"
ai_note: "Generated with AI assistance from user story US_041"
summary: "Train and deploy ML.NET classification model for no-show risk scoring with version management and 15-minute rollback."
post_date: "2026-04-16"
---

# Task - TASK_001_AI_NOSHOW_RISK_MODEL

## Requirement Reference
- User Story: us_041
- Story Location: .propel/context/tasks/EP-009/us_041/us_041.md
- Acceptance Criteria:
    - AC-1: ML.NET trains on patient history, appointment type, time slot, no-show patterns per AIR-007
    - AC-2: Model registered and serves risk predictions per TR-009
    - AC-3: Version rollback within 15 minutes per AIR-O03
    - AC-4: Synthetic data for initial training
    - AC-5: Rollback with no service interruption

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | No |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| AI-ML | ML.NET | 3.x |
| Backend | ASP.NET Core | 8.0 LTS |
| Database | SQL Server Express | 2022 |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | Yes |
| **AIR Requirements** | AIR-007, AIR-O03 |
| **AI Pattern** | ML.NET binary classification for no-show risk |
| **Prompt Template Path** | N/A (ML pipeline) |
| **Guardrails Config** | Risk score 0-100; rollback ≤ 15min |
| **Model Provider** | ML.NET 3.x |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |

## Task Overview
Build the ML.NET classification pipeline: define features (patient history, appointment type, time slot, prior no-shows), train on synthetic data initially, register model versions with side-by-side storage, expose prediction endpoint, and support 15-minute rollback.

## Dependent Tasks
- task_001_db_identity_scheduling_entities (US_008) — Requires Appointment history
- task_001_db_backup_seed_data (US_011) — Synthetic data for initial training

## Impacted Components
- NEW: NoShowModelTrainer — ML.NET training pipeline
- NEW: NoShowPredictionEngine — Model serving
- NEW: ModelVersionManager — Version storage and rollback
- NEW: NoShowRiskController — Prediction endpoint

## Implementation Plan
1. Define feature set: patient demographics, appointment type, time slot, prior no-shows
2. Create synthetic training data generator for initial launch
3. Build ML.NET binary classification pipeline (FastTree trainer)
4. Implement ModelVersionManager storing model artifacts with version metadata
5. Create NoShowPredictionEngine loading active model version
6. Build prediction endpoint POST /api/clinical/risk/predict
7. Implement rollback endpoint switching to previous model version
8. Add periodic retraining trigger on accumulated real data

## Current Project State
```
[PLACEHOLDER - Updated after US_008, US_011 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | src/Modules/Scheduling/Scheduling.Infrastructure/ML/NoShowModelTrainer.cs | Training pipeline |
| CREATE | src/Modules/Scheduling/Scheduling.Infrastructure/ML/NoShowPredictionEngine.cs | Model serving |
| CREATE | src/Modules/Scheduling/Scheduling.Infrastructure/ML/ModelVersionManager.cs | Version management |
| CREATE | src/Modules/Scheduling/Scheduling.API/Controllers/NoShowRiskController.cs | Prediction endpoint |
| CREATE | src/Modules/Scheduling/Scheduling.Infrastructure/ML/SyntheticDataGenerator.cs | Initial training data |
| CREATE | src/Modules/Scheduling/Scheduling.Infrastructure/ML/NoShowModelInput.cs | ML.NET input/output types |
| CREATE | src/Modules/Scheduling/Scheduling.Infrastructure/Services/NoShowRiskService.cs | Risk service implementation |

## External References
- ML.NET: https://learn.microsoft.com/dotnet/machine-learning/
- ML.NET Binary Classification: https://learn.microsoft.com/dotnet/machine-learning/tutorials/sentiment-analysis

## Build Commands
- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy
- [x] ML.NET model trains on feature set
- [x] Prediction returns risk score 0-100
- [x] Model version stored with metadata
- [x] Rollback switches to previous version within 15 minutes
- [x] Synthetic data suffices for initial training

## Implementation Checklist
- [x] Define feature set for no-show prediction
- [x] Create synthetic training data generator
- [x] Build ML.NET FastTree classification pipeline
- [x] Implement ModelVersionManager with version storage
- [x] Create NoShowPredictionEngine loading active model
- [x] Build prediction endpoint returning risk score 0-100
- [x] Implement rollback endpoint with ≤15-minute switch
- [x] Add periodic retraining trigger for real data
