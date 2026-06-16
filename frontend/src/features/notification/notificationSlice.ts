import { createSlice } from '@reduxjs/toolkit';

interface NotificationState {
  notifications: unknown[];
  unreadCount: number;
}

const initialState: NotificationState = {
  notifications: [],
  unreadCount: 0,
};

const notificationSlice = createSlice({
  name: 'notification',
  initialState,
  reducers: {
    incrementUnread(state) {
      state.unreadCount += 1;
    },
    resetUnread(state) {
      state.unreadCount = 0;
    },
    clearNotifications(state) {
      state.notifications = [];
      state.unreadCount = 0;
    },
  },
});

export const { incrementUnread, resetUnread, clearNotifications } =
  notificationSlice.actions;
export const notificationReducer = notificationSlice.reducer;
