using FactureQr.Core.Billing;

namespace FactureQr.Core.Invoicing;

/// <summary>
/// The independent who issues the invoices: the account that receives the
/// payments and the structured address printed as the creditor of the
/// payment part. The address must be the one of the account holder.
/// </summary>
/// <param name="Account">The IBAN or QR-IBAN that receives the payments.</param>
/// <param name="Address">The structured address of the account holder.</param>
public sealed record CreditorAccount(Iban Account, StructuredAddress Address);
