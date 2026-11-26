namespace FactureQr.Api.Infrastructure;

/// <summary>
/// The paths of the pages of the front end (web/). They are in French, like the
/// interface, and never overlap the English paths of the API.
/// </summary>
public static class FrontEnd
{
    /// <summary>Route patterns answered with <c>index.html</c>.</summary>
    public static IReadOnlyList<string> Pages { get; } = ["/clients/{**page}", "/factures/{**page}", "/parametres"];
}
