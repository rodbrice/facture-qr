import { describe, expect, it, vi } from 'vitest'
import { api, ApiError, toApiError } from './api'
import { json, problem, sentBody, stubFetch } from './test-utils'

describe('toApiError', () => {
  it('keeps the field errors of a 400 validation problem', async () => {
    const error = await toApiError(
      problem(400, { title: 'One or more validation errors occurred.', errors: { postalCode: ['The postal code is required.'], 'lines[1]': ['Too precise.'] } }),
    )

    expect(error.status).toBe(400)
    expect(error.fieldErrors).toEqual({ postalCode: ['The postal code is required.'], 'lines[1]': ['Too precise.'] })
    expect(error.message).toBe('One or more validation errors occurred.')
  })

  it('uses the detail of a 409 as its message', async () => {
    const error = await toApiError(problem(409, { title: 'Conflict', detail: 'Invoice 2026-0001 is Issued: only a draft can change.' }))

    expect(error.status).toBe(409)
    expect(error.message).toBe('Invoice 2026-0001 is Issued: only a draft can change.')
    expect(error.fieldErrors).toEqual({})
  })

  it('still gives the status when the body is not JSON', async () => {
    const error = await toApiError(new Response('Internal error', { status: 500 }))

    expect(error.status).toBe(500)
    expect(error.message).toBe('Erreur 500')
  })

  it('ignores an errors member that is not an object of string lists', async () => {
    const error = await toApiError(problem(400, { title: 'Bad Request', errors: { name: 'not a list', town: ['Too long.', 42] } }))

    expect(error.fieldErrors).toEqual({ town: ['Too long.'] })
  })
})

describe('api', () => {
  it('sends JSON and returns the parsed answer', async () => {
    const fetchMock = stubFetch(json({ id: 'c1', address: { name: 'Simon Muster' } }, 201))
    const address = { name: 'Simon Muster', street: null, buildingNumber: null, postalCode: '8000', town: 'Seldwyla', country: 'CH' }

    const customer = await api.createCustomer(address)

    expect(customer.id).toBe('c1')
    expect(fetchMock.mock.calls[0][0]).toBe('/customers')
    expect(fetchMock.mock.calls[0][1]?.method).toBe('POST')
    expect(sentBody(fetchMock)).toEqual(address)
  })

  it('leaves omitted dates out of the request, so the API uses its defaults', async () => {
    const fetchMock = stubFetch(json({}), json({}))

    await api.issue('i1', { issueDate: undefined, dueDate: '2026-12-31' })
    await api.markPaid('i1')

    expect(sentBody(fetchMock, 0)).toEqual({ dueDate: '2026-12-31' })
    expect(sentBody(fetchMock, 1)).toEqual({})
  })

  it('throws an ApiError with status 0 when the API does not answer', async () => {
    vi.stubGlobal('fetch', vi.fn<typeof fetch>().mockRejectedValue(new TypeError('Failed to fetch')))

    const error = await api.invoices().catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).status).toBe(0)
  })

  it('throws the ProblemDetails of an error answer', async () => {
    stubFetch(problem(404, { title: 'Not Found', detail: 'No invoice with the identifier x.' }))

    await expect(api.invoice('x')).rejects.toMatchObject({ status: 404, message: 'No invoice with the identifier x.' })
  })
})
