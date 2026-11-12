namespace FactureQr.Core.Billing;

/// <summary>
/// A structured address as required in every QR-bill since 22 November 2025
/// (Implementation Guidelines v2.4, chapters 4.2.2 and 4.3.1): name, optional
/// street and building number, postal code, town and ISO 3166-1 country code.
/// Combined address lines are no longer permitted.
/// </summary>
public sealed record StructuredAddress
{
    /// <summary>Maximum length of the name (first name + last name, or company name).</summary>
    public const int MaxNameLength = 70;

    /// <summary>Maximum length of the street or P.O. box.</summary>
    public const int MaxStreetLength = 70;

    /// <summary>Maximum length of the building number.</summary>
    public const int MaxBuildingNumberLength = 16;

    /// <summary>Maximum length of the postal code (without country prefix).</summary>
    public const int MaxPostalCodeLength = 16;

    /// <summary>Maximum length of the town.</summary>
    public const int MaxTownLength = 35;

    /// <summary>Creates and validates a structured address.</summary>
    /// <param name="name">Name of the person or company (required, at most 70 characters).</param>
    /// <param name="street">Street or P.O. box (optional, at most 70 characters).</param>
    /// <param name="buildingNumber">Building number (optional, at most 16 characters).</param>
    /// <param name="postalCode">Postal code without country prefix (required, at most 16 characters).</param>
    /// <param name="town">Town (required, at most 35 characters).</param>
    /// <param name="country">Two-letter ISO 3166-1 country code (required).</param>
    /// <exception cref="QrBillValidationException">A field is missing, too long or uses a character outside the permitted set.</exception>
    public StructuredAddress(string name, string? street, string? buildingNumber, string postalCode, string town, string country)
    {
        Name = SpsCharacters.Validate("Name", name, MaxNameLength, required: true)!;
        Street = SpsCharacters.Validate("Street", street, MaxStreetLength, required: false);
        BuildingNumber = SpsCharacters.Validate("Building number", buildingNumber, MaxBuildingNumberLength, required: false);
        PostalCode = SpsCharacters.Validate("Postal code", postalCode, MaxPostalCodeLength, required: true)!;
        Town = SpsCharacters.Validate("Town", town, MaxTownLength, required: true)!;
        Country = ValidateCountry(country);
    }

    /// <summary>Name of the person or company.</summary>
    public string Name { get; }

    /// <summary>Street or P.O. box, or null.</summary>
    public string? Street { get; }

    /// <summary>Building number, or null.</summary>
    public string? BuildingNumber { get; }

    /// <summary>Postal code.</summary>
    public string PostalCode { get; }

    /// <summary>Town.</summary>
    public string Town { get; }

    /// <summary>Two-letter ISO 3166-1 country code, upper case.</summary>
    public string Country { get; }

    private static string ValidateCountry(string? country)
    {
        string value = (country ?? string.Empty).Trim().ToUpperInvariant();
        if (value.Length != 2 || !value.All(char.IsAsciiLetterUpper))
        {
            throw new QrBillValidationException("Country", "a two-letter ISO 3166-1 country code is required (for example CH).");
        }

        return value;
    }
}
