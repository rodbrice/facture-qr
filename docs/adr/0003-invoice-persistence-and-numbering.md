# ADR-0003: Invoice model, PostgreSQL with EF Core, and yearly numbering

**Status:** Accepted · **Date:** 2026-11-12

## Context

The second unit (B1) adds what an invoice is before any API or screen exists: customers, invoices with their lines, the states of an invoice and its number. Three rules come from the business and the QR-bill standard:

- An issued invoice is a legal document. Once sent, its customer, address, lines and total must not change.
- The total is the sum of the lines, and it must fit in a QR-bill: at most 999 999 999.99 CHF, and at least 0.01 CHF to be issued (the amount range of the Implementation Guidelines, already enforced by `QrBillRequest`).
- Numbers are sequential within a year (`2026-0001`, `2026-0002`, …, then `2027-0001`) and never used twice, even when two invoices are issued at the same moment.

## Decision

### 1. The rules live in the domain classes, the database repeats the critical ones

`Invoice`, `InvoiceLine` and `Customer` sit in `FactureQr.Core/Invoicing` and have no dependency on EF Core. Every change goes through a method (`AddLine`, `RemoveLine`, `Issue`, `MarkPaid`) that checks the rules first and changes nothing when a rule is broken (`InvoiceRuleException`). Setters are private, so code cannot bypass the methods.

- **States.** `Draft` → `Issued` → `Paid`. Only a draft can change. `Issue` requires at least one line, a total of at least 0.01 CHF, a number from the year of the issue date and a due date not before the issue date. An issued invoice can only be marked as paid, with a date not before the issue date; a paid invoice is final. *Overdue* is not a state: it is derived from the due date (`IsOverdue(today)`), so it never needs a background job to stay correct.
- **Amounts.** A line amount is quantity × unit price rounded to the centime, half away from zero. Quantities have up to three decimals (1.5 hours, 0.125 kg), prices two. The total is updated with every line change and checked against the QR-bill maximum before the line is added.
- **Address copy.** The invoice copies the customer's structured address when it is created; a draft for another customer is created anew. A later change of the customer's address does not touch issued invoices.
- **Cancellation and credit notes** are not part of B1. When they are needed they will be new states or documents, not edits of an issued invoice.

The database repeats the rules whose violation would corrupt stored data even if some future code bypassed the domain: amount columns are `numeric(11, 2)` (exactly the QR-bill range), check constraints tie the status to its fields (a draft has no number or dates; an issued or paid invoice has all of them, a number from the year of the issue date, a due date not before the issue date and a total of at least 0.01; only a paid invoice has a payment date), and a unique index on `(number_year, number_sequence)` makes a duplicate number impossible. The sum of the lines is not repeated in the database: it would need a trigger, and the domain keeps it in one place.

### 2. PostgreSQL with EF Core and code-first migrations

- **PostgreSQL** because it is free, runs anywhere in a container, has real transactions, check constraints, `INSERT … ON CONFLICT … RETURNING` and exact `numeric` amounts. SQLite was rejected: no row locks, so the concurrency guarantee below could not be tested in the same way as it would run.
- **EF Core 10 with Npgsql** because it maps the domain classes without attributes (configuration in `FactureQr.Persistence/Configurations`), maps the structured address as a *complex property* (columns of the owning table, rebuilt through the record's constructor so its validation runs again on read), and produces migrations that are plain C# files under review. Snake-case table and column names (`EFCore.NamingConventions`) keep raw SQL readable.
- **Migrations are the only way the schema changes.** `InitialSchema` creates the four tables. CI applies all migrations to an empty PostgreSQL database with `dotnet ef database update`, and an integration test does the same and checks that the model has no change missing from a migration (`HasPendingModelChanges`).
- **Optimistic concurrency on the invoice.** `Invoice.Revision` is incremented by every change and is a concurrency token. If two users edit or issue the same invoice from copies read at the same time, the second save fails with `DbUpdateConcurrencyException` and nothing is written. This is what stops a stale draft from being edited after another session issued it.

### 3. Numbers come from a counter row per year, locked by the issuing transaction

`invoice_number_counters` has one row per year with the last sequence used. `InvoiceIssuer.IssueAsync` runs one transaction that:

1. reads the draft;
2. allocates the number with one statement:
   ```sql
   INSERT INTO invoice_number_counters (year, last_sequence) VALUES (@year, 1)
   ON CONFLICT (year) DO UPDATE SET last_sequence = invoice_number_counters.last_sequence + 1
   RETURNING last_sequence
   ```
3. calls `Invoice.Issue` with that number and saves;
4. commits.

The statement creates the year's row or increments it, and in both cases keeps the row locked until the transaction ends. A second transaction issuing in the same year waits on the lock, then reads the committed value and gets the next number. If anything fails after the allocation (a broken rule, a concurrency conflict), the transaction rolls back and the increment with it, so **no number is lost and there are no gaps**.

Options rejected:

| Option | Why not |
|---|---|
| `SELECT MAX(number) + 1` | Two transactions read the same maximum; one of them fails on the unique index (or, without the index, both get the same number). The concurrency test fails with this allocator. |
| A PostgreSQL `SEQUENCE` per year | Sequences are not transactional: a rolled-back issue leaves a gap, and a new sequence would have to be created every year. |
| `SERIALIZABLE` isolation with retries | Works, but every issuing path needs a retry loop, and the lock above gives the same guarantee with a simpler failure mode. |

The cost is that issues in the same year are serialised for the duration of one short transaction. For an independent's volume of invoices this is irrelevant.

## Consequences

- The invariants are tested twice: unit tests of the domain (`InvoiceTests`, `InvoiceNumberTests`) and integration tests against a real PostgreSQL started with Testcontainers (`MigrationTests`, `InvoiceStorageTests`, `ConcurrencyTests`). Running the integration tests needs Docker.
- `ConcurrencyTests` issues 40 drafts at once, on 40 connections, both in a year whose counter exists and in a year whose counter does not, and checks that the numbers are exactly 1 to 40; it also issues one draft from two connections at once and checks that it is issued once and the failed attempt does not use up a number.
- The core library changed shape as ADR-0001 allowed: it now holds the invoice model next to the QR-bill rules. Turning an issued invoice into a `QrBillRequest` is left to the next unit, together with the API.
