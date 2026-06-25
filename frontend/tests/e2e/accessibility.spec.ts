import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';

test.describe('Accessibility smoke checks', () => {
  test('login and registration pages should have no critical WCAG violations', async ({ page }) => {
    for (const path of ['/login', '/register']) {
      await page.goto(path);

      const results = await new AxeBuilder({ page })
        .withTags(['wcag2a', 'wcag2aa'])
        .analyze();

      const violations = results.violations.filter(
        (violation) => violation.impact === 'critical' || violation.impact === 'serious',
      );

      expect(
        violations,
        `${path} has accessibility violations: ${JSON.stringify(
          violations.map((v) => ({ id: v.id, impact: v.impact, nodes: v.nodes.length })),
          null,
          2,
        )}`,
      ).toHaveLength(0);
    }
  });
});
