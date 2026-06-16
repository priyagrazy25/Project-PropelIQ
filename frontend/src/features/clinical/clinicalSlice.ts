import { createSlice } from '@reduxjs/toolkit';

export type IntakeMode = 'ai' | 'manual';

export interface IntakeDraft {
  allergies: {
    name: string;
    type: string;
    severity: string;
    reaction: string;
  };
  medications: {
    name: string;
    dosage: string;
    notes: string;
  };
  history: {
    conditions: string[];
    additionalNotes: string;
  };
  symptoms: {
    chiefComplaint: string;
    duration: string;
    severity: string;
  };
}

interface ClinicalState {
  documents: unknown[];
  patientView: unknown;
  isProcessing: boolean;
  error: string | null;
  /** Shared intake draft for AI ↔ Manual mode switching (UXR-103). */
  intakeDraft: IntakeDraft | null;
  /** Current intake mode (AC-1, AC-2). */
  intakeMode: IntakeMode;
  /** Whether AI service is available (AC-4, NFR-013). */
  aiAvailable: boolean;
  /** Consecutive low-confidence exchange count (AC-5, AIR-008). */
  lowConfidenceCount: number;
  /** Fields whose values were manually edited — confidence scores removed (edge case). */
  manuallyEditedFields: string[];
}

const initialState: ClinicalState = {
  documents: [],
  patientView: null,
  isProcessing: false,
  error: null,
  intakeDraft: null,
  intakeMode: 'ai',
  aiAvailable: true,
  lowConfidenceCount: 0,
  manuallyEditedFields: [],
};

const clinicalSlice = createSlice({
  name: 'clinical',
  initialState,
  reducers: {
    setProcessing(state, action: { payload: boolean }) {
      state.isProcessing = action.payload;
    },
    setError(state, action: { payload: string | null }) {
      state.error = action.payload;
    },
    setIntakeDraft(state, action: { payload: IntakeDraft }) {
      state.intakeDraft = action.payload;
    },
    clearIntakeDraft(state) {
      state.intakeDraft = null;
    },
    setIntakeMode(state, action: { payload: IntakeMode }) {
      state.intakeMode = action.payload;
    },
    setAiAvailable(state, action: { payload: boolean }) {
      state.aiAvailable = action.payload;
    },
    incrementLowConfidence(state) {
      state.lowConfidenceCount += 1;
    },
    resetLowConfidence(state) {
      state.lowConfidenceCount = 0;
    },
    markFieldManuallyEdited(state, action: { payload: string }) {
      if (!state.manuallyEditedFields.includes(action.payload)) {
        state.manuallyEditedFields.push(action.payload);
      }
    },
    clearClinicalState(state) {
      state.documents = [];
      state.patientView = null;
      state.isProcessing = false;
      state.error = null;
      state.intakeDraft = null;
      state.intakeMode = 'ai';
      state.aiAvailable = true;
      state.lowConfidenceCount = 0;
      state.manuallyEditedFields = [];
    },
  },
});

export const {
  setProcessing,
  setError,
  setIntakeDraft,
  clearIntakeDraft,
  setIntakeMode,
  setAiAvailable,
  incrementLowConfidence,
  resetLowConfidence,
  markFieldManuallyEdited,
  clearClinicalState,
} = clinicalSlice.actions;
export const clinicalReducer = clinicalSlice.reducer;
