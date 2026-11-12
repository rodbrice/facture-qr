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

    /// <summary>A customer's address was changed.</summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Customer {CustomerId} changed")]
    public static partial void CustomerChanged(ILogger logger, Guid customerId);

    /// <summary>A draft was created.</summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Invoice {InvoiceId} created for customer {CustomerId}")]
    public static partial void InvoiceCreated(ILogger logger, Guid invoiceId, Guid customerId);

    /// <summary>A draft was issued.</summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Invoice {InvoiceId} issued as {InvoiceNumber}")]
    public static partial void InvoiceIssued(ILogger logger, Guid invoiceId, InvoiceNumber invoiceNumber);

    /// <summary>An issued invoice was marked as paid.</summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Invoice {InvoiceNumber} marked as paid on {PaidOn}")]
    public static partial void InvoicePaid(ILogger logger, InvoiceNumber? invoiceNumber, DateOnly paidOn);
}
