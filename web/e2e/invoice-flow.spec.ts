import { readFile } from 'node:fs/promises'
import { expect, test } from '@playwright/test'

// The whole v0.1 flow through the user interface, against the real API and PostgreSQL:
// create a customer, create a draft with lines, issue it, download its PDF, mark it as paid.
// The customer's name is unique per run, so the test also passes on a database that already has data.
test('a customer is created, invoiced, the invoice issued, downloaded and marked as paid', async ({ page }) => {
  const customerName = `Simon Muster ${Date.now().toString(36)}`

  await page.goto('/')
  await expect(page).toHaveURL(/\/factures$/)

  // Customer: the API's 400 is shown next to the field, then the corrected address is saved.
  await page.getByRole('navigation').getByRole('link', { name: 'Clients' }).click()
  await page.getByRole('link', { name: 'Nouveau client' }).click()
  await page.getByLabel('Nom ou raison sociale').fill(customerName)
  await page.getByLabel('Rue ou case postale').fill('Musterstrasse')
  await page.getByLabel('Numéro').fill('1')
  await page.getByLabel('NPA').fill('8000')
  await page.getByLabel('Localité').fill('Seldwyla')
  await page.getByLabel('Pays').fill('Suisse')
  await page.getByRole('button', { name: 'Enregistrer' }).click()
  await expect(page.getByLabel('Pays')).toHaveAttribute('aria-invalid', 'true')
  await expect(page.getByLabel('Pays')).toBeFocused()

  await page.getByLabel('Pays').fill('CH')
  await page.getByRole('button', { name: 'Enregistrer' }).click()
  await expect(page.getByRole('status').filter({ hasText: `Client « ${customerName} » enregistré.` })).toBeVisible()
  await expect(page.getByRole('rowheader', { name: customerName })).toBeVisible()

  // Draft with two lines; the totals are the API's.
  await page.getByRole('navigation').getByRole('link', { name: 'Factures' }).click()
  await page.getByLabel('Client').selectOption({ label: `${customerName}, 8000 Seldwyla` })
  await page.getByRole('button', { name: 'Créer un brouillon' }).click()
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Brouillon de facture')

  await page.getByLabel('Description').fill('Analyse')
  await page.getByLabel('Quantité').fill('2')
  await page.getByLabel('Prix unitaire (CHF)').fill('150')
  await page.getByRole('button', { name: 'Ajouter la ligne' }).click()
  await expect(page.getByTestId('invoice-total')).toHaveText('300.00')

  // A line the API refuses: its message is announced next to the line form.
  await page.getByLabel('Description').fill('Développement')
  await page.getByLabel('Quantité').fill('0')
  await page.getByLabel('Prix unitaire (CHF)').fill('120')
  await page.getByRole('button', { name: 'Ajouter la ligne' }).click()
  await expect(page.getByRole('alert').filter({ hasText: /quantity/i })).toBeVisible()

  await page.getByLabel('Quantité').fill('7,5')
  await page.getByRole('button', { name: 'Ajouter la ligne' }).click()
  await expect(page.getByTestId('invoice-total')).toHaveText("1'200.00")

  // Issue with the default dates.
  await page.getByRole('button', { name: 'Émettre' }).click()
  const heading = page.getByRole('heading', { level: 1 })
  await expect(heading).toHaveText(/^Facture \d{4}-\d{4}$/)
  const number = (await heading.textContent())!.replace('Facture ', '')
  await expect(page.getByText('Émise', { exact: true })).toBeVisible()

  // The PDF.
  const downloading = page.waitForEvent('download')
  await page.getByRole('link', { name: 'Télécharger le PDF' }).click()
  const download = await downloading
  expect(download.suggestedFilename()).toBe(`facture-${number}.pdf`)
  const pdf = await readFile((await download.path())!)
  expect(pdf.subarray(0, 5).toString('latin1')).toBe('%PDF-')

  // Payment.
  await page.getByRole('button', { name: 'Marquer comme payée' }).click()
  await expect(page.getByText('Payée', { exact: true })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Marquer comme payée' })).toHaveCount(0)

  // The list shows the invoice as paid.
  await page.getByRole('link', { name: '← Factures' }).click()
  const row = page.getByRole('row').filter({ has: page.getByRole('rowheader', { name: number }) })
  await expect(row).toContainText(customerName)
  await expect(row).toContainText('Payée')
  await expect(row).toContainText("1'200.00")

  // Settings: the creditor of the configuration.
  await page.getByRole('navigation').getByRole('link', { name: 'Paramètres' }).click()
  await expect(page.getByText('CH44 3199 9123 0008 8901 2')).toBeVisible()
})

test('the pages can be reached and a customer saved with the keyboard only', async ({ page }) => {
  await page.goto('/factures')

  await page.keyboard.press('Tab')
  await expect(page.getByRole('link', { name: 'Aller au contenu' })).toBeFocused()
  await page.keyboard.press('Tab')
  await page.keyboard.press('Tab')
  await expect(page.getByRole('navigation').getByRole('link', { name: 'Clients' })).toBeFocused()
  await page.keyboard.press('Enter')
  await expect(page).toHaveURL(/\/clients$/)
  await expect(page.locator('main')).toBeFocused()

  await page.keyboard.press('Tab')
  await expect(page.getByRole('link', { name: 'Nouveau client' })).toBeFocused()
  await page.keyboard.press('Enter')
  await expect(page).toHaveURL(/\/clients\/nouveau$/)
  await expect(page.locator('main')).toBeFocused()
  await page.keyboard.press('Tab')
  await page.keyboard.press('Tab')
  await expect(page.getByLabel('Nom ou raison sociale')).toBeFocused()
  await page.keyboard.type(`Pia Rutschmann ${Date.now().toString(36)}`)
  await page.keyboard.press('Tab')
  await page.keyboard.type('Grosse Marktgasse')
  await page.keyboard.press('Tab')
  await page.keyboard.type('28')
  await page.keyboard.press('Tab')
  await page.keyboard.type('9400')
  await page.keyboard.press('Tab')
  await page.keyboard.type('Rorschach')
  await page.keyboard.press('Enter')

  await expect(page.getByRole('status').filter({ hasText: 'enregistré' })).toBeVisible()
})
