# ADR-0004: HTTP API, error responses and the invoice PDF

**Status:** Accepted · **Date:** 2026-11-12

## Context

The third unit (B2) exposes the invoice model of B1 over HTTP, so that a front end (B3) can drive the whole v0.1 flow: register customers, create an invoice with lines, issue it, download its PDF with the QR-bill and mark it as paid. The application is local and used by one person; authentication, several users, VAT and EUR are outside v0.1 (ADR-0001).

## Decision

### 1. One project, Minimal APIs, endpoints grouped by resource

`src/FactureQr.Api` is an ASP.NET Core application with Minimal APIs. The endpoints are mapped in two static classes, `CustomerEndpoints` and `InvoiceEndpoints`; each handler is a short method that reads, calls the domain and saves. There are no controllers, no mediator and no repository layer: the handlers use `FactureQrDbContext` and `InvoiceIssuer` from B1 directly. Handlers return typed results (`Results<Ok<…>, ValidationProblem, ProblemHttpResult>`), so the OpenAPI document lists each response.

| Method and path | Purpose | Success |
|---|---|---|
| `POST /customers` | Create a customer | 201 + `Location` |
| `GET /customers`, `GET /customers/{id}` | List by name, read one | 200 |
| `POST /invoices` | Create a draft for a customer, optionally with lines | 201 + `Location` |
| `GET /invoices` | List, newest first, without lines; each row carries the status and whether the invoice is overdue | 200 |
| `GET /invoices/{id}` | Read one with its lines | 200 |
| `POST /invoices/{id}/lines`, `DELETE /invoices/{id}/lines/{lineId}` | Edit a draft | 200 |
| `POST /invoices/{id}/issue` | Issue: number, issue date, due date | 200 |
| `POST /invoices/{id}/mark-paid` | Record the payment date | 200 |
| `GET /invoices/{id}/pdf` | The invoice as PDF (`application/pdf`) | 200 |
| `GET /health` | `Healthy` when the database answers, 503 otherwise | 200 |
| `GET /openapi/v1.json` | The OpenAPI 3.1 document | 200 |

State changes are actions (`/issue`, `/mark-paid`) rather than a `PATCH` of the status, because each one has its own rules and its own data (dates) and can be refused for its own reasons.

### 2. Status codes: 400 for the request, 404 for the URL, 409 for the state

Every error has a ProblemDetails body (RFC 9457, `application/problem+json`).

- **400 Bad Request**: the request is wrong whatever the stored data is. Broken JSON, a missing or null required value, an address that breaks a QR-bill rule, a line with an invalid quantity or price, a line description with a character the PDF cannot print, an issue date outside the years of the number or too late for the default due date, a due date before the issue date, a payment date in the future or before the issue date, a customer identifier in the body that does not exist. Validation errors carry an `errors` object keyed by the JSON field (`postalCode`, `lines[1]`, `dueDate`), so a form can show the message next to the field.
- **404 Not Found**: the customer, invoice or line named in the URL does not exist.
- **409 Conflict**: the request is valid, but the state of the invoice does not allow it. Editing or issuing an invoice that is no longer a draft, issuing a draft without lines or with a total below 0.01 CHF, marking a draft or a paid invoice as paid, asking for the PDF of a draft, adding a line to a draft that has no room left on the page, or a concurrent change detected by the revision of the invoice (ADR-0003).

The handlers check the 404 and the obvious state conflicts themselves before calling the domain. The rules that only the domain knows (for example "an invoice needs a line to be issued") raise `InvoiceRuleException`, which `DomainExceptionHandler` turns into a 409; nothing is saved in that case, and a refused issue does not use up a number because the transaction of ADR-0003 is rolled back. Any other exception is a 500 without details.

The JSON options reject a missing or null value for a non-nullable property (`RespectNullableAnnotations`, `RespectRequiredConstructorParameters`) instead of silently using a default. Enum values are written as names (`"Issued"`). A body that cannot be bound is a 400 in every environment: the Development default of ASP.NET Core, which throws instead, is turned off so that it does not become a 500 through the exception handler.

### 3. Validation reuses the domain

There is no separate validation library and no second copy of the rules. The request records are converted into domain values (`StructuredAddress`, `Invoice.AddLine`), whose constructors and methods already validate and explain; the handler turns the exception into a 400 for the field it came from. The only checks written in the API are those that depend on the request alone and that the domain cannot know: the default dates, a payment date not in the future, the year range of the number.

### 4. The creditor comes from the configuration

The creditor (name, structured address, IBAN or QR-IBAN) is read from the `Invoicing` configuration section (`appsettings.json`, environment variables such as `Invoicing__Creditor__Iban`). They are validated with the QR-bill rules when the application starts, so a wrong IBAN stops the start with a message naming the setting rather than failing at the first PDF. For one local user this is enough; editing these settings from the user interface (B3) may move them into the database with their own migration.

### 5. Dates are Swiss calendar dates

