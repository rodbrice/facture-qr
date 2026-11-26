import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ApiError, type IssueDates } from '../api'
import { IssueForm } from './IssueForm'
import { MarkPaidForm } from './MarkPaidForm'

describe('IssueForm', () => {
  it('leaves empty dates out, so the API uses today and 30 days later', async () => {
    const onIssue = vi.fn<(dates: IssueDates) => Promise<void>>().mockResolvedValue()
    render(<IssueForm onIssue={onIssue} />)

    await userEvent.setup().click(screen.getByRole('button', { name: 'Émettre' }))

    expect(onIssue).toHaveBeenCalledWith({ issueDate: undefined, dueDate: undefined })
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
