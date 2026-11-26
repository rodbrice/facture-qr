# Changelog

All notable changes to this project are recorded in this file. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] - 2026-11-26

The first release: invoices in CHF with a Swiss QR-bill, for one person on one computer. The QR-bills pass the library's validation, a round trip through the decoder and an independent QR decoder; their validation in the SIX portal is still pending. There is no authentication: the application must not be exposed on a network.

### Added

- QR-bill core library (`FactureQr.Core`, unit B0): Swiss and Liechtenstein IBAN and QR-IBAN, structured addresses with the character set of the standard, QR reference and Creditor Reference (ISO 11649), the rules that tie the account to the reference, and the payment part with receipt as PDF or PNG with French headings, rendered with SwissQRBill.NET. Three synthetic samples, one per reference variant, under `docs/samples`. ([ADR-0001](docs/adr/0001-scope-and-non-goals.md), [ADR-0002](docs/adr/0002-qr-bill-library.md))
- Invoice model and storage (`FactureQr.Persistence`, unit B1): customers, invoices in CHF with their lines, states draft, issued and paid, overdue derived from the due date, and yearly numbering (`2026-0001`) that stays unique and without gaps when two invoices are issued at the same moment. PostgreSQL with EF Core, schema created by migrations, critical rules repeated as database constraints. ([ADR-0003](docs/adr/0003-invoice-persistence-and-numbering.md))
- HTTP API (`FactureQr.Api`, unit B2): customers, drafts and their lines, issuing, payment, the list of invoices with their state, the invoice as a one-page A4 PDF in French with its QR-bill, the creditor from the configuration, a health check and the OpenAPI document. Errors as ProblemDetails (400, 404, 409); logs as JSON lines without names or addresses. ([ADR-0004](docs/adr/0004-http-api.md))
- Front end in French (`web/`, unit B3): React and TypeScript, served by the API; customers, the list of invoices with an overdue flag, the editor of a draft, issuing, the PDF download, the payment and the settings, with each error of the API shown next to its field or action. ([ADR-0005](docs/adr/0005-front-end.md))
- Docker Compose demo (unit B4): `docker compose up` builds the application, starts PostgreSQL, applies the migrations and adds synthetic customers and invoices in every state, on `127.0.0.1:8080` only. ([ADR-0006](docs/adr/0006-docker-compose-demo.md))
- Protections of the local API (unit B4): a change that the browser reports as coming from another site is refused with 403, and only the local host names are answered, which stops DNS rebinding. ([ADR-0007](docs/adr/0007-local-api-browser-protections.md))
- Tests and continuous integration: unit tests of the QR-bill rules and of the invoice model, golden payloads written from the standard, round trips through the decoder, storage and API tests against PostgreSQL (Testcontainers), the QR code of the invoice PDF read back by an independent decoder, Vitest tests of the front end and a Playwright end-to-end test of the whole flow. GitHub Actions runs the build with warnings as errors, the formatting check, the linters, all the tests, the end-to-end test and the Docker Compose demo on every push and pull request.
- Security tooling: CodeQL analysis of the C# and TypeScript code, Dependabot version updates for NuGet, npm, GitHub Actions and the Docker images, and a threat model in the README.
- Documentation: README in English with a summary in French, an architecture diagram, screenshots of the demo, decision records and a validation record per unit under `docs/validation`.

[Unreleased]: https://github.com/rodbrice/facture-qr/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/rodbrice/facture-qr/releases/tag/v0.1.0
