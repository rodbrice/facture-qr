import { describe, expect, it } from 'vitest'
import { ApiError } from './api'
import { formErrors } from './formErrors'

describe('formErrors', () => {
  it('places the errors of known fields next to them and the others on the form', () => {
    const error = new ApiError(400, 'One or more validation errors occurred.', {
      postalCode: ['The postal code is required.'],
      town: ['Too long.', 'Bad character.'],
      customerId: ['No customer.'],
    })

    expect(formErrors(error, ['postalCode', 'town'])).toEqual({
      fields: { postalCode: 'The postal code is required.', town: 'Too long. Bad character.' },
      form: ['No customer.'],
    })
  })

  it('puts the message of an error without field errors on the form', () => {
    const error = new ApiError(409, 'The draft is full: this line would not fit on the invoice page.')

    expect(formErrors(error, ['line'])).toEqual({ fields: {}, form: ['The draft is full: this line would not fit on the invoice page.'] })
  })

  it('reports an unexpected error on the form', () => {
    expect(formErrors(new Error('boom'), [])).toEqual({ fields: {}, form: ['Erreur inattendue : boom'] })
  })
})
