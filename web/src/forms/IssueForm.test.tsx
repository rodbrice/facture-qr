import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ApiError, type IssueDates } from '../api'
import { IssueForm } from './IssueForm'
import { MarkPaidForm } from './MarkPaidForm'

/** jsdom never reports a partly typed date, so the browser's badInput flag is set by hand. */
function markPartlyTyped(input: HTMLElement) {
  Object.defineProperty(input, 'validity', { configurable: true, get: () => ({ badInput: true }) })
}

describe('IssueForm', () => {
  it('leaves empty dates out, so the API uses today and 30 days later', async () => {
    const onIssue = vi.fn<(dates: IssueDates) => Promise<void>>().mockResolvedValue()
    render(<IssueForm onIssue={onIssue} />)

    await userEvent.setup().click(screen.getByRole('button', { name: 'Émettre' }))

    expect(onIssue).toHaveBeenCalledWith({ issueDate: undefined, dueDate: undefined })
  })

  it('refuses partly typed dates instead of issuing with the default dates', async () => {
    const onIssue = vi.fn<(dates: IssueDates) => Promise<void>>().mockResolvedValue()
    render(<IssueForm onIssue={onIssue} />)
    const issueDate = screen.getByLabelText("Date d'émission")
    const dueDate = screen.getByLabelText('Échéance')
    markPartlyTyped(issueDate)
    markPartlyTyped(dueDate)

    await userEvent.setup().click(screen.getByRole('button', { name: 'Émettre' }))

    expect(onIssue).not.toHaveBeenCalled()
    expect(issueDate).toHaveAttribute('aria-invalid', 'true')
    expect(issueDate).toHaveAccessibleDescription(/Date incomplète\./)
    expect(dueDate).toHaveAccessibleDescription(/Date incomplète\./)
    expect(issueDate).toHaveFocus()
  })

  it('shows a refused due date next to the due date field', async () => {
    const onIssue = vi.fn<(dates: IssueDates) => Promise<void>>().mockRejectedValue(
      new ApiError(400, 'One or more validation errors occurred.', { dueDate: ['The due date 2026-11-01 is before the issue date 2026-11-12.'] }),
    )
    render(<IssueForm onIssue={onIssue} />)
    const user = userEvent.setup()

    await user.type(screen.getByLabelText("Date d'émission"), '2026-11-12')
    await user.type(screen.getByLabelText('Échéance'), '2026-11-01')
    await user.click(screen.getByRole('button', { name: 'Émettre' }))

    expect(onIssue).toHaveBeenCalledWith({ issueDate: '2026-11-12', dueDate: '2026-11-01' })
    expect(screen.getByLabelText('Échéance')).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByLabelText('Échéance')).toHaveAccessibleDescription(/before the issue date/)
  })

  it('announces a 409 next to the issue button', async () => {
    const onIssue = vi.fn<(dates: IssueDates) => Promise<void>>().mockRejectedValue(new ApiError(409, 'An invoice needs at least one line to be issued.'))
    render(<IssueForm onIssue={onIssue} />)

    await userEvent.setup().click(screen.getByRole('button', { name: 'Émettre' }))

    expect(screen.getByRole('alert')).toHaveTextContent('An invoice needs at least one line to be issued.')
  })
})

describe('MarkPaidForm', () => {
  it('refuses a partly typed payment date instead of recording today', async () => {
    const onMarkPaid = vi.fn<(paidOn?: string) => Promise<void>>().mockResolvedValue()
    render(<MarkPaidForm onMarkPaid={onMarkPaid} />)
    const paidOn = screen.getByLabelText('Date du paiement')
    markPartlyTyped(paidOn)

    await userEvent.setup().click(screen.getByRole('button', { name: 'Marquer comme payée' }))

    expect(onMarkPaid).not.toHaveBeenCalled()
    expect(paidOn).toHaveAttribute('aria-invalid', 'true')
    expect(paidOn).toHaveAccessibleDescription(/Date incomplète\./)
    expect(paidOn).toHaveFocus()
  })

  it('shows a refused payment date next to its field', async () => {
    const onMarkPaid = vi.fn<(paidOn?: string) => Promise<void>>().mockRejectedValue(
      new ApiError(400, 'One or more validation errors occurred.', { paidOn: ['The payment date 2030-01-01 is in the future.'] }),
    )
    render(<MarkPaidForm onMarkPaid={onMarkPaid} />)
    const user = userEvent.setup()

    await user.type(screen.getByLabelText('Date du paiement'), '2030-01-01')
    await user.click(screen.getByRole('button', { name: 'Marquer comme payée' }))

    expect(onMarkPaid).toHaveBeenCalledWith('2030-01-01')
    expect(screen.getByLabelText('Date du paiement')).toHaveAccessibleDescription(/in the future/)
  })
})
