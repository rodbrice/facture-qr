using FactureQr.Core.Billing;

namespace FactureQr.Core.Tests;

public class StructuredAddressTests
{
    [Fact]
    public void Accepts_the_guidelines_example_and_trims_values()
    {
        // IG v2.4 chapter 4.3.1, Table 9.
        var address = new StructuredAddress(" Pia-Maria Rutschmann-Schnyder ", "Grosse Marktgasse ", " 28", "9400", "Rorschach", "ch");

        Assert.Equal("Pia-Maria Rutschmann-Schnyder", address.Name);
        Assert.Equal("Grosse Marktgasse", address.Street);
        Assert.Equal("28", address.BuildingNumber);
        Assert.Equal("9400", address.PostalCode);
        Assert.Equal("Rorschach", address.Town);
        Assert.Equal("CH", address.Country);
    }

    [Fact]
    public void Collapses_runs_of_spaces_into_one_as_the_printed_bill_does()
    {
        var address = new StructuredAddress("Max  Muster", "Grosse   Marktgasse", "28", "9400", "Rorschach", "CH");

        Assert.Equal("Max Muster", address.Name);
        Assert.Equal("Grosse Marktgasse", address.Street);
    }

    [Fact]
    public void Street_and_building_number_are_optional()
    {
        var address = new StructuredAddress("Muster Stiftung", null, "  ", "3001", "Bern", "CH");

        Assert.Null(address.Street);
        Assert.Null(address.BuildingNumber);
    }

    [Theory]
    [InlineData("", "Musterstrasse", "1", "8000", "Seldwyla", "CH", "Name")]
    [InlineData("Simon Muster", "Musterstrasse", "1", "", "Seldwyla", "CH", "Postal code")]
    [InlineData("Simon Muster", "Musterstrasse", "1", "8000", " ", "CH", "Town")]
    [InlineData("Simon Muster", "Musterstrasse", "1", "8000", "Seldwyla", "", "Country")]
    [InlineData("Simon Muster", "Musterstrasse", "1", "8000", "Seldwyla", "CHE", "Country")]
    [InlineData("Simon Muster", "Musterstrasse", "1", "8000", "Seldwyla", "C1", "Country")]
    public void Rejects_missing_required_fields(string name, string? street, string? number, string postalCode, string town, string country, string expectedField)
    {
        var e = Assert.Throws<QrBillValidationException>(() => new StructuredAddress(name, street, number, postalCode, town, country));

        Assert.Equal(expectedField, e.Field);
    }

    [Theory]
    [InlineData(70, 70, 16, 16, 35, true)]
    [InlineData(71, 70, 16, 16, 35, false)]
    [InlineData(70, 71, 16, 16, 35, false)]
    [InlineData(70, 70, 17, 16, 35, false)]
    [InlineData(70, 70, 16, 17, 35, false)]
    [InlineData(70, 70, 16, 16, 36, false)]
    public void Enforces_maximum_lengths(int name, int street, int number, int postalCode, int town, bool valid)
    {
        StructuredAddress Create() => new(new string('N', name), new string('S', street), new string('1', number), new string('9', postalCode), new string('T', town), "CH");

        if (valid)
        {
            Assert.NotNull(Create());
        }
        else
        {
            Assert.Throws<QrBillValidationException>(Create);
        }
    }

    [Theory]
    [InlineData("Max Muster & Söhne")]       // Latin-1 Supplement
    [InlineData("Łukasz Žídek")]             // Latin Extended-A
    [InlineData("Ștefan Țărmure")]           // Ș ș Ț ț
    [InlineData("Prix en € et CHF")]         // euro sign
    [InlineData("Pia-Maria O'Neill / Co.")]  // Basic Latin punctuation
    public void Accepts_the_extended_latin_character_set(string name)
    {
        Assert.Equal(name, new StructuredAddress(name, null, null, "8000", "Seldwyla", "CH").Name);
    }

    [Theory]
    [InlineData("Иван Петров")]   // Cyrillic
    [InlineData("山田 太郎")]      // CJK
    [InlineData("Max 😀")]        // emoji (outside the Basic Multilingual Plane)
    [InlineData("Ⅻ")]            // Number Forms
    [InlineData("Tab\there")]     // control character
    public void Rejects_characters_outside_the_permitted_set(string name)
    {
        var e = Assert.Throws<QrBillValidationException>(() => new StructuredAddress(name, null, null, "8000", "Seldwyla", "CH"));

        Assert.Contains("permitted character set", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Addresses_with_the_same_values_are_equal()
    {
        var one = new StructuredAddress("Simon Muster", "Musterstrasse", "1", "8000", "Seldwyla", "CH");
        var two = new StructuredAddress("Simon Muster", "Musterstrasse", "1", "8000", "Seldwyla", "ch");

        Assert.Equal(one, two);
    }
}
