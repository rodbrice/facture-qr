# ADR-0001: Scope of the first unit and non-goals

**Status:** Accepted · **Date:** 2026-11-12

## Context

Facture QR is a small invoicing tool for independents in Switzerland: invoices with a valid QR-bill payment part and, later, reconciliation of incoming payments. The QR-bill is a regulated format (SIX Implementation Guidelines, IG). Getting the payment part wrong means bills that banks reject, so the first unit of work must prove that the core produces correct QR-bills before any screen or database exists.

The first unit (B0) is deliberately a spike with production-quality tests, not a product: its purpose is to validate the standard, the library and the validation methods, and to leave the repository with a working quality baseline (build with warnings as errors, formatting check, tests, CI).

## Decision

1. **B0 contains only the core library and its tests.** `FactureQr.Core` holds the domain rules of the payment part (structured address, IBAN/QR-IBAN, QR reference, Creditor Reference, request validation) and a thin renderer over the QR-bill library. There is no database, no HTTP API, no user interface, no deployment and no paid service in B0. These come in later units, on top of this core.

2. **CHF only.** The request model accepts only `CHF` and refuses any other currency with an explicit message. Reasons:
   - The QR-IBAN with QR reference, the variant most Swiss banks offer to independents, is valid only in CHF (IG v2.4, chapters 2.10 and 2.12.1).
   - IG v2.4, in force from 14 November 2026, restricts EUR bills to "IBAN with Creditor Reference" or "IBAN with unstructured message", and EUR bills will be processed as SEPA Credit Transfers after November 2027 at the latest. Supporting EUR means implementing rules that are changing; supporting CHF means implementing rules that are stable between v2.3 and v2.4.
   - The target users invoice in CHF. If EUR is ever added, it will be a separate decision with its own ADR and the v2.4 restrictions listed above.

3. **No VAT logic.** The tool is not a tax tool. Amounts are entered as totals; whether they include VAT is the user's business.

4. **The core design may change in the next unit.** B0 is a spike. The public surface of `FactureQr.Core` (records, static renderer) is small enough to reshape when the API and persistence are added; nothing outside this repository depends on it.

5. **Only synthetic data.** Every IBAN, name and address in tests, samples and documentation comes from the examples in Annex A of the IG. No real person, company or account appears in the repository.

## Consequences

- The next units can build the invoice aggregate, the API and the UI on a core whose correctness is already demonstrated by golden tests, round-trip tests and the external validation recorded in `docs/validation/`.
- A user who needs EUR invoices cannot use the tool; the refusal message says so.
- The repository stays small: one library, one test project, one tool that regenerates the samples.
