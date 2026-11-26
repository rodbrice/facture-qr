import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router'
import { describe, expect, it } from 'vitest'
import type { Invoice } from '../api'
import { json, problem, stubFetch } from '../test-utils'
import { InvoicePage } from './InvoicePage'

const draft: Invoice = {
  id: 'i1',
  number: null,
  status: 'Draft',
  overdue: false,
  customerId: 'c1',
  billTo: { name: 'Simon Muster', street: null, buildingNumber: null, postalCode: '8000', town: 'Seldwyla', country: 'CH' },
  lines: [
    { id: 'l1', position: 1, description: 'Analyse', quantity: 2, unitPrice: 100, amount: 200 },
    { id: 'l2', position: 2, description: 'Développement', quantity: 1, unitPrice: 50, amount: 50 },
  ],
  total: 250,
  issueDate: null,
  dueDate: null,
  paidOn: null,
}

const missingLine = () => problem(404, { title: 'Not Found', detail: 'The invoice i1 has no line l1.' })

function renderPage() {
  render(
    <MemoryRouter initialEntries={['/factures/i1']}>
      <Routes>
        <Route path="/factures/:id" element={<InvoicePage />} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('InvoicePage', () => {
  it('sends one removal at a time: a double click removes the line once and shows no error', async () => {
    const fetchMock = stubFetch(json(draft))
    let answer: (response: Response) => void = () => {}
    fetchMock.mockReturnValueOnce(new Promise<Response>((resolve) => (answer = resolve)))
    renderPage()
    const user = userEvent.setup()

    await user.dblClick(await screen.findByRole('button', { name: 'Supprimer la ligne 1' }))

    expect(fetchMock).toHaveBeenCalledTimes(2)
    expect(fetchMock.mock.calls[1]?.[1]?.method).toBe('DELETE')
    expect(screen.getByRole('button', { name: 'Supprimer la ligne 2' })).toBeDisabled()

    answer(json({ ...draft, lines: [{ ...draft.lines[1]!, position: 1 }], total: 50 }))

    expect(await screen.findByText('Ligne 1 supprimée.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Supprimer la ligne 1' })).toBeEnabled()
    expect(screen.queryByText(/has no line/)).not.toBeInTheDocument()
  })

  it('clears a refused removal once a line is added', async () => {
    stubFetch(json(draft), missingLine(), json({ ...draft, lines: [...draft.lines, { id: 'l3', position: 3, description: 'Tests', quantity: 1, unitPrice: 80, amount: 80 }], total: 330 }))
    renderPage()
    const user = userEvent.setup()

    await user.click(await screen.findByRole('button', { name: 'Supprimer la ligne 1' }))
    expect(await screen.findByText('The invoice i1 has no line l1.')).toBeInTheDocument()

    await user.type(screen.getByLabelText(/Description/), 'Tests')
    await user.type(screen.getByLabelText(/Prix unitaire/), '80')
    await user.click(screen.getByRole('button', { name: 'Ajouter la ligne' }))

    expect(await screen.findByText('Ligne ajoutée.')).toBeInTheDocument()
    expect(screen.queryByText('The invoice i1 has no line l1.')).not.toBeInTheDocument()
  })

  it('clears a refused removal once the invoice is issued', async () => {
    stubFetch(json(draft), missingLine(), json({ ...draft, number: '2026-0001', status: 'Issued', issueDate: '2026-11-26', dueDate: '2026-12-26' }))
    renderPage()
    const user = userEvent.setup()

    await user.click(await screen.findByRole('button', { name: 'Supprimer la ligne 1' }))
    expect(await screen.findByText('The invoice i1 has no line l1.')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Émettre' }))

    expect(await screen.findByText('Facture émise sous le numéro 2026-0001.')).toBeInTheDocument()
    expect(screen.queryByText('The invoice i1 has no line l1.')).not.toBeInTheDocument()
  })
})
