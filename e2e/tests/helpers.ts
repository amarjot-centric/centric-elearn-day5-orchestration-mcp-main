import { expect, type Page } from '@playwright/test';

// The app has no auth; a unique name keeps every test's student separate.
export function uniqueName(): string {
  return `e2e-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

// Adds a paid course to the cart from its detail page (this also saves the name in the browser).
export async function addPaidCourseToCart(page: Page, courseId: number, name: string) {
  await page.goto(`/courses/${courseId}`);
  // The course page also has name inputs in the reviews and Q&A forms, so scope to the purchase form.
  await page
    .locator('form')
    .filter({ has: page.getByRole('heading', { name: 'Buy this course' }) })
    .getByPlaceholder('Your name')
    .fill(name);
  await page.getByRole('button', { name: 'Add to cart' }).click();
  await expect(page.getByText('Added to your cart.')).toBeVisible();
}

// Pays for everything in the cart from the Cart page and returns the order id.
export async function payInCart(page: Page, totalText: string): Promise<number> {
  await page.getByRole('link', { name: 'Cart', exact: true }).click();
  await page.getByRole('button', { name: `Pay ${totalText}` }).click();
  const message = page.getByText(/Thank you! Order \d+ is paid/);
  await expect(message).toBeVisible();
  const match = (await message.textContent())?.match(/Order (\d+) is paid/);
  if (!match) throw new Error('Could not read the order id from the checkout message.');
  return Number(match[1]);
}
