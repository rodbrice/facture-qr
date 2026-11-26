# ADR-0007: Protections of a local API without authentication

**Status:** Accepted · **Date:** 2026-11-26

## Context

v0.1 runs on one computer for one person and has no authentication (ADR-0004). The Docker Compose demo publishes it on `127.0.0.1` only (ADR-0006), so no other computer reaches it. That leaves the browser of the person using it: any web page open in that browser can send requests to `http://localhost:8080`.

- The browser does not let another site **read** the answers: the API sends no CORS headers.
- Another site can still **send** some requests. A POST without a body, or with a form content type, needs no CORS preflight. `POST /invoices/{id}/issue` and `POST /invoices/{id}/mark-paid` accept an empty body, so a page that knew or guessed an invoice identifier could issue or mark it as paid. The other endpoints that change data need a JSON body, which a cross-site page can only send after a preflight that the API does not answer.
- **DNS rebinding:** a site can make its own host name resolve to `127.0.0.1`. The browser then treats the API as that site's origin, and the page can read and change everything. The API answered any host name (`AllowedHosts: *`).

## Decision

1. **Requests from another site that would change data are refused with 403.** Browsers send the `Sec-Fetch-Site` header with each request: `same-origin` for the pages served by the API, `none` for an address typed by the user. A request with any other value (`same-site`, `cross-site`) and a method other than GET, HEAD or OPTIONS is refused before it reaches an endpoint (`CrossSiteRequests`), with a ProblemDetails body and a warning in the log. Requests without the header (curl, scripts, the integration tests) are not sent by a browser on behalf of a page and pass.
2. **Only the local host names are answered.** `AllowedHosts` is `localhost;127.0.0.1;[::1]` in `appsettings.json`; ASP.NET Core answers 400 to a request whose `Host` header names anything else, which stops DNS rebinding. Running the API under another host name means changing this setting, together with adding authentication.

## Alternatives considered

- **An anti-forgery token** (a cookie and a header checked on each change). It would also cover browsers that do not send `Sec-Fetch-Site`, but needs a token endpoint and changes in every request of the front end. Current versions of Chrome, Edge, Firefox and Safari send the header.
- **Requiring a JSON body on issue and mark-paid.** It would close the two endpoints found today but not the next endpoint written with an optional body; the check in front of all endpoints does not depend on how each one reads its body.
- **Authentication.** It is the real answer once the application leaves one computer, and is out of scope for v0.1 (ADR-0004).

## Consequences

- The front end, the end-to-end test, the Vite development server (it keeps the `localhost` host name) and the Docker Compose demo are unchanged.
- `BrowserRequestTests` checks the refused and accepted values of `Sec-Fetch-Site`, that reading is not refused, and the host names.
- These protections do not replace authentication: any program running on the same computer can still call the API. The threat model in the README lists what v0.1 protects and what it does not.
