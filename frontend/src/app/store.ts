import { configureStore } from '@reduxjs/toolkit';
import { identityReducer } from '../features/identity/identitySlice';
import { schedulingReducer } from '../features/scheduling/schedulingSlice';
import { clinicalReducer } from '../features/clinical/clinicalSlice';
import { notificationReducer } from '../features/notification/notificationSlice';

export const store = configureStore({
  reducer: {
    identity: identityReducer,
    scheduling: schedulingReducer,
    clinical: clinicalReducer,
    notification: notificationReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
