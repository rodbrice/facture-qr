import { render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it } from 'vitest'
import type { InvoiceSummary } from '../api'
import { json, stubFetch } from '../test-utils'
import { InvoicesPage } from './InvoicesPage'

const issued: InvoiceSummary = {
  id: 'i1',
  number: '2026-0001',
  status: 'Issued',
  overdue: true,
  customerId: 'c1',
  customerName: 'Simon Muster',
  total: 1200,
  issueDate: '2026-10-01',
  dueDate: '2026-10-31',
  paidOn: null,
}

describe('InvoicesPage', () => {
  it('lists each invoice with its state, the overdue flag and the total given by the API', async () => {
    stubFetch(
      json([issued, { ...issued, id: 'i2', number: null, status: 'Draft', overdue: false, total: 0, issueDate: null, dueDate: null }]),
      json([{ id: 'c1', address: { name: 'Simon Muster', street: null, buildingNumber: null, postalCode: '8000', town: 'Seldwyla', country: 'CH' } }]),
    )
    render(
      <MemoryRouter>
        <InvoicesPage />
      </MemoryRouter>,
    )

    const row = (await screen.findByRole('rowheader', { name: '2026-0001' })).closest('tr')!
    expect(within(row).getByText('Émise')).toBeInTheDocument()
    expect(within(row).getByText('En retard')).toBeInTheDocument()
    expect(within(row).getByText("1'200.00")).toBeInTheDocument()
    expect(within(row).getByText('31.10.2026')).toBeInTheDocument()

    const draft = screen.getByRole('rowheader', { name: 'Brouillon' }).closest('tr')!
    expect(within(draft).queryByText('En retard')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Client')).toBeInTheDocument()
  })
})
