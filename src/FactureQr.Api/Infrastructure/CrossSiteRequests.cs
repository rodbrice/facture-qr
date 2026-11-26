namespace FactureQr.Api.Infrastructure;

/// <summary>
/// Refuses, with 403, a request that would change data when the browser says it
/// comes from another site (ADR-0007). The API has no authentication, and
/// <c>POST /invoices/{id}/issue</c> and <c>POST /invoices/{id}/mark-paid</c>
/// accept an empty body, which any page open in the same browser could send to
/// <c>http://localhost</c> without a CORS preflight.
/// <para>
/// Browsers send <c>Sec-Fetch-Site</c> with every request: <c>same-origin</c>
/// for the front end served by the API, <c>none</c> when the user typed the
/// address. Any other value (<c>same-site</c>, <c>cross-site</c>) on a method
/// other than GET, HEAD or OPTIONS is refused. Requests without the header
/// (curl, scripts, the tests) are not browser requests and pass.
/// </para>
/// </summary>
public static class CrossSiteRequests
{
    private const string LogCategory = "FactureQr.Api.CrossSiteRequests";

    /// <summary>Adds the check to the request pipeline.</summary>
    public static IApplicationBuilder UseCrossSiteRequestRefusal(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (IsRefused(context.Request))
            {
                ILogger logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(LogCategory);
                ApiLog.CrossSiteRequestRefused(logger, context.Request.Method, context.Request.Path);
                await TypedResults.Problem(
                    "Requests that change data are accepted only from the pages of this application.",
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden").ExecuteAsync(context);
                return;
            }

            await next(context);
        });

    /// <summary>True when the request changes data and the browser reports another site as its origin.</summary>
    public static bool IsRefused(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method))
        {
            return false;
        }

        string? site = request.Headers["Sec-Fetch-Site"];
        return site is not null && site is not ("same-origin" or "none");
    }
}
