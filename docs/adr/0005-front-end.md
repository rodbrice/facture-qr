# ADR-0005: React front end served by the API

**Status:** Accepted · **Date:** 2026-11-26

## Context

The fourth unit (B3) makes the v0.1 flow usable from a browser: customers, the list of invoices with their state and whether they are overdue, the editor of a draft, issuing, the PDF, the payment and the settings, with the errors of the API shown to the user. The application stays local and used by one person, without authentication (ADR-0004). The HTTP contract of B2 is the base; the front end must not need a second copy of its rules.

## Decision

### 1. React, TypeScript and Vite in `web/`, with as few libraries as possible

The front end is a React application in TypeScript, built with Vite, in the top-level `web/` folder. Pages are routed in the browser with React Router. There is no component library, no state library and no form library: each page loads what it shows from the API (`useResource`) and replaces it with the answer of each action, and each form is a small component with its own state. All the code is plain React that can be read and changed without knowing another framework. Linting uses oxlint with its React and accessibility (jsx-a11y) rules, the type check `tsc -b`.

### 2. The API serves the built front end; the API paths do not change

`npm run build` writes the front end into `src/FactureQr.Api/wwwroot` (ignored by git). The API serves `index.html` at `/` and the assets with the static file middleware, so in production the browser talks to one origin and no CORS configuration is needed.

The pages have French paths (`/factures`, `/factures/{id}`, `/clients`, `/clients/nouveau`, `/clients/{id}`, `/parametres`) that never overlap the English API paths. The API answers `index.html` only for these paths (`FrontEnd.Pages`, `MapFallbackToFile`), so reloading a page works while an unknown API path is still a 404 and no endpoint of B2 had to move under an `/api` prefix.

During development, `npm run dev` starts the Vite server, which forwards the API paths to the API started with `dotnet run` on port 5080.

### 3. The API is the only place where the rules live

- The forms do not repeat the QR-bill rules (lengths, characters, country code, quantities, dates). They send what was typed (`noValidate`, no `required` attribute that would stop the request) and show the API's answer. The only checks in the browser are those without which no request can be written: a quantity or a price that is not a number, an invoice without a chosen customer.
- Every amount shown (line amounts, total) is the one in the API's answer; the front end never adds up lines itself. Amounts are written as on the PDF (`1'200.00`), dates as `dd.MM.yyyy`.
- Errors (ProblemDetails, ADR-0004): an entry of `errors` in a 400 whose key is a field of the form (`postalCode`, `dueDate`, `paidOn`) is shown under that field; any other key, a 404, a 409 or no answer at all is shown in the alert of the form or of the action that was refused (adding a line, issuing, removing a line, marking as paid).
- The messages of the API are in English and are shown as they come, inside the French interface. The domain stops at the first rule broken, so a refused customer names one field at a time.

### 4. Two additions to the API

- `PUT /customers/{id}` replaces the address of a customer (200, 400 naming the field, 404). Invoices already created keep the address they copied (ADR-0003).
- `GET /settings/creditor` returns the creditor of the configuration (address, IBAN, whether it is a QR-IBAN). The settings page only shows it: the creditor stays in the configuration as decided in ADR-0004, and changing it means changing the configuration and restarting the API. Editing it from the interface, which would move it into the database, is not part of v0.1.

### 5. Basic accessibility, built in

Every input has a label; its hint and its error are linked to it with `aria-describedby`, and an error sets `aria-invalid`. Errors appear in a `role="alert"` region and, after a refused submission, the focus moves to the first invalid field or to the alert. Notices of success use `role="status"`. The page has a skip link, a visible focus ring on every control, a title per page, tables with header cells, and the focus moves to the main content after a change of page. Everything is reachable with the keyboard; a Playwright test goes through the navigation and a form with the keyboard only. No automated audit tool is part of v0.1.

### 6. French only

The interface is in French, like the PDF. There is one language and no translation framework; the texts are written in the components.

### 7. Tests

- **Vitest** with Testing Library (jsdom): the error mapping (`toApiError`, `formErrors`), the formatting, and the forms (labels, values sent, errors placed next to their field, focus, keyboard use).
- **Playwright**: one end-to-end test of the whole flow (customer refused then saved, draft with lines and a refused line, issue, PDF download, payment, list, settings) and one keyboard test, against the API and a real PostgreSQL database, on the built front end served by the API. In CI the video of the run is kept as an artifact.

## Consequences

- Building the front end needs Node.js 22; the API still builds, tests and runs without it (it then serves no pages).
- CI has a second job for the front end (lint, type check, Vitest, build), and the main job installs Chromium to run the end-to-end test after the .NET tests, on the database the migrations step created.
- The rules of an address or a line are changed in one place, the domain; the front end shows the new messages without a change.
