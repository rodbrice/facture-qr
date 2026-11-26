// The calls to the HTTP API of src/FactureQr.Api. The types follow docs/api/openapi.json.
// Every error answer (ProblemDetails) becomes an ApiError.

export type InvoiceStatus = 'Draft' | 'Issued' | 'Paid'

/** A structured address as the QR-bill requires it. */
export interface Address {
  name: string
  street: string | null
  buildingNumber: string | null
  postalCode: string
  town: string
  country: string
}

export interface Customer {
  id: string
  address: Address
}

export interface Line {
  id: string
  position: number
  description: string
  quantity: number
  unitPrice: number
  amount: number
}

export interface Invoice {
  id: string
  number: string | null
  status: InvoiceStatus
  overdue: boolean
  customerId: string
  billTo: Address
  lines: Line[]
  total: number
  issueDate: string | null
  dueDate: string | null
  paidOn: string | null
}

export interface InvoiceSummary {
  id: string
  number: string | null
  status: InvoiceStatus
  overdue: boolean
  customerId: string
  customerName: string
  total: number
  issueDate: string | null
  dueDate: string | null
  paidOn: string | null
}

export interface NewLine {
  description: string
  quantity: number
  unitPrice: number
}

/** Dates are ISO dates (2026-11-12); an omitted date takes the API's default. */
export interface IssueDates {
  issueDate?: string
  dueDate?: string
}

export interface Creditor {
  address: Address
  iban: string
  isQrIban: boolean
}

/**
 * An error answered by the API, or no answer at all (status 0).
 * fieldErrors holds the `errors` of a 400, keyed by the JSON field (postalCode, lines[1], dueDate).
 */
export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: Readonly<Record<string, readonly string[]>>

  constructor(status: number, message: string, fieldErrors: Readonly<Record<string, readonly string[]>> = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

/** Reads a ProblemDetails body (RFC 9457); a body that is not one still gives an error with the status. */
export async function toApiError(response: Response): Promise<ApiError> {
  let body: unknown
  try {
    body = await response.json()
  } catch {
    body = undefined
  }

  const problem = isObject(body) ? body : {}
  const detail = typeof problem.detail === 'string' ? problem.detail : undefined
  const title = typeof problem.title === 'string' ? problem.title : undefined
  return new ApiError(response.status, detail ?? title ?? `Erreur ${response.status}`, fieldErrorsOf(problem.errors))
}

function fieldErrorsOf(errors: unknown): Record<string, string[]> {
  const result: Record<string, string[]> = {}
  if (!isObject(errors)) {
    return result
  }

  for (const [field, messages] of Object.entries(errors)) {
    if (Array.isArray(messages)) {
      result[field] = messages.filter((m): m is string => typeof m === 'string')
    }
  }

  return result
}

function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

async function send<T>(method: string, path: string, body?: unknown): Promise<T> {
  let response: Response
  try {
    response = await fetch(path, {
      method,
      headers: body === undefined ? { Accept: 'application/json' } : { Accept: 'application/json', 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch {
    throw new ApiError(0, "L'API ne répond pas. Vérifiez qu'elle est démarrée.")
  }

  if (!response.ok) {
    throw await toApiError(response)
  }

  return (await response.json()) as T
}

const id = (value: string) => encodeURIComponent(value)

export const api = {
  customers: () => send<Customer[]>('GET', '/customers'),
  customer: (customerId: string) => send<Customer>('GET', `/customers/${id(customerId)}`),
  createCustomer: (address: Address) => send<Customer>('POST', '/customers', address),
  updateCustomer: (customerId: string, address: Address) => send<Customer>('PUT', `/customers/${id(customerId)}`, address),

  invoices: () => send<InvoiceSummary[]>('GET', '/invoices'),
  invoice: (invoiceId: string) => send<Invoice>('GET', `/invoices/${id(invoiceId)}`),
  createDraft: (customerId: string) => send<Invoice>('POST', '/invoices', { customerId }),
  addLine: (invoiceId: string, line: NewLine) => send<Invoice>('POST', `/invoices/${id(invoiceId)}/lines`, line),
  removeLine: (invoiceId: string, lineId: string) => send<Invoice>('DELETE', `/invoices/${id(invoiceId)}/lines/${id(lineId)}`),
  issue: (invoiceId: string, dates: IssueDates) => send<Invoice>('POST', `/invoices/${id(invoiceId)}/issue`, dates),
  markPaid: (invoiceId: string, paidOn?: string) => send<Invoice>('POST', `/invoices/${id(invoiceId)}/mark-paid`, { paidOn }),
  pdfUrl: (invoiceId: string) => `/invoices/${id(invoiceId)}/pdf`,

  creditor: () => send<Creditor>('GET', '/settings/creditor'),
}
