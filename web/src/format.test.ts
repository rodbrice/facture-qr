import { describe, expect, it } from 'vitest'
import { formatChf, formatDate, parseDecimal } from './format'

describe('format', () => {
  it('writes amounts as on the invoice PDF', () => {
    expect(formatChf(1200)).toBe("1'200.00")
    expect(formatChf(0.5)).toBe('0.50')
    expect(formatChf(999999999.99)).toBe("999'999'999.99")
  })

  it('writes ISO dates as day.month.year', () => {
    expect(formatDate('2026-11-12')).toBe('12.11.2026')
    expect(formatDate(null)).toBe('')
  })

  it('reads numbers typed with a dot, a comma or thousands separators', () => {
    expect(parseDecimal('7.5')).toBe(7.5)
    expect(parseDecimal(' 7,5 ')).toBe(7.5)
    expect(parseDecimal("1'200")).toBe(1200)
    expect(parseDecimal('')).toBeNull()
    expect(parseDecimal('abc')).toBeNull()
    expect(parseDecimal('1.2.3')).toBeNull()
  })
})
