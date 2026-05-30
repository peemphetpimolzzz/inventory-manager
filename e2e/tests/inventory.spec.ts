import { expect, test } from '@playwright/test';

test('dashboard shows the KPI cards', async ({ page }) => {
  await page.goto('/dashboard');
  await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
  await expect(page.getByText('Stock value')).toBeVisible();
  await expect(page.getByText('Low stock')).toBeVisible();
});

test('products list shows seeded data and search filters it', async ({ page }) => {
  await page.goto('/products');
  await expect(page.getByRole('heading', { name: 'Products' })).toBeVisible();
  await page.getByPlaceholder('Search name or SKU…').fill('Coffee');
  await expect(page.getByText('Arabica Coffee Beans 1kg')).toBeVisible();
});

test('create a product and drive it below its reorder level', async ({ page }) => {
  const suffix = Date.now().toString().slice(-6);
  const categoryName = `E2E Cat ${suffix}`;
  const sku = `E2E-${suffix}`;

  // Create a category.
  await page.goto('/categories');
  await page.getByRole('button', { name: '+ New category' }).click();
  await page.getByLabel('Name', { exact: true }).fill(categoryName);
  await page.getByRole('button', { name: 'Save' }).click();
  await expect(page.getByRole('cell', { name: categoryName })).toBeVisible();

  // Create a product in that category.
  await page.goto('/products');
  await page.getByRole('button', { name: '+ New product' }).click();
  await page.getByLabel('SKU').fill(sku);
  await page.getByLabel('Name', { exact: true }).fill(`E2E Widget ${suffix}`);
  await page.getByLabel('Category').selectOption({ label: categoryName });
  await page.getByLabel('Unit price').fill('25');
  await page.getByLabel('Initial quantity').fill('10');
  await page.getByLabel('Reorder level').fill('5');
  await page.getByRole('button', { name: 'Save' }).click();

  // The new product starts healthy.
  await page.getByPlaceholder('Search name or SKU…').fill(sku);
  const row = page.getByRole('row', { name: new RegExp(sku) });
  await expect(row).toBeVisible();
  await expect(row.getByText('OK')).toBeVisible();

  // Stock out enough to fall below the reorder level (10 - 8 = 2 <= 5).
  await row.getByRole('button', { name: 'Out', exact: true }).click();
  await page.getByLabel('Quantity', { exact: true }).fill('8');
  await page.getByRole('button', { name: 'Confirm' }).click();

  await page.getByPlaceholder('Search name or SKU…').fill(sku);
  await expect(row.getByText('Low')).toBeVisible();
});
