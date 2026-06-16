import { createSlice, type PayloadAction } from '@reduxjs/toolkit';

interface IdentityState {
  isAuthenticated: boolean;
  sessionChecked: boolean;
  userId: string | null;
  patientId: string | null;
  role: 'Patient' | 'Provider' | 'Admin' | 'FrontDesk' | null;
  fullName: string | null;
}

const initialState: IdentityState = {
  isAuthenticated: false,
  sessionChecked: false,
  userId: null,
  patientId: null,
  role: null,
  fullName: null,
};

const identitySlice = createSlice({
  name: 'identity',
  initialState,
  reducers: {
    loginSuccess(
      state,
      action: PayloadAction<Omit<IdentityState, 'isAuthenticated' | 'sessionChecked'>>,
    ) {
      state.isAuthenticated = true;
      state.sessionChecked = true;
      state.userId = action.payload.userId;
      state.patientId = action.payload.patientId;
      state.role = action.payload.role;
      state.fullName = action.payload.fullName;
    },
    sessionRestoreFailed(state) {
      state.sessionChecked = true;
    },
    tokenRefreshed(_state) {
      // Access token updated in authInterceptor; this action is for middleware hooks
    },
    logout(state) {
      state.isAuthenticated = false;
      state.sessionChecked = true;
      state.userId = null;
      state.patientId = null;
      state.role = null;
      state.fullName = null;
    },
  },
});

export const { loginSuccess, sessionRestoreFailed, tokenRefreshed, logout } = identitySlice.actions;
export const identityReducer = identitySlice.reducer;
