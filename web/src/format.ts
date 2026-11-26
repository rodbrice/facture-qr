import type { Address, InvoiceStatus } from './api'

// Amounts and dates are written as on the invoice PDF: 1'200.00 and 12.11.2026.

export function formatChf(amount: number): string {
  const [units, cents] = Math.abs(amount).toFixed(2).split('.')
  const grouped = units.replace(/\B(?=(\d{3})+(?!\d))/g, "'")
  return `${amount < 0 ? '-' : ''}${grouped}.${cents}`
}

/** An ISO date (2026-11-12) as 12.11.2026; an empty string when there is no date. */
export function formatDate(iso: string | null): string {
  if (!iso) {
    return ''
  }

  const [year, month, day] = iso.split('-')
  return `${day}.${month}.${year}`
}

export const statusLabels: Record<InvoiceStatus, string> = {
  Draft: 'Brouillon',
  Issued: 'Émise',
  Paid: 'Payée',
}

/**
 * Reads a number typed by the user: a dot or a comma as decimal separator,
 * apostrophes and spaces as thousands separators. Null when it is not a number.
 */
export function parseDecimal(text: string): number | null {
  const normalised = text.trim().replace(/['’\s]/g, '').replace(',', '.')
  return /^-?\d+(\.\d+)?$/.test(normalised) ? Number(normalised) : null
}

export function streetLine(address: Address): string {
  return [address.street, address.buildingNumber].filter(Boolean).join(' ')
}

export function townLine(address: Address): string {
  return `${address.postalCode} ${address.town}`
}
