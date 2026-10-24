import { test, expect } from '@playwright/test';
import { uniqueName } from './helpers';

// Course 10 "React Fundamentals" costs $49.00. Coupon WELCOME10 takes 10% off.

test.describe('checkout', () => {
  test('a coupon is applied and the cart total matches the paid amount @smoke', async ({ page }) => {
    const name = uniqueName();

    // Saving the name on My Courses lets the Courses page add to cart.
    await page.goto('/my-courses');
    await page.getByPlaceholder('Your name').fill(name);
    await page.getByRole('button', { name: 'Show my courses' }).click();

    await page.getByRole('link', { name: 'Courses', exact: true }).click();
    await page
      .locator('a.card')
      .filter({ hasText: 'React Fundamentals' })
      .getByRole('button', { name: 'Add to cart' })
      .click();
    await expect(page.getByText('Added to your cart.')).toBeVisible();

    await page.getByRole('link', { name: 'Cart', exact: true }).click();
    await page.getByPlaceholder('Coupon code').fill('WELCOME10');
    await page.getByRole('button', { name: 'Show my cart' }).click();

    const tfoot = page.locator('table.cart-table tfoot');
    const row = (label: string) => tfoot.getByRole('row').filter({ has: page.getByText(label, { exact: true }) });
    await expect(row('Subtotal')).toContainText('$49.00');
    await expect(row('Coupon discount')).toContainText('-$4.90');
    await expect(row('Total')).toContainText('$44.10');
    await expect(page.getByRole('button', { name: 'Pay $44.10' })).toBeVisible();

    await page.getByRole('button', { name: 'Pay $44.10' }).click();
    await expect(page.getByText(/Thank you! Order \d+ is paid \(\$44\.10\)\. You are enrolled\./)).toBeVisible();

    // The purchase enrolls the student in the course.
    await page.getByRole('link', { name: 'My Courses', exact: true }).click();
    await page.getByRole('button', { name: 'Show my courses' }).click();
    await expect(page.getByRole('link', { name: 'React Fundamentals' })).toBeVisible();
  });
});
