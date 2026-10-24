import { test, expect } from '@playwright/test';
import { addPaidCourseToCart, payInCart, uniqueName } from './helpers';

// Course 11 "Git and Team Workflows" costs $19.00 and has 9 lessons.

test.describe('refunds', () => {
  test('a buyer with little progress can refund a paid order @smoke', async ({ page }) => {
    const name = uniqueName();
    await addPaidCourseToCart(page, 11, name);
    await payInCart(page, '$19.00');

    // Finish one lesson (1 of 9 is about 11%, under the 30% limit).
    await page.goto('/courses/11');
    await page.getByRole('button', { name: 'Mark complete' }).first().click();
    await expect(page.getByText('Lesson marked complete.')).toBeVisible();

    await page.getByRole('link', { name: 'My Orders', exact: true }).click();
    await page.getByRole('button', { name: 'Show my orders' }).click();
    await expect(page.locator('.order-status')).toHaveText('Paid');

    await page.getByRole('button', { name: 'Refund' }).click();
    await expect(page.locator('.order-status')).toHaveText('Refunded');
    await expect(page.getByRole('button', { name: 'Refund' })).toHaveCount(0);
    await expect(page.locator('.message.error')).toHaveCount(0);
  });

  test('a refund is refused once the student has finished 30% of a course @smoke', async ({ page, request }) => {
    const name = uniqueName();
    await addPaidCourseToCart(page, 11, name);
    await payInCart(page, '$19.00');

    // Finish 3 of 9 lessons (33%) through the API to keep the setup short.
    const course = await (await request.get('/api/courses/11')).json();
    for (const lesson of course.lessons.slice(0, 3)) {
      const res = await request.post('/api/progress', {
        data: { studentName: name, courseId: 11, lessonId: lesson.id },
      });
      expect(res.ok()).toBeTruthy();
    }

    await page.getByRole('link', { name: 'My Orders', exact: true }).click();
    await page.getByRole('button', { name: 'Show my orders' }).click();
    await page.getByRole('button', { name: 'Refund' }).click();

    await expect(page.locator('.message.error')).toContainText(
      'You have finished 33% of Git and Team Workflows. Refunds need progress under 30% on every course.',
    );
    await expect(page.locator('.order-status')).toHaveText('Paid');
    await expect(page.getByRole('button', { name: 'Refund' })).toHaveCount(1);
  });

  test('a seeded order older than 7 days cannot be refunded @smoke', async ({ page }) => {
    // Seeded order 5 (student "Elena Tanaka") was placed 2025-10-04, more than 7 days before 2026-10-01,
    // and her progress on its course is under 30%, so the age rule is what blocks the refund.
    await page.goto('/orders');
    await page.getByPlaceholder('Your name').fill('Elena Tanaka');
    await page.getByRole('button', { name: 'Show my orders' }).click();

    const order = page.locator('.panel').filter({ has: page.getByText('Order 5', { exact: true }) });
    await expect(order).toHaveCount(1);
    await order.getByRole('button', { name: 'Refund' }).click();

    await expect(order.locator('.message.error')).toHaveText(
      'Refunds are only possible within 7 days of the order.',
    );
    await expect(order.locator('.order-status')).toHaveText('Paid');
  });
});