"Today" is the date in Europe/Zurich, read from an injected `TimeProvider` so that tests fix it. Without a body, `/issue` uses today and a due date 30 days later (the usual payment term in Switzerland; the request can give any other due date), and `/mark-paid` uses today. *Overdue* is computed from today when a response is written, as in B1.

### 6. The payment reference comes from the invoice number

`InvoicePayment.CreateRequest` builds the QR-bill of an issued invoice. Its reference is made of the year and the sequence of the number padded to seven digits (`2026-0042` → `20260000042`): with a QR-IBAN it becomes a QR reference (left-padded to 26 digits plus the check digit), with a plain IBAN a Creditor Reference (`RF` + check digits + the same digits). Different numbers therefore always give different references, and v0.2 can find the invoice from the reference of an incoming payment. The message of the bill is `Facture <number>`; the debtor is the address copied onto the invoice.

Some banks require a customer identification number at the start of a QR reference. This is not supported in v0.1; when needed, it will be a configured prefix of the same digits.

### 7. The PDF is one A4 page in French

`InvoicePdf` draws, with the canvas of SwissQRBill.NET, the creditor, the address of the customer (on the right, for a window envelope), the number, the dates, the lines with their amounts and the total, and the payment part with receipt across the bottom 105 mm, separated by a dashed line with scissors. Amounts use the Swiss notation (`1'200.00`), dates `dd.MM.yyyy`. Descriptions wrap inside their column using the font metrics of the library.

- **One page only.** The canvas of the library has a single page. Instead of splitting an invoice, the API refuses a line that would not fit (409 on `POST /lines`, 400 on `POST /invoices`), so an issued invoice always has a PDF. At least ten one-line descriptions fit; long descriptions take more room.
- **Regular font only.** The bold font the library embeds contains only the characters of its own headings; printing a name such as `Max Muster & Söhne` in bold fails. Headings of the invoice are therefore in the regular font.
- **Only printable text is stored.** The regular font covers Basic Latin, Latin-1 (except `ÿ`), Latin Extended-A, `Ș ș Ț ț` and `€`; any other character makes the rendering fail. Every text that reaches the PDF is therefore checked against the font (`InvoicePdf.FirstUnprintable`) when it is stored, not when the PDF is asked for. A description: the typographic apostrophes, quotes, dashes and ellipsis that word processors and phones insert (`’ ‘ “ ” – — …`) are replaced by their plain equivalents, and a description that still contains a character outside the font is refused with a 400 naming the character (`U+1F600`). An address: the QR-bill character set is wider than the font by one character, `ÿ` (as in `L'Haÿ-les-Roses`), so `Customer` and `CreditorAccount` refuse an address with a character outside the font; `POST /customers` answers 400 naming the field and the character, and a creditor configured with such a character stops the start-up with a message naming `Invoicing:Creditor` and the field. The rest of the page (the number, the dates, the amounts, the message `Facture <number>` and the French headings) is built from ASCII and from the font's own characters.
- `QrBillRenderer` and `InvoicePdf` are the only classes that use the library (ADR-0002); the payment part goes through the same validation in both.

### 8. Logs, health and OpenAPI

- **Logs** are written to the console as JSON, one object per line, with the values of each message as separate properties. One entry per request (method, path, status code, duration) comes from the ASP.NET Core HTTP logging middleware, configured without headers and bodies because bodies contain addresses. Business events (customer created, invoice created, issued, paid, request refused with 409) are logged with identifiers and invoice numbers only, never names or addresses.
- **`/health`** runs one check, that the database accepts a connection.
- **OpenAPI.** The document is generated by `Microsoft.AspNetCore.OpenApi` from the endpoints and their XML comments, served at `/openapi/v1.json` and committed as `docs/api/openapi.json`. A test fails when the committed copy differs from what the API serves; `FACTUREQR_UPDATE_OPENAPI=1 dotnet test` rewrites it. No interactive documentation page is included.

### 9. Not in this unit

No authentication (local, single-user application; the API must not be exposed on a network as it is), no pagination (an independent's volume of invoices fits in one response), no deletion of drafts or customers, no cancellation or credit notes, no VAT, no EUR, no container image or deployment (B4).

## Consequences

- The integration tests (`tests/FactureQr.Api.Tests`) start the API in memory with `WebApplicationFactory`, each test against its own database created by the migrations in a PostgreSQL container started by Testcontainers. They cover 200, 201, 400, 404 and 409 for every endpoint, the default dates, the list with its status and overdue flags, the printable-text rule for descriptions and addresses, 20 drafts issued through the API at the same time, the page limit, health, the start-up check of the creditor and the OpenAPI document.
- The PDF tests do not trust the library that drew the PDF: `PdfQrReader` paints the filled shapes of the payment part found by PdfPig onto a bitmap and decodes the QR code with ZXing.Net; the decoded text is then parsed and compared with the invoice (account, amount, reference, debtor, message), for a QR-IBAN and for a plain IBAN.
- The front end of B3 can rely on the `errors` keys of a 400 to place messages next to fields, and on 409 to tell the user that the invoice has changed state.
