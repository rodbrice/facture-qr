# Facture QR

Invoicing with Swiss QR-bills for independents, in .NET.

A QR-bill is the only payment slip accepted in Switzerland and Liechtenstein since 30 September 2022. Producing one correctly means following the SIX Implementation Guidelines: structured addresses, a QR-IBAN with a QR reference or an IBAN with an ISO 11649 Creditor Reference, a precise 31-line payload inside the Swiss QR Code, and a fixed 210 × 105 mm layout. This repository is being built in small, verified steps, starting with that core.

**Status:** first unit (B0) in progress. The repository contains the QR-bill core library, its tests, three synthetic samples and the quality baseline (build with warnings as errors, formatting check, continuous integration). The samples pass the library's validation, a round trip through the decoder and an independent QR decoder; validation in the SIX portal is still pending. There is no application yet: no database, no API and no user interface. The planned next steps are listed at the end.

A French summary follows the English text ([Résumé en français](#résumé-en-français)).

## What the core does today

`src/FactureQr.Core` is a small library with no infrastructure dependencies:

| Type | Responsibility |
|---|---|
| `Iban` | Swiss or Liechtenstein IBAN (21 characters, modulo 97 check) and QR-IBAN detection (QR-IID 30000–31999). |
| `StructuredAddress` | Name, street, building number, postal code, town, country, with the lengths and the character set of the standard. Combined address lines are not accepted (mandatory since 22 November 2025). |
| `PaymentReference` | QR reference (27 digits, modulo 10 recursive check digit), Creditor Reference (ISO 11649, modulo 97-10 check digits) or none; creation and validation of both. |
| `QrBillRequest` | One payment part in CHF: creditor, account, optional amount, optional debtor, reference, optional message, with the rules that tie them together (a QR-IBAN requires a QR reference; a plain IBAN excludes it; CHF only). |
| `QrBillRenderer` | Validates a request, then produces the Swiss QR Code text, the payment part with receipt as PDF or PNG (French headings), or the QR code alone as PNG; decodes QR code text back into a request. |

Rendering uses [SwissQRBill.NET](https://github.com/manuelbl/SwissQRBill.NET) (MIT) behind a single class; see [ADR-0002](docs/adr/0002-qr-bill-library.md).

## Samples

Three synthetic bills, one per reference variant, are under [`docs/samples`](docs/samples): the PDF, a PNG preview, the QR code alone (`.qr.png`) and the QR code text (`.qr.txt`). All names, addresses and accounts come from the examples in Annex A of the SIX Implementation Guidelines and are fictitious. Each sample passes the library's validation, the round trip through the decoder and an independent QR decoder; their validation in the SIX portal is pending (see `docs/validation/B0.md`).

| Sample | Account | Reference |
|---|---|---|
| a | QR-IBAN | QR reference (QRR) |
| b | IBAN | Creditor Reference (SCOR) |
| c | IBAN | none |

![Sample a: payment part with receipt](docs/samples/sample-a.png)

## Build and test

Requirements: .NET SDK 10 (see `global.json`). On Linux, PNG rendering also needs the SkiaSharp native library, which the test and tool projects reference as a NuGet package.

```sh
dotnet restore
dotnet build --configuration Release -warnaserror
dotnet format --verify-no-changes
dotnet test --configuration Release
```

To regenerate the sample files:

```sh
dotnet run --project tools/FactureQr.Samples -- docs/samples
```

The same four commands run in GitHub Actions on every push and pull request (`.github/workflows/ci.yml`).

## How correctness is checked

The standard is regulated and SIX offers no quality check of its own, so the repository keeps its evidence:

- [`docs/validation/B0-norma.md`](docs/validation/B0-norma.md): the version of the Implementation Guidelines in force, the Style Guide, the library's compatibility and the available validation methods, each with source, URL and reading date.
- [`docs/validation/B0.md`](docs/validation/B0.md): the commands run, their results, the independent checks (golden payloads written by hand from the standard, round trip through the decoder, a second QR decoder, geometry of the rendered payment part against the Style Guide) and the protocol for the SIX validation portal.
- Tests: `PaymentReferenceTests`, `StructuredAddressTests`, `IbanTests`, `QrBillRequestTests`, `CurrencyRuleTests`, `QrPayloadGoldenTests`, `RoundTripTests`, `RenderingSmokeTests`.

## Design decisions

- [ADR-0001: Scope of the first unit and non-goals](docs/adr/0001-scope-and-non-goals.md) (CHF only, no VAT, no application yet).
- [ADR-0002: Use SwissQRBill.NET to render the payment part](docs/adr/0002-qr-bill-library.md).

## Planned next steps

1. Invoice model (customers, lines, totals, sequential numbering per year, issued invoices become immutable) with PostgreSQL.
2. HTTP API and a React front end, served together, with a `docker compose` demo.
3. Import of camt.054 bank statements and automatic reconciliation by reference.

Nothing here is a commitment to dates; each step is delivered when it is verified.

## Licence

MIT, see [`LICENSE`](LICENSE). Third-party licences are listed in [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).

---

## Résumé en français

**Facture QR** est un outil de facturation avec QR-facture pour les indépendants en Suisse, écrit en .NET. La QR-facture est le seul bulletin de versement accepté en Suisse et au Liechtenstein depuis le 30 septembre 2022 ; la produire correctement suppose de respecter les Implementation Guidelines de SIX (adresse structurée, QR-IBAN avec référence QR ou IBAN avec référence de créancier ISO 11649, charge utile de 31 lignes dans le Swiss QR Code, mise en page fixe de 210 × 105 mm).

**État :** première unité (B0) en cours. Le dépôt contient la bibliothèque de base (`FactureQr.Core`), ses tests, trois exemples synthétiques et la base de qualité (compilation avec avertissements traités comme des erreurs, vérification du formatage, intégration continue). Les exemples passent la validation de la bibliothèque, un aller-retour par le décodeur et un décodeur QR indépendant ; la validation sur le portail SIX est encore en attente. Il n'y a pas encore d'application : ni base de données, ni API, ni interface. Les étapes suivantes sont le modèle de facture avec PostgreSQL, l'API et l'interface React avec une démo `docker compose`, puis l'import des relevés camt.054 pour le rapprochement automatique des paiements.

**Vérification :** la version en vigueur de la norme, la compatibilité de la bibliothèque et les méthodes de validation sont consignées dans `docs/validation/B0-norma.md` ; les commandes exécutées, leurs résultats et le protocole du portail de validation SIX sont dans `docs/validation/B0.md`. Seule la devise CHF est acceptée dans cette version (ADR-0001). Toutes les données d'exemple sont fictives et proviennent de l'annexe A des Implementation Guidelines.

**Compiler et tester :** SDK .NET 10, puis `dotnet build --configuration Release -warnaserror`, `dotnet format --verify-no-changes` et `dotnet test --configuration Release`. Licence MIT.
