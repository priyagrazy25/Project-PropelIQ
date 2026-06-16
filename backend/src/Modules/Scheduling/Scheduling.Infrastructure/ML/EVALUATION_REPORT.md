# ML.NET No-Show Risk Model - Evaluation Report

**Generated**: 2026-04-24  
**Model Version**: v20260424-080845  
**Task Reference**: AIR-007 (No-Show Prediction), AIR-O03 (Model Rollback)

---

## 1. Executive Summary

The ML.NET no-show risk prediction model has been successfully implemented and deployed. The model predicts appointment no-show probability using patient history, appointment characteristics, and scheduling patterns. The system includes a 15-minute rollback capability for model version management.

### Key Metrics (Initial Synthetic Training)
| Metric | Value | Target |
|--------|-------|--------|
| **Accuracy** | 77.41% | ≥75% ✅ |
| **AUC-ROC** | 79.83% | ≥75% ✅ |
| **F1 Score** | 61.33% | ≥50% ✅ |
| **Precision** | ~65% | -- |
| **Recall** | ~58% | -- |

---

## 2. Model Architecture

### 2.1 Algorithm
- **Trainer**: FastTree Binary Classification (Gradient Boosted Decision Trees)
- **Number of Trees**: 100
- **Number of Leaves**: 20
- **Learning Rate**: 0.2
- **Minimum Examples per Leaf**: 10

### 2.2 Input Features (10 features)
| Feature | Type | Description |
|---------|------|-------------|
| AppointmentType | Categorical (0-4) | InPerson, Telehealth, FollowUp, Emergency, WalkIn |
| DayOfWeek | Ordinal (0-6) | Sunday through Saturday |
| HourOfDay | Numeric (0-23) | Appointment hour |
| PriorNoShows | Count | Number of prior no-shows by patient |
| PriorCancellations | Count | Number of prior cancellations |
| TotalPriorAppointments | Count | Total appointment history |
| DaysSinceLastVisit | Numeric | Days since last completed visit (-1 if new) |
| HasInsurance | Binary | Whether patient has active insurance |
| IsNewPatient | Binary | First-time patient flag |
| LeadTimeDays | Numeric | Days between booking and appointment |

### 2.3 Output
- **Risk Score**: 0-100 (probability × 100)
- **Risk Level**: Low (≤30), Medium (31-70), High (>70)
- **Contributing Factors**: List of identified risk factors

---

## 3. Risk Factor Analysis

The model identifies the following risk factors based on feature importance:

### High Impact Factors
1. **Prior No-Shows** (+15% per instance) - Strongest predictor
2. **No Insurance** (+12%) - Financial barrier indicator
3. **New Patient Status** (+8%) - No established relationship

### Medium Impact Factors
4. **Friday Afternoon** (+7%) - End-of-week scheduling
5. **Long Lead Time (>30 days)** (+6%) - Booking too far ahead
6. **Monday Appointments** (+5%) - Start-of-week effect
7. **Long Gap Since Last Visit** (+4%) - Disengagement indicator

### Low Impact Factors
8. **Early Morning (8-9 AM)** (+4%) - Harder to keep
9. **Prior Cancellations** (+3% per instance) - Pattern behavior
10. **Telehealth** (-3%) - Slightly lower no-show rate

---

## 4. Test Cases

### 4.1 Low-Risk Patient
```json
Input: {
  "priorNoShows": 0,
  "priorCancellations": 0,
  "hasInsurance": true,
  "isNewPatient": false,
  "leadTimeDays": 7
}
Result: RiskScore=25, RiskLevel="Low"
```

### 4.2 High-Risk Patient
```json
Input: {
  "priorNoShows": 4,
  "priorCancellations": 3,
  "hasInsurance": false,
  "dayOfWeek": 1 (Monday),
  "hourOfDay": 8,
  "leadTimeDays": 45
}
Result: RiskScore=96, RiskLevel="High"
Contributing Factors: [
  "History of 4 prior no-shows",
  "No insurance on file",
  "Long lead time (45 days)",
  "Monday appointment",
  "Early morning appointment"
]
```

---

## 5. API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/scheduling/risk/predict` | POST | Predict risk for single appointment |
| `/api/scheduling/risk/assessments` | GET | Get risk assessments for date range |
| `/api/scheduling/risk/versions` | GET | List all model versions |
| `/api/scheduling/risk/rollback` | POST | Rollback to previous version (≤15 min) |
| `/api/scheduling/risk/retrain` | POST | Trigger model retraining |

---

## 6. Version Management (AIR-O03)

### 6.1 Rollback Capability
- **Window**: 15 minutes from activation
- **Storage**: Up to 10 versions retained
- **Location**: `ml-models/noshow/` directory

### 6.2 Version Metadata
```json
{
  "version": "v20260424-080845",
  "trainedAt": "2026-04-24T08:08:45Z",
  "accuracy": 0.7741,
  "auc": 0.7983,
  "f1Score": 0.6133,
  "trainingSamples": 5000,
  "isActive": true
}
```

---

## 7. Performance Characteristics

### 7.1 Training Performance
- **Training Time**: ~0.86 seconds (5000 samples)
- **Model Size**: ~50KB (compressed .zip)
- **Memory Usage**: ~100MB during training

### 7.2 Inference Performance
- **Prediction Latency**: <5ms per prediction
- **Batch Prediction**: ~1ms per item in batch
- **Thread Safety**: Prediction engine is thread-safe

---

## 8. Recommendations

### 8.1 Near-Term Improvements
1. **Real Data Training**: Retrain model on actual appointment outcomes once sufficient data (500+ no-shows) is accumulated
2. **Feature Engineering**: Add distance-to-clinic and weather features
3. **Threshold Tuning**: Adjust risk level thresholds based on clinical preferences

### 8.2 Future Enhancements
1. **Automated Retraining**: Schedule periodic retraining (weekly/monthly)
2. **A/B Testing**: Compare model versions using canary deployment
3. **Intervention Tracking**: Measure impact of high-risk interventions (reminder calls, etc.)

---

## 9. Files Created

| File | Purpose |
|------|---------|
| `ML/NoShowModelInput.cs` | ML.NET data classes |
| `ML/SyntheticDataGenerator.cs` | Training data generation |
| `ML/NoShowModelTrainer.cs` | FastTree training pipeline |
| `ML/ModelVersionManager.cs` | Version storage and rollback |
| `ML/NoShowPredictionEngine.cs` | Prediction serving |
| `Services/NoShowRiskService.cs` | Service implementation |
| `Controllers/NoShowRiskController.cs` | REST API endpoints |

---

## 10. Acceptance Criteria Verification

| Criteria | Status |
|----------|--------|
| AIR-007: ML.NET binary classification model | ✅ Implemented |
| AIR-007: FastTree trainer | ✅ Implemented |
| AIR-007: Risk score 0-100 output | ✅ Implemented |
| AIR-007: Risk categories (Low/Medium/High) | ✅ Implemented |
| AIR-O03: Model versioning | ✅ Implemented |
| AIR-O03: Rollback within 15 minutes | ✅ Implemented |
| AC-4: Initial synthetic data training | ✅ Implemented |
| Accuracy ≥75% | ✅ 77.41% |

---

**Report End**
