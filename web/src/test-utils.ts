import { vi } from 'vitest'

/** A ProblemDetails answer as the API writes it. */
export function problem(status: number, body: Record<string, unknown>): Response {
  return new Response(JSON.stringify({ status, ...body }), { status, headers: { 'Content-Type': 'application/problem+json' } })
}

export function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

/** Replaces fetch with a mock that answers the given responses in order. */
export function stubFetch(...responses: Response[]) {
  const fetchMock = vi.fn<typeof fetch>()
  for (const response of responses) {
    fetchMock.mockResolvedValueOnce(response)
  }
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

/** The JSON body of the n-th call of a fetch mock. */
export function sentBody(fetchMock: ReturnType<typeof stubFetch>, call = 0): unknown {
  const body = fetchMock.mock.calls[call]?.[1]?.body
  return typeof body === 'string' ? JSON.parse(body) : undefined
}
