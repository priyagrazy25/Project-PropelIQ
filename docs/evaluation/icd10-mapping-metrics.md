# ICD-10 Mapping Service Evaluation Report
**EP-008/US_038 - task_002_ai_icd10_mapping.md**  
**Date:** 2025-01-15  
**Service:** `Icd10MappingService` (AIR-004)

## 1. Implementation Summary

### Components Delivered
| Component | Location | Status |
|-----------|----------|--------|
| `IIcd10MappingService` | Clinical.Application/Abstractions/ | ✅ Complete |
| `Icd10MappingService` | Clinical.Infrastructure/AI/ | ✅ Complete |
| `MedicalCodingController` | Clinical.API/Controllers/ | ✅ Complete |
| `Icd10Code` Entity | Clinical.Domain/Entities/ | ✅ Complete |
| `Icd10SeedData` | Clinical.Infrastructure/Data/ | ✅ Complete |

### Acceptance Criteria Mapping
| AC | Requirement | Implementation |
|----|-------------|----------------|
| AC-1 | Map diagnoses from 360-view to ICD-10 codes | `MapDiagnosesAsync()` queries `ExtractedData` (Category=Diagnosis) |
| AC-2 | Return top-3 candidates ranked by confidence | `MaxCandidates = 3`, sorted by confidence descending |
| AC-3 | Flag codes below 50% confidence for review | `AllBelowThreshold` flag when all candidates < 0.5 |
| AC-4 | Integrate with NER/classification engine | Uses `IAiInferenceService` with Phi-3-mini via Ollama |

## 2. Technical Architecture

### Token Budget Compliance (AIR-O01)
```csharp
// Configured in TokenBudgetGuard
public enum AiRequestType { MedicalCoding = 4096, ... }
```

- **Budget per request:** 4,096 tokens
- **Enforcement:** `TokenBudgetGuard.Validate()` stops processing when exceeded
- **Prompt structure:** ~200 tokens system + ~100 tokens per diagnosis

### AI Classification Pipeline
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│ ExtractedData   │───▶│ BuildClassPrompt │───▶│ Ollama/Phi-3    │
│ (Diagnoses)     │    │ (NER extraction) │    │ (Classification)│
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                                       │
                       ┌──────────────────┐            │
                       │ ParseAiResponse  │◀───────────┘
                       │ (Extract terms)  │
                       └────────┬─────────┘
                                │
┌─────────────────┐    ┌────────▼─────────┐    ┌─────────────────┐
│ ICD-10 Lookup   │───▶│ MatchCandidates  │───▶│ Ranked Results  │
│ Table (28 codes)│    │ (Hybrid scoring) │    │ (Top-3 + flags) │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

## 3. Evaluation Metrics

### 3.1 Expected Performance (Design Targets)

| Metric | Target | Notes |
|--------|--------|-------|
| **Precision@1** | ≥ 0.70 | Primary candidate is correct 70%+ |
| **Precision@3** | ≥ 0.85 | Correct code in top 3 candidates 85%+ |
| **MRR** | ≥ 0.80 | Mean reciprocal rank across test set |
| **Latency** | < 2s | Per diagnosis mapping |

### 3.2 Confidence Score Calibration

The hybrid scoring system combines:
1. **AI term extraction** (~60% weight) - Semantic understanding
2. **Keyword matching** (~30% weight) - ICD-10 keyword overlap
3. **Short description boost** (+0.30) - Direct match bonus

```csharp
// Confidence thresholds
const double LowConfidenceThreshold = 0.5;  // Flag for review

// Color coding in UI (CodeCandidateCard.tsx)
confidence >= 0.7  → green (High)
0.5 <= confidence < 0.7 → amber (Medium)  
confidence < 0.5 → red (Low - requires review)
```

### 3.3 ICD-10 Lookup Table Coverage

| Category | Code Count | Common Conditions |
|----------|------------|-------------------|
| Endocrine | 8 | Diabetes (E10, E11), Hyperlipidemia (E78), Obesity (E66), Hypothyroidism (E03) |
| Circulatory | 5 | Hypertension (I10), CHF (I50), AFib (I48), CAD (I25) |
| Respiratory | 5 | Asthma (J45), COPD (J44), URI (J06) |
| Mental | 2 | Depression (F32), Anxiety (F41) |
| Musculoskeletal | 2 | Low back pain (M54), OA knee (M17) |
| Digestive | 2 | GERD (K21) |
| Genitourinary | 1 | UTI (N39) |
| Skin | 1 | Dermatitis (L30) |
| Symptoms | 2 | Cough (R05), Headache (R51) |
| **Total** | **28** | Primary care focus |

## 4. API Endpoints

### POST `/api/clinical/codes/icd10/{patientId}`
Maps patient diagnoses to ICD-10-CM codes.

**Response:**
```json
{
  "results": [
    {
      "extractedDataId": "guid",
      "sourceDiagnosis": "Type 2 diabetes with hyperglycemia",
      "candidates": [
        { "code": "E11.65", "description": "Type 2 diabetes mellitus with hyperglycemia", "confidence": 0.92 },
        { "code": "E11.9", "description": "Type 2 diabetes mellitus without complications", "confidence": 0.78 },
        { "code": "E11.8", "description": "Type 2 diabetes mellitus with unspecified complications", "confidence": 0.65 }
      ],
      "allBelowThreshold": false
    }
  ],
  "message": "Mapped 1 diagnosis(es) to ICD-10-CM codes.",
  "totalTokensUsed": 312
}
```

### GET `/api/clinical/codes/verification-queue`
Returns pending codes for staff verification (SCR-018).

### POST `/api/clinical/codes/{entryId}/verify`
Accepts or rejects a code entry (UXR-602).

## 5. Test Scenarios

### Scenario 1: Common Diabetes Mapping
**Input:** "Type 2 diabetes"  
**Expected Top Candidate:** E11.9 (confidence > 0.8)

### Scenario 2: Hypertension Mapping
**Input:** "High blood pressure"  
**Expected Top Candidate:** I10 (confidence > 0.85)

### Scenario 3: Low Confidence Scenario
**Input:** "General fatigue and malaise"  
**Expected:** All candidates flagged (`allBelowThreshold: true`)

### Scenario 4: Ambiguous Diagnosis
**Input:** "Breathing difficulty"  
**Expected:** Multiple respiratory codes (J45, J44, J06) with similar confidence

## 6. Future Enhancements

1. **Expand ICD-10 lookup table** - Add 500+ codes for specialty coverage
2. **Fine-tune confidence calibration** - Based on real-world verification data
3. **CPT code integration** - Extend service for procedure coding
4. **Audit trail** - Log all mapping decisions for compliance (HIPAA/HITECH)
5. **Feedback loop** - Use staff corrections to improve AI model

## 7. Build Verification

```
Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:05.67
```

All components compile successfully and integrate with existing:
- `TokenBudgetGuard` (AIR-O01 compliance)
- `IAiInferenceService` (Ollama/Phi-3 integration)
- `ClinicalDbContext` (EF Core persistence)
- Staff verification workflow (SCR-018)
