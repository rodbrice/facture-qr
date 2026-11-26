import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ApiError, type NewLine } from '../api'
import { LineForm } from './LineForm'

async function fill(description: string, quantity: string, unitPrice: string) {
  const user = userEvent.setup()
  await user.type(screen.getByLabelText(/Description/), description)
  await user.clear(screen.getByLabelText(/Quantité/))
  await user.type(screen.getByLabelText(/Quantité/), quantity)
  await user.type(screen.getByLabelText(/Prix unitaire/), unitPrice)
  await user.click(screen.getByRole('button', { name: 'Ajouter la ligne' }))
}

describe('LineForm', () => {
  it('sends the typed numbers, a comma read as the decimal separator, then empties the form', async () => {
    const onAdd = vi.fn<(line: NewLine) => Promise<void>>().mockResolvedValue()
    render(<LineForm onAdd={onAdd} />)

    await fill('Développement', '7,5', '120')

    expect(onAdd).toHaveBeenCalledWith({ description: 'Développement', quantity: 7.5, unitPrice: 120 })
    expect(screen.getByLabelText(/Description/)).toHaveValue('')
    expect(screen.getByLabelText(/Description/)).toHaveFocus()
  })

  it('does not send a quantity that is not a number and says so next to the field', async () => {
    const onAdd = vi.fn<(line: NewLine) => Promise<void>>()
    render(<LineForm onAdd={onAdd} />)

    await fill('Analyse', 'deux', '150')

    expect(onAdd).not.toHaveBeenCalled()
    expect(screen.getByLabelText(/Quantité/)).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByLabelText(/Quantité/)).toHaveAccessibleDescription(/Saisissez un nombre/)
    expect(screen.getByLabelText(/Quantité/)).toHaveFocus()
  })

  it('announces the API refusal of the line and keeps what was typed', async () => {
    const onAdd = vi.fn<(line: NewLine) => Promise<void>>().mockRejectedValue(
      new ApiError(400, 'One or more validation errors occurred.', { line: ['The quantity must be greater than 0.'] }),
    )
    render(<LineForm onAdd={onAdd} />)

    await fill('Analyse', '0', '150')

    expect(screen.getByRole('alert')).toHaveTextContent('The quantity must be greater than 0.')
    expect(screen.getByLabelText(/Description/)).toHaveValue('Analyse')
  })

  it('announces a 409 when the draft can no longer take the line', async () => {
    const onAdd = vi.fn<(line: NewLine) => Promise<void>>().mockRejectedValue(new ApiError(409, 'The draft is full: this line would not fit on the invoice page.'))
    render(<LineForm onAdd={onAdd} />)

    await fill('Analyse', '1', '150')

    expect(screen.getByRole('alert')).toHaveTextContent('The draft is full')
  })
})
