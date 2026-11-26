import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ApiError, type Address } from '../api'
import { CustomerForm } from './CustomerForm'

const empty: Address = { name: '', street: null, buildingNumber: null, postalCode: '', town: '', country: 'CH' }

describe('CustomerForm', () => {
  it('labels every field and sends the trimmed address, with null for an empty street', async () => {
    const onSubmit = vi.fn<(address: Address) => Promise<void>>().mockResolvedValue()
    render(<CustomerForm initial={empty} submitLabel="Enregistrer" onSubmit={onSubmit} />)
    const user = userEvent.setup()

    await user.type(screen.getByLabelText(/Nom ou raison sociale/), ' Simon Muster ')
    await user.type(screen.getByLabelText(/NPA/), '8000')
    await user.type(screen.getByLabelText(/Localité/), 'Seldwyla')
    expect(screen.getByLabelText(/Rue ou case postale/)).toHaveValue('')
    expect(screen.getByLabelText(/Numéro/)).toHaveValue('')
    expect(screen.getByLabelText(/Pays/)).toHaveValue('CH')
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }))

    expect(onSubmit).toHaveBeenCalledWith({ name: 'Simon Muster', street: null, buildingNumber: null, postalCode: '8000', town: 'Seldwyla', country: 'CH' })
  })

  it('shows a 400 error of the API next to the field it names, and moves the focus there', async () => {
    const onSubmit = vi.fn<(address: Address) => Promise<void>>().mockRejectedValue(
      new ApiError(400, 'One or more validation errors occurred.', { country: ['The country code must be two letters (ISO 3166-1).'] }),
    )
    render(<CustomerForm initial={{ ...empty, name: 'Simon Muster', postalCode: '8000', town: 'Seldwyla', country: 'Suisse' }} submitLabel="Enregistrer" onSubmit={onSubmit} />)

    await userEvent.setup().click(screen.getByRole('button', { name: 'Enregistrer' }))

    const country = screen.getByLabelText(/Pays/)
    expect(country).toHaveAttribute('aria-invalid', 'true')
    expect(country).toHaveAccessibleDescription(/The country code must be two letters/)
    expect(country).toHaveFocus()
    expect(screen.getByLabelText(/Localité/)).not.toHaveAttribute('aria-invalid')
  })

  it('announces an error that names no field of the form in its alert', async () => {
    const onSubmit = vi.fn<(address: Address) => Promise<void>>().mockRejectedValue(new ApiError(404, 'No customer with the identifier c1.'))
    render(<CustomerForm initial={{ ...empty, name: 'Simon Muster' }} submitLabel="Enregistrer" onSubmit={onSubmit} />)

    await userEvent.setup().click(screen.getByRole('button', { name: 'Enregistrer' }))

    expect(screen.getByRole('alert')).toHaveTextContent('No customer with the identifier c1.')
    expect(screen.getByRole('alert')).toHaveFocus()
  })

  it('can be filled and submitted with the keyboard only', async () => {
    const onSubmit = vi.fn<(address: Address) => Promise<void>>().mockResolvedValue()
    render(<CustomerForm initial={empty} submitLabel="Enregistrer" onSubmit={onSubmit} />)
    const user = userEvent.setup()

    await user.tab()
    expect(screen.getByLabelText(/Nom ou raison sociale/)).toHaveFocus()
    await user.keyboard('Simon Muster{Tab}Musterstrasse{Tab}1{Tab}8000{Tab}Seldwyla{Enter}')

    expect(onSubmit).toHaveBeenCalledWith({ name: 'Simon Muster', street: 'Musterstrasse', buildingNumber: '1', postalCode: '8000', town: 'Seldwyla', country: 'CH' })
  })
})
