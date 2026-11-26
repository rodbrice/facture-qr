# ADR-0006: Docker Compose demo with migrations and synthetic data at start

**Status:** Accepted · **Date:** 2026-11-26

## Context

The fifth unit (B4) is the first public release, v0.1. Someone who clones the repository should get the application running with one command, following only the README, without installing .NET, Node.js or PostgreSQL, and should see invoices in every state without typing them first. Until now the schema was created with `dotnet ef database update` (ADR-0003) and the front end was built by hand into the API's `wwwroot` (ADR-0005). The application has no authentication (ADR-0004), so the demo must stay on the computer it runs on.

## Decision

### 1. Two services: PostgreSQL and one image of the API with the front end

`docker-compose.yml` at the root starts `db` (`postgres:17-alpine`, the version of the tests and of CI) and `app`, built by the root `Dockerfile` in three stages: Node.js 22 builds the front end into `src/FactureQr.Api/wwwroot`, the .NET 10 SDK publishes the API with it, and the final image holds only the ASP.NET Core runtime and the published files and runs as the image's unprivileged user. The browser talks to one origin, as in ADR-0005.

`app` publishes port 8080 on `127.0.0.1` only (`FACTUREQR_PORT` changes the port on the host); `db` publishes no port at all, so only `app` reaches it. The database password in the file is a demo value for a database that cannot be reached from outside the Docker network. The data lives in the named volume `db-data` and survives `docker compose down`; `docker compose down --volumes` deletes it.

### 2. The API applies the migrations when it starts, if configured

With `Database:ApplyMigrations=true`, a hosted service (`DatabaseStartup`) applies the pending migrations before the server answers. The schema is still created only by the migrations of ADR-0003; this only changes who runs them. The setting is off by default, so running the API by hand or in the tests is unchanged. If the migration fails, the API does not start, and `depends_on: condition: service_healthy` makes the API wait until PostgreSQL accepts connections.

### 3. Synthetic data added to an empty database, if configured

With `Demo:SeedSyntheticData=true`, the same hosted service adds `DemoData` when the database holds no customer and no invoice: five customers and five invoices, one draft, one issued, one issued and overdue, two paid. Names and addresses are fictitious and come from the examples of the QR-bill standard, like the samples; the creditor is the synthetic one of the samples. The data goes through the domain (`Customer`, `Invoice`) and `InvoiceIssuer`, so it obeys the same rules and gets its numbers the same way as data entered through the API. The dates are counted from today, so the overdue invoice is overdue whatever the day the demo starts. A database that already holds data is never touched, so restarting the demo keeps what was entered.

### 4. The configuration comes from the environment

`docker-compose.yml` sets the connection string, the two settings above and the creditor (`Invoicing__Creditor__*`) as environment variables. The API runs in the Production environment: `appsettings.Development.json` is not used by the demo.

## Alternatives considered

- **A separate migration container** (an EF Core migration bundle run before the API). It keeps the migration out of the API, but adds a third service, a second build target and the `dotnet-ef` tool to the image, for a demo that runs one instance of the API. Running the migrations in the API behind a setting is less code and is tested with the rest of the API.
- **A SQL seed script run by the PostgreSQL image** (`/docker-entrypoint-initdb.d`). It would run before the schema exists (the migrations run later, in the API), would repeat the numbering and the totals by hand, and could drift from the domain rules. Going through the domain avoids both.
- **Seeding through the HTTP API from a script** (curl). It would test the same rules, but needs a third container and parsing of identifiers in shell; the in-process seed is shorter and covered by `DatabaseStartupTests`.

## Consequences

- `docker compose up` gives a working application at `http://localhost:8080` with synthetic data. The CI workflow has a job that builds and starts the demo and checks that it answers with the five invoices.
- The API has two new settings, both off unless set. `DatabaseStartupTests` checks that, with both, a database that does not exist is created, migrated and filled with invoices in every state; that a restart or a database that already holds data adds nothing; and that without them the API neither migrates nor seeds.
- The demo is not a deployment: there is no TLS, no authentication and no backup. Exposing it beyond `127.0.0.1` is out of scope (see the threat model in the README).
