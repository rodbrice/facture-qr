using Microsoft.AspNetCore.Http.HttpResults;

namespace FactureQr.Api.Infrastructure;

/// <summary>The error responses of the API, all with a ProblemDetails body (RFC 9457).</summary>
public static class Problems
{
    /// <summary>400: one field of the request is invalid.</summary>
    public static ValidationProblem Invalid(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });

    /// <summary>404: the resource named in the URL does not exist.</summary>
    public static ProblemHttpResult NotFound(string detail) =>
        TypedResults.Problem(detail, statusCode: StatusCodes.Status404NotFound, title: "Not Found");

    /// <summary>409: the request is valid, but the current state does not allow it.</summary>
    public static ProblemHttpResult Conflict(string detail) =>
        TypedResults.Problem(detail, statusCode: StatusCodes.Status409Conflict, title: "Conflict");

    /// <summary>
    /// The JSON name of a field reported by the QR-bill rules, for example
    /// "Postal code" becomes "postalCode".
    /// </summary>
    public static string JsonName(string field)
    {
        string[] words = (field ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return string.Empty;
        }

        return string.Concat(words.Select((w, i) => i == 0 ? w.ToLowerInvariant() : char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
    }
}
