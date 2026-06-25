# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: admin-user-management.spec.ts >> Admin User Management >> create user and search user flow
- Location: tests\e2e\admin-user-management.spec.ts:61:3

# Error details

```
Error: expect(locator).toBeVisible() failed

Locator: getByRole('cell', { name: 'Playwright User1781606729532' })
Expected: visible
Timeout: 5000ms
Error: element(s) not found

Call log:
  - Expect "toBeVisible" with timeout 5000ms
  - waiting for getByRole('cell', { name: 'Playwright User1781606729532' })

```

```yaml
- banner:
  - link "Admin Portal":
    - /url: /management
  - button "Notifications"
  - text: SA
- navigation "Admin navigation":
  - link "User Management":
    - /url: /management
  - link "Dashboard":
    - /url: /management/dashboard
  - link "Queue":
    - /url: /management/queue
  - link "Conflicts":
    - /url: /management/conflicts
  - link "Medical Codes":
    - /url: /management/codes
  - link "Risk":
    - /url: /management/risk
  - link "Audit Logs":
    - /url: /management/audit
  - link "Settings":
    - /url: /management/settings
  - separator
  - button "Sign Out"
- main:
  - heading "User Management" [level=1]
  - button "Create User"
  - textbox "Search users":
    - /placeholder: Search by name or email...
    - text: Playwright
  - combobox "Filter by role":
    - option "All Roles" [selected]
    - option "Admin"
    - option "Provider"
    - option "Front Desk"
    - option "Patient"
  - combobox "Filter by status":
    - option "All Statuses" [selected]
    - option "Active"
    - option "Inactive"
  - table:
    - rowgroup:
      - row "Name Email Role Status Last Login Actions":
        - columnheader "Name"
        - columnheader "Email"
        - columnheader "Role"
        - columnheader "Status"
        - columnheader "Last Login"
        - columnheader "Actions"
    - rowgroup:
      - row "Playwright User1781601094504 playwright.user.1781601094504@example.com Provider Active — Edit Deactivate":
        - cell "Playwright User1781601094504"
        - cell "playwright.user.1781601094504@example.com"
        - cell "Provider"
        - cell "Active"
        - cell "—"
        - cell "Edit Deactivate":
          - button "Edit"
          - button "Deactivate"
      - row "Playwright User1781601807044 playwright.user.1781601807044@example.com Provider Active — Edit Deactivate":
        - cell "Playwright User1781601807044"
        - cell "playwright.user.1781601807044@example.com"
        - cell "Provider"
        - cell "Active"
        - cell "—"
        - cell "Edit Deactivate":
          - button "Edit"
          - button "Deactivate"
      - row "Playwright User1781601920301 playwright.user.1781601920301@example.com Provider Active — Edit Deactivate":
        - cell "Playwright User1781601920301"
        - cell "playwright.user.1781601920301@example.com"
        - cell "Provider"
        - cell "Active"
        - cell "—"
        - cell "Edit Deactivate":
          - button "Edit"
          - button "Deactivate"
      - row "Playwright User1781602007624 playwright.user.1781602007624@example.com Provider Active — Edit Deactivate":
        - cell "Playwright User1781602007624"
        - cell "playwright.user.1781602007624@example.com"
        - cell "Provider"
        - cell "Active"
        - cell "—"
        - cell "Edit Deactivate":
          - button "Edit"
          - button "Deactivate"
      - row "Playwright User1781602116510 playwright.user.1781602116510@example.com Provider Active — Edit Deactivate":
        - cell "Playwright User1781602116510"
        - cell "playwright.user.1781602116510@example.com"
        - cell "Provider"
        - cell "Active"
        - cell "—"
        - cell "Edit Deactivate":
          - button "Edit"
          - button "Deactivate"
      - row "Playwright User1781602304582 playwright.user.1781602304582@example.com Provider Active — Edit Deactivate":
        - cell "Playwright User1781602304582"
        - cell "playwright.user.1781602304582@example.com"
        - cell "Provider"
        - cell "Active"
        - cell "—"
        - cell "Edit Deactivate":
          - button "Edit"
          - button "Deactivate"
      - row "Playwright User1781602940527 playwright.user.1781602940527@example.com Provider Active — Edit Deactivate":
        - cell "Playwright User1781602940527"
        - cell "playwright.user.1781602940527@example.com"
        - cell "Provider"
        - cell "Active"
        - cell "—"
        - cell "Edit Deactivate":
          - button "Edit"
          - button "Deactivate"
      - row "Playwright User1781603023010 playwright.user.1781603023010@example.com Provider Active — Edit Deactivate":
        - cell "Playwright User1781603023010"
        - cell "playwright.user.1781603023010@example.com"
        - cell "Provider"
        - cell "Active"
        - cell "—"
        - cell "Edit Deactivate":
          - button "Edit"
          - button "Deactivate"
      - row "Playwright User1781603073384 playwright.user.1781603073384@example.com Provider Active — Edit Deactivate":
        - cell "Playwright User1781603073384"
        - cell "playwright.user.1781603073384@example.com"
        - cell "Provider"
        - cell "Active"
        - cell "—"
        - cell "Edit Deactivate":
          - button "Edit"
          - button "Deactivate"
      - row "Playwright User1781603165121 playwright.user.1781603165121@example.com Provider Active — Edit Deactivate":
        - cell "Playwright User1781603165121"
        - cell "playwright.user.1781603165121@example.com"
        - cell "Provider"
        - cell "Active"
        - cell "—"
        - cell "Edit Deactivate":
          - button "Edit"
          - button "Deactivate"
  - text: Showing 1–10 of 13
  - button "Previous page" [disabled]: Prev
  - button "Next page": Next
- region "Notifications alt+T"
```

