import { test, expect } from '@playwright/test';
import { uniqueName } from './helpers';

// Course 1 "Intro to C# Programming" is free and has 7 lessons.

test.describe('enrollment and progress', () => {
  test('enrolling in a free course and completing lessons shows up in My Progress', async ({ page }) => {
    const name = uniqueName();
    await page.goto('/courses/1');
    await page
      .locator('form')
      .filter({ has: page.getByRole('heading', { name: 'Enroll', exact: true }) })
      .getByPlaceholder('Your name')
      .fill(name);
    await page.getByRole('button', { name: 'Enroll', exact: true }).click();
    await expect(page.getByText('You are enrolled!')).toBeVisible();

    await page.getByRole('button', { name: 'Mark complete' }).first().click();
    await expect(page.getByText('Lesson marked complete.')).toBeVisible();
    await page.getByRole('button', { name: 'Mark complete' }).first().click();
    await expect(page.getByText('Completed', { exact: true })).toHaveCount(2);

    await page.getByRole('link', { name: 'My Progress', exact: true }).click();
    await page.getByPlaceholder('Your name').fill(name);
    await page.getByRole('button', { name: 'Show my progress' }).click();

    await expect(page.getByRole('link', { name: 'Intro to C# Programming' })).toBeVisible();
    await expect(page.getByText(/2 of 7 lessons/)).toBeVisible();
  });
});
