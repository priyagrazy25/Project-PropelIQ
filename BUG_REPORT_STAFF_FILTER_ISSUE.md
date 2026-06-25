# Bug Report: Staff Role Filter Not Displaying Updated Users

## Issue Summary
When filtering by "Staff" role in the User Management page, after editing a user's role to Staff, the updated user does not appear in the filtered results.

## Steps to Reproduce
1. Navigate to Admin Portal → User Management (`/management`)
2. Select "Staff" from the Role filter dropdown
3. Observe current results (shows only Bala R with FrontDesk role)
4. Click "Edit" on a user
5. Change their role to "Staff" (displayed as Staff in the dropdown but sends FrontDesk value)
6. Click Save
7. **EXPECTED**: Updated user appears in the Staff-filtered results
8. **ACTUAL**: Updated user does not appear; only original Staff users show

## Root Cause Analysis

### 1. Role Display vs. Internal Value Mismatch
**Location**: [frontend/src/features/identity/components/UserTable.tsx:119](./frontend/src/features/identity/components/UserTable.tsx)

The table displays the raw role value from API:
```jsx
<Badge variant="outline">{user.role}</Badge>
```

This shows "FrontDesk" to users instead of the human-readable "Staff" label, causing confusion.

### 2. Correct Filter/Form Mapping
**Verified to be working correctly:**
- Filter dropdown: `<option value="FrontDesk">Staff</option>` ✓
- Create/Edit form: `<option value="FrontDesk">Staff</option>` ✓
- API sends role as "FrontDesk" ✓

### 3. Potential Issues
- **UI Cache**: The page might not be refreshing after role update
- **Role Display**: Users don't see "Staff" label in results table, they see "FrontDesk"
- **Confusion**: The term "Staff" in filter/form displays as "FrontDesk" in results

## Code Locations

### Files Involved:
1. **[frontend/src/features/identity/pages/AdminUserManagementPage.tsx](./frontend/src/features/identity/pages/AdminUserManagementPage.tsx)**
   - Line 78-93: fetchUsers with role filter
   - Line 107: setRoleFilter sends "FrontDesk" correctly

2. **[frontend/src/features/identity/components/UserTable.tsx](./frontend/src/features/identity/components/UserTable.tsx)**
   - Line 119: Displays raw `user.role` value (shows "FrontDesk" instead of "Staff")

3. **[frontend/src/features/identity/components/UserForm.tsx](./frontend/src/features/identity/components/UserForm.tsx)**
   - Line 222: Form select correctly maps "FrontDesk" value to "Staff" label

4. **[frontend/src/features/identity/api/adminApi.ts](./frontend/src/features/identity/api/adminApi.ts)**
   - Line 55-56: fetchUsers builds query with role parameter
   - Type definition: `UserRole = 'Patient' | 'Provider' | 'Admin' | 'FrontDesk'`

## Recommended Fixes

### Fix 1: Display Human-Readable Role Labels
Modify UserTable.tsx to display "Staff" instead of "FrontDesk":
```jsx
const roleDisplay = {
  'FrontDesk': 'Staff',
  'Patient': 'Patient',
  'Admin': 'Admin',
  'Provider': 'Provider'
};

<Badge variant="outline">{roleDisplay[user.role] || user.role}</Badge>
```

### Fix 2: Ensure UI Refresh After Role Change
Verify triggerReload() is called after successful update (appears correct in line 172)

### Fix 3: Add Role Consistency Validation
Add backend validation to ensure role value is always one of: `Admin | Patient | Provider | FrontDesk`

## Test Cases
- [ ] Filter by Staff role shows only FrontDesk users
- [ ] Edit user to Staff role updates immediately
- [ ] Reopen User Management page shows persisted Staff role
- [ ] Switching role filters properly updates the user list
- [ ] Display shows "Staff" label instead of "FrontDesk"

## Screenshots
- Current: Shows "FrontDesk" in badge
- Expected: Should show "Staff" in badge

## Current System State
- Total Users: 18 (5 active, 13 inactive)
- Admin credentials: admin@upap.com / Admin@123
- Backend: Running on localhost:5037
- Frontend: Running on localhost:3000
- Current user visible: Bala R (FrontDesk/Staff role, Active status)