# Test source

```ts
  14  |   if (ACTION_WAIT_MS > 0) {
  15  |     await page.waitForTimeout(ACTION_WAIT_MS);
  16  |   }
  17  | }
  18  | 
  19  | function getRetryDelayMs(response: Response, attempt: number): number {
  20  |   const retryAfter = response.headers()['retry-after'];
  21  |   const retryAfterSeconds = Number(retryAfter ?? '');
  22  |   if (!Number.isNaN(retryAfterSeconds) && retryAfterSeconds > 0) {
  23  |     return Math.min(retryAfterSeconds * 1000, LOGIN_MAX_BACKOFF_MS);
  24  |   }
  25  | 
  26  |   return Math.min(LOGIN_RETRY_WAIT_MS * attempt, LOGIN_MAX_BACKOFF_MS);
  27  | }
  28  | 
  29  | async function submitLoginWithRetry(page: Page) {
  30  |   for (let attempt = 1; attempt <= LOGIN_MAX_RETRIES; attempt += 1) {
  31  |     await page.getByLabel('Email Address').fill(ADMIN_EMAIL);
  32  |     await waitAfterAction(page);
  33  |     await page.getByPlaceholder('Enter your password').fill(ADMIN_PASSWORD);
  34  |     await waitAfterAction(page);
  35  | 
  36  |     const [loginResponse] = await Promise.all([
  37  |       page.waitForResponse(
  38  |         (response) =>
  39  |           response.url().includes('/api/auth/login') &&
  40  |           response.request().method() === 'POST',
  41  |         { timeout: 15000 },
  42  |       ),
  43  |       page.getByRole('button', { name: 'Sign In' }).click(),
  44  |     ]);
  45  | 
  46  |     if (loginResponse.ok()) {
  47  |       return;
  48  |     }
  49  | 
  50  |     if (loginResponse.status() === 429 && attempt < LOGIN_MAX_RETRIES) {
  51  |       const waitMs = getRetryDelayMs(loginResponse, attempt);
  52  |       await page.waitForTimeout(waitMs);
  53  |       continue;
  54  |     }
  55  | 
  56  |     throw new Error(`Login request failed with status ${String(loginResponse.status())}`);
  57  |   }
  58  | }
  59  | 
  60  | test.describe('Admin User Management', () => {
  61  |   test('create user and search user flow', async ({ page }) => {
  62  |     test.setTimeout(TEST_TIMEOUT_MS);
  63  | 
  64  |     const unique = Date.now();
  65  |     const firstName = 'Playwright';
  66  |     const lastName = `User${unique}`;
  67  |     const email = `playwright.user.${unique}@example.com`;
  68  | 
  69  |     await page.goto('/login');
  70  |     await waitAfterAction(page);
  71  | 
  72  |     await submitLoginWithRetry(page);
  73  |     await waitAfterAction(page);
  74  | 
  75  |     await expect(page).toHaveURL(/\/management(\/dashboard)?/, {
  76  |       timeout: 15000,
  77  |     });
  78  |     await waitAfterAction(page);
  79  | 
  80  |     await page.goto('/management');
  81  |     await waitAfterAction(page);
  82  |     await expect(page.getByRole('heading', { name: 'User Management' })).toBeVisible();
  83  |     await waitAfterAction(page);
  84  | 
  85  |     await page.getByRole('button', { name: 'Create User' }).click();
  86  |     await waitAfterAction(page);
  87  |     await expect(page.getByRole('dialog', { name: 'Create User' })).toBeVisible();
  88  |     await waitAfterAction(page);
  89  | 
  90  |     await page.getByLabel('First Name *').fill(firstName);
  91  |     await waitAfterAction(page);
  92  |     await page.getByLabel('Last Name *').fill(lastName);
  93  |     await waitAfterAction(page);
  94  |     await page.getByLabel('Email *').fill(email);
  95  |     await waitAfterAction(page);
  96  |     await page.getByLabel('Role *').selectOption('Provider');
  97  |     await waitAfterAction(page);
  98  |     await page.getByRole('button', { name: 'Create User' }).click();
  99  |     await waitAfterAction(page);
  100 | 
  101 |     await expect(page.getByRole('dialog', { name: 'Create User' })).toBeHidden();
  102 |     await waitAfterAction(page);
  103 | 
  104 |     const searchBox = page.getByPlaceholder('Search by name or email...');
  105 |     await searchBox.fill(email);
  106 |     await waitAfterAction(page);
  107 | 
  108 |     await expect(page.getByRole('cell', { name: email })).toBeVisible();
  109 |     await expect(page.getByRole('cell', { name: `${firstName} ${lastName}` })).toBeVisible();
  110 |     await waitAfterAction(page);
  111 | 
  112 |     await searchBox.fill(firstName);
  113 |     await waitAfterAction(page);
> 114 |     await expect(page.getByRole('cell', { name: `${firstName} ${lastName}` })).toBeVisible();
      |                                                                                ^ Error: expect(locator).toBeVisible() failed
  115 |     await waitAfterAction(page);
  116 |   });
  117 | });
  118 | 
```