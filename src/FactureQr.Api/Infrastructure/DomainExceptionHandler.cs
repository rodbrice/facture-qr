using FactureQr.Core.Invoicing;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace FactureQr.Api.Infrastructure;

/// <summary>
/// Turns the exceptions that mean "the request is valid, but the current state
/// of the invoice does not allow it" into a 409 Conflict with a ProblemDetails
/// body: a rule of the invoice model (<see cref="InvoiceRuleException"/>, for
/// example issuing an invoice twice) or a concurrent change detected by the
/// database (<see cref="DbUpdateConcurrencyException"/>). Errors in the request
/// itself are answered with a 400 by the endpoints before the model is changed;
/// anything else is left to the default handler (500, without details).
/// </summary>
public sealed partial class DomainExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService problemDetails;
    private readonly ILogger<DomainExceptionHandler> logger;

    /// <summary>Creates the handler.</summary>
    public DomainExceptionHandler(IProblemDetailsService problemDetails, ILogger<DomainExceptionHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(problemDetails);
        ArgumentNullException.ThrowIfNull(logger);

        this.problemDetails = problemDetails;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        string? detail = exception switch
        {
            InvoiceRuleException rule => rule.Message,
            DbUpdateConcurrencyException => "The invoice was changed by another request at the same time. Read it again and retry.",
            _ => null,
        };

        if (detail is null)
        {
            return false;
        }

        LogConflict(logger, detail);
        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = StatusCodes.Status409Conflict, Title = "Conflict", Detail = detail },
        });
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Request refused with 409: {Reason}")]
    private static partial void LogConflict(ILogger logger, string reason);
}
