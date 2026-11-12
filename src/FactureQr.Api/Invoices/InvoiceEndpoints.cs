using FactureQr.Api.Infrastructure;
using FactureQr.Core.Invoicing;
using FactureQr.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace FactureQr.Api.Invoices;

/// <summary>
/// The <c>/invoices</c> endpoints. Each one answers 400 when the request itself
/// is invalid, 404 when the invoice in the URL does not exist and 409 when the
/// request is valid but the state of the invoice does not allow it (see ADR-0004).
/// </summary>
public static class InvoiceEndpoints
{
    private const string LogCategory = "FactureQr.Api.Invoices";

    /// <summary>Maps the endpoints.</summary>
    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/invoices").WithTags("Invoices");

        group.MapPost("/", Create)
            .WithSummary("Creates a draft for a customer, optionally with its lines.")
            .ProducesValidationProblem();

        group.MapGet("/", List)
            .WithSummary("Lists the invoices, newest first, optionally filtered by status or overdue.")
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}", Get)
            .WithSummary("Returns one invoice with its lines.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/lines", AddLine)
            .WithSummary("Adds a line at the end of a draft.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}/lines/{lineId:guid}", RemoveLine)
            .WithSummary("Removes a line from a draft; the following lines move up.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/issue", Issue)
            .WithSummary("Issues a draft: gives it the next number of the year and freezes it.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/mark-paid", MarkPaid)
            .WithSummary("Records that an issued invoice was paid.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}/pdf", Pdf)
            .WithSummary("Returns the invoice as a one-page A4 PDF with its QR-bill payment part.")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Created<InvoiceResponse>, ValidationProblem>> Create(
        CreateInvoiceRequest request, FactureQrDbContext db, SwissCalendar calendar, ILoggerFactory loggers, CancellationToken cancellationToken)
    {
        Customer? customer = await db.Customers.SingleOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return Problems.Invalid("customerId", $"No customer with the identifier {request.CustomerId}.");
        }

        var invoice = Invoice.CreateDraft(customer);
        IReadOnlyList<LineRequest> lines = request.Lines ?? [];
        for (int i = 0; i < lines.Count; i++)
        {
            try
            {
                invoice.AddLine(lines[i].Description, lines[i].Quantity, lines[i].UnitPrice);
            }
            catch (InvoiceRuleException e)
            {
                return Problems.Invalid($"lines[{i}]", e.Message);
            }
        }

        if (!InvoicePdf.LinesFitOnOnePage(invoice.Lines))
        {
            return Problems.Invalid("lines", "The lines do not fit on one invoice page.");
        }

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken);
        ILogger logger = loggers.CreateLogger(LogCategory);
        ApiLog.InvoiceCreated(logger, invoice.Id, customer.Id);

        return TypedResults.Created($"/invoices/{invoice.Id}", InvoiceResponse.From(invoice, calendar.Today));
    }

    private static async Task<Ok<List<InvoiceSummary>>> List(
        InvoiceStatus? status, bool? overdue, FactureQrDbContext db, SwissCalendar calendar, CancellationToken cancellationToken)
    {
        DateOnly today = calendar.Today;
        IQueryable<Invoice> query = db.Invoices.AsNoTracking();
        if (status is InvoiceStatus wanted)
        {
            query = query.Where(i => i.Status == wanted);
        }

        if (overdue is bool isOverdue)
        {
            query = isOverdue
                ? query.Where(i => i.Status == InvoiceStatus.Issued && i.DueDate < today)
                : query.Where(i => i.Status != InvoiceStatus.Issued || i.DueDate >= today);
        }

        // Identifiers are UUID version 7: ordering by them is ordering by creation time.
        List<Invoice> invoices = await query.OrderByDescending(i => i.Id).ToListAsync(cancellationToken);
        return TypedResults.Ok(invoices.Select(i => InvoiceSummary.From(i, today)).ToList());
    }

    private static async Task<Results<Ok<InvoiceResponse>, ProblemHttpResult>> Get(
        Guid id, FactureQrDbContext db, SwissCalendar calendar, CancellationToken cancellationToken)
    {
        Invoice? invoice = await Find(db, id, cancellationToken);
        return invoice is null ? NotFound(id) : TypedResults.Ok(InvoiceResponse.From(invoice, calendar.Today));
    }

    private static async Task<Results<Ok<InvoiceResponse>, ValidationProblem, ProblemHttpResult>> AddLine(
        Guid id, LineRequest line, FactureQrDbContext db, SwissCalendar calendar, CancellationToken cancellationToken)
    {
        Invoice? invoice = await Find(db, id, cancellationToken);
        if (invoice is null)
        {
            return NotFound(id);
        }

        if (invoice.Status != InvoiceStatus.Draft)
        {
            return NotADraft(invoice);
        }

        try
        {
            invoice.AddLine(line.Description, line.Quantity, line.UnitPrice);
        }
        catch (InvoiceRuleException e)
        {
            return Problems.Invalid("line", e.Message);
        }

        // The line is valid, but the draft has no room left for it; nothing is saved.
        if (!InvoicePdf.LinesFitOnOnePage(invoice.Lines))
        {
            return Problems.Conflict("The draft is full: this line would not fit on the invoice page.");
        }

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(InvoiceResponse.From(invoice, calendar.Today));
    }

    private static async Task<Results<Ok<InvoiceResponse>, ProblemHttpResult>> RemoveLine(
        Guid id, Guid lineId, FactureQrDbContext db, SwissCalendar calendar, CancellationToken cancellationToken)
    {
        Invoice? invoice = await Find(db, id, cancellationToken);
        if (invoice is null)
        {
            return NotFound(id);
        }

        if (invoice.Status != InvoiceStatus.Draft)
        {
            return NotADraft(invoice);
        }

        if (!invoice.Lines.Any(l => l.Id == lineId))
        {
            return Problems.NotFound($"The invoice {id} has no line {lineId}.");
        }

        invoice.RemoveLine(lineId);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(InvoiceResponse.From(invoice, calendar.Today));
    }

    private static async Task<Results<Ok<InvoiceResponse>, ValidationProblem, ProblemHttpResult>> Issue(
        Guid id,
        IssueRequest? request,
        FactureQrDbContext db,
        InvoiceIssuer issuer,
        InvoicingSettings settings,
        SwissCalendar calendar,
        ILoggerFactory loggers,
        CancellationToken cancellationToken)
    {
        DateOnly issueDate = request?.IssueDate ?? calendar.Today;
        DateOnly dueDate = request?.DueDate ?? issueDate.AddDays(settings.PaymentTermDays);
        if (issueDate.Year is < InvoiceNumber.MinYear or > InvoiceNumber.MaxYear)
        {
            return Problems.Invalid("issueDate", $"The issue date must be in the years {InvoiceNumber.MinYear} to {InvoiceNumber.MaxYear}.");
        }

        if (dueDate < issueDate)
        {
            return Problems.Invalid("dueDate", $"The due date {dueDate:yyyy-MM-dd} is before the issue date {issueDate:yyyy-MM-dd}.");
        }

        if (!await db.Invoices.AnyAsync(i => i.Id == id, cancellationToken))
        {
            return NotFound(id);
        }

        // A draft that cannot be issued (already issued, no lines, total too small) raises
        // InvoiceRuleException, which DomainExceptionHandler answers with 409; nothing is saved.
        InvoiceNumber number = await issuer.IssueAsync(id, issueDate, dueDate, cancellationToken);
        ILogger logger = loggers.CreateLogger(LogCategory);
        ApiLog.InvoiceIssued(logger, id, number);

        Invoice invoice = (await Find(db, id, cancellationToken))!;
        return TypedResults.Ok(InvoiceResponse.From(invoice, calendar.Today));
    }

    private static async Task<Results<Ok<InvoiceResponse>, ValidationProblem, ProblemHttpResult>> MarkPaid(
        Guid id, MarkPaidRequest? request, FactureQrDbContext db, SwissCalendar calendar, ILoggerFactory loggers, CancellationToken cancellationToken)
    {
        Invoice? invoice = await Find(db, id, cancellationToken);
        if (invoice is null)
        {
            return NotFound(id);
        }

        if (invoice.Status != InvoiceStatus.Issued)
        {
            return Problems.Conflict($"Only an issued invoice can be marked as paid; this one is {invoice.Status}.");
        }

        DateOnly today = calendar.Today;
        DateOnly paidOn = request?.PaidOn ?? today;
        if (paidOn > today)
        {
            return Problems.Invalid("paidOn", $"The payment date {paidOn:yyyy-MM-dd} is in the future.");
        }

        if (paidOn < invoice.IssueDate)
        {
            return Problems.Invalid("paidOn", $"The payment date {paidOn:yyyy-MM-dd} is before the issue date {invoice.IssueDate:yyyy-MM-dd}.");
        }

        invoice.MarkPaid(paidOn);
        await db.SaveChangesAsync(cancellationToken);
        ILogger logger = loggers.CreateLogger(LogCategory);
        ApiLog.InvoicePaid(logger, invoice.Number, paidOn);

        return TypedResults.Ok(InvoiceResponse.From(invoice, today));
    }

    private static async Task<Results<FileContentHttpResult, ProblemHttpResult>> Pdf(
        Guid id, FactureQrDbContext db, InvoicingSettings settings, CancellationToken cancellationToken)
    {
        Invoice? invoice = await Find(db, id, cancellationToken);
        if (invoice is null)
        {
            return NotFound(id);
        }

        if (invoice.Number is not InvoiceNumber number)
        {
            return Problems.Conflict("A draft has no PDF: issue it first.");
        }

        byte[] pdf = InvoicePdf.Render(invoice, settings.Creditor);
        return TypedResults.File(pdf, "application/pdf", $"facture-{number}.pdf");
    }

    private static Task<Invoice?> Find(FactureQrDbContext db, Guid id, CancellationToken cancellationToken) =>
        db.Invoices.Include(i => i.Lines).SingleOrDefaultAsync(i => i.Id == id, cancellationToken);

    private static ProblemHttpResult NotFound(Guid id) => Problems.NotFound($"No invoice with the identifier {id}.");

    private static ProblemHttpResult NotADraft(Invoice invoice) =>
        Problems.Conflict($"Invoice {invoice.Number} is {invoice.Status}: only a draft can change.");
}
