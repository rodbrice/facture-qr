using FactureQr.Core.Invoicing;

namespace FactureQr.Api.Infrastructure;

/// <summary>
/// The business events the API logs, as structured messages: each value is a
/// separate property of the JSON log entry. Only identifiers and invoice
/// numbers are logged, never names or addresses.
/// </summary>
public static partial class ApiLog
{
    /// <summary>A customer was created.</summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Customer {CustomerId} created")]
    public static partial void CustomerCreated(ILogger logger, Guid customerId);

    /// <summary>The address of a customer was replaced.</summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Customer {CustomerId} updated")]
    public static partial void CustomerUpdated(ILogger logger, Guid customerId);

    /// <summary>A draft was created.</summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Invoice {InvoiceId} created for customer {CustomerId}")]
    public static partial void InvoiceCreated(ILogger logger, Guid invoiceId, Guid customerId);

    /// <summary>A draft was issued.</summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Invoice {InvoiceId} issued as {InvoiceNumber}")]
    public static partial void InvoiceIssued(ILogger logger, Guid invoiceId, InvoiceNumber invoiceNumber);

    /// <summary>An issued invoice was marked as paid.</summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Invoice {InvoiceNumber} marked as paid on {PaidOn}")]
    public static partial void InvoicePaid(ILogger logger, InvoiceNumber? invoiceNumber, DateOnly paidOn);

    /// <summary>A request that would change data came from another site and was refused.</summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Cross-site {Method} {Path} refused")]
    public static partial void CrossSiteRequestRefused(ILogger logger, string method, PathString path);

    /// <summary>The migrations were applied when the API started.</summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Database migrations applied: {PendingCount} were pending")]
    public static partial void MigrationsApplied(ILogger logger, int pendingCount);

    /// <summary>The synthetic demo data was added to an empty database.</summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Synthetic demo data added: {CustomerCount} customers, {InvoiceCount} invoices")]
    public static partial void SyntheticDataSeeded(ILogger logger, int customerCount, int invoiceCount);

    /// <summary>The synthetic demo data was not added because the database already holds data.</summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Synthetic demo data not added: the database is not empty")]
    public static partial void SyntheticDataSkipped(ILogger logger);
}
