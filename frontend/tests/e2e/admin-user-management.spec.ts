/// <reference types="node" />

import { expect, test, type Page, type Response } from '@playwright/test';

const ADMIN_EMAIL = process.env.ADMIN_EMAIL ?? 'admin@upap.com';
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD ?? 'Admin@123';
const ACTION_WAIT_MS = Number(process.env.ACTION_WAIT_MS ?? '500');
const LOGIN_MAX_RETRIES = Number(process.env.LOGIN_MAX_RETRIES ?? '6');
const LOGIN_RETRY_WAIT_MS = Number(process.env.LOGIN_RETRY_WAIT_MS ?? '15000');
const LOGIN_MAX_BACKOFF_MS = Number(process.env.LOGIN_MAX_BACKOFF_MS ?? '8000');
const TEST_TIMEOUT_MS = Number(process.env.TEST_TIMEOUT_MS ?? '120000');

async function waitAfterAction(page: Page) {
  if (ACTION_WAIT_MS > 0) {
    await page.waitForTimeout(ACTION_WAIT_MS);
  }
}

function getRetryDelayMs(response: Response, attempt: number): number {
  const retryAfter = response.headers()['retry-after'];
  const retryAfterSeconds = Number(retryAfter ?? '');
  if (!Number.isNaN(retryAfterSeconds) && retryAfterSeconds > 0) {
    return Math.min(retryAfterSeconds * 1000, LOGIN_MAX_BACKOFF_MS);
  }

  return Math.min(LOGIN_RETRY_WAIT_MS * attempt, LOGIN_MAX_BACKOFF_MS);
}

async function submitLoginWithRetry(page: Page) {
  for (let attempt = 1; attempt <= LOGIN_MAX_RETRIES; attempt += 1) {
    await page.getByLabel('Email Address').fill(ADMIN_EMAIL);
    await waitAfterAction(page);
    await page.getByPlaceholder('Enter your password').fill(ADMIN_PASSWORD);
    await waitAfterAction(page);

    const [loginResponse] = await Promise.all([
      page.waitForResponse(
        (response) =>
          response.url().includes('/api/auth/login') &&
          response.request().method() === 'POST',
        { timeout: 15000 },
      ),
      page.getByRole('button', { name: 'Sign In' }).click(),
    ]);

    if (loginResponse.ok()) {
      return;
    }

    if (loginResponse.status() === 429 && attempt < LOGIN_MAX_RETRIES) {
      const waitMs = getRetryDelayMs(loginResponse, attempt);
      await page.waitForTimeout(waitMs);
      continue;
    }

    throw new Error(`Login request failed with status ${String(loginResponse.status())}`);
  }
}

test.describe('Admin User Management', () => {
  test('create user and search user flow', async ({ page }) => {
    test.setTimeout(TEST_TIMEOUT_MS);

    const unique = Date.now();
    const firstName = 'Playwright';
    const lastName = `User${unique}`;
    const email = `playwright.user.${unique}@example.com`;

    await page.goto('/login');
    await waitAfterAction(page);

    await submitLoginWithRetry(page);
    await waitAfterAction(page);

    await expect(page).toHaveURL(/\/management(\/dashboard)?/, {
      timeout: 15000,
    });
    await waitAfterAction(page);

    await page.goto('/management');
    await waitAfterAction(page);
    await expect(page.getByRole('heading', { name: 'User Management' })).toBeVisible();
    await waitAfterAction(page);

    await page.getByRole('button', { name: 'Create User' }).click();
    await waitAfterAction(page);
    await expect(page.getByRole('dialog', { name: 'Create User' })).toBeVisible();
    await waitAfterAction(page);

    await page.getByLabel('First Name *').fill(firstName);
    await waitAfterAction(page);
    await page.getByLabel('Last Name *').fill(lastName);
    await waitAfterAction(page);
    await page.getByLabel('Email *').fill(email);
    await waitAfterAction(page);
    await page.getByLabel('Role *').selectOption('Provider');
    await waitAfterAction(page);
    await page.getByRole('button', { name: 'Create User' }).click();
    await waitAfterAction(page);

    await expect(page.getByRole('dialog', { name: 'Create User' })).toBeHidden();
    await waitAfterAction(page);

    const searchBox = page.getByPlaceholder('Search by name or email...');
    await searchBox.fill(email);
    await waitAfterAction(page);

    await expect(page.getByRole('cell', { name: email })).toBeVisible();
    await expect(page.getByRole('cell', { name: `${firstName} ${lastName}` })).toBeVisible();
    await waitAfterAction(page);

    await searchBox.fill(firstName);
    await waitAfterAction(page);
    await expect(page.getByRole('cell', { name: `${firstName} ${lastName}` })).toBeVisible();
    await waitAfterAction(page);
  });
});
