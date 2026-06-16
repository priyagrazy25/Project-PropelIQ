import { createSlice } from '@reduxjs/toolkit';

interface SchedulingState {
  appointments: unknown[];
  availableSlots: unknown[];
  isLoading: boolean;
  error: string | null;
}

const initialState: SchedulingState = {
  appointments: [],
  availableSlots: [],
  isLoading: false,
  error: null,
};

const schedulingSlice = createSlice({
  name: 'scheduling',
  initialState,
  reducers: {
    setLoading(state, action: { payload: boolean }) {
      state.isLoading = action.payload;
    },
    setError(state, action: { payload: string | null }) {
      state.error = action.payload;
    },
    clearSchedulingState(state) {
      state.appointments = [];
      state.availableSlots = [];
      state.isLoading = false;
      state.error = null;
    },
  },
});

export const { setLoading, setError, clearSchedulingState } =
  schedulingSlice.actions;
export const schedulingReducer = schedulingSlice.reducer;
