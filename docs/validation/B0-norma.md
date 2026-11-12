# B0 step 0: verification of the QR-bill standard (12 November 2026)

Before any code was written, the five points below were re-read on the day from the official SIX pages, the SIX PDFs and the library's repository. Each point records the source, the URL, the reading date and whether it confirms the assumption the B0 specification was built on. None of the points contradicts the specification; two details are worth knowing and are noted under "Findings".

All sources were read on **2026-11-12**.

## 1. Version of the Implementation Guidelines (IG) in force

| | |
|---|---|
| Sources | SIX QR-bill page: <https://www.six-group.com/en/products-services/banking-services/payment-standardization/standards/qr-bill.html> · IG v2.4 PDF: <https://www.six-group.com/dam/download/banking-services/standardization/qr-bill/ig-qr-bill-v2.4-en.pdf> · IG v2.3 PDF: <https://www.six-group.com/dam/download/banking-services/standardization/qr-bill/ig-qr-bill-v2.3-en.pdf> |
| Read on | 2026-11-12 |
| Result | **Confirmed.** On 12 November 2026 the version in force is **v2.3** (SIX page heading: "IG QR-bill version 2.3 (valid since 1 January 2024 resp. 21 November 2025)"). **v2.4** is dated 24 February 2026 and is "valid from 14 November 2026" (cover page); the IG states "This version will be introduced with the SIC release of 13 November 2026" and "Version 2.3 remains valid until November 2027". |
| What v2.4 changes | Quoted from the IG v2.4 "Change control": "Version 2.4 does not result in any technical adjustments for invoicing in Swiss francs (CHF). For invoicing in euros (EUR), only the combination IBAN/SCOR reference and IBAN/unstructured message is possible." The revision history lists: use of QR-IBAN and QR reference "now exclusively for orders in CHF" (chapters 2.8, 2.10, 2.12, 4.2.2, 4.3.2); Romansh as an additional correspondence language (3.2, 4.4, Annex C); adjustments due to the obligation to use the structured address (4.2.2, 5.2); the syntax of the "Billing information" line is now the responsibility of SIX (Annex D); "field" renamed "line" without substantive change. |
| Data structure | Unchanged: header `SPC` / version `0200` / coding `1`, structured address only (`AdrTp` = `S`), reference types `QRR`, `SCOR`, `NON`, trailer `EPD`. The IG notes that "only the version designation 0200 is permitted in master version 02", so the payload version does not change with v2.3 or v2.4. |
| Structured address | Since 22 November 2025 "only the structured address has been permitted in the QR-bill" (SIX page). IG v2.4 chapter 4.3.1: the elements "Street", "Building number", "Postal code", "Town" and "Country" are available; "Postal code", "Town" and "Country" must be filled in. |
| Character set | IG v2.4 chapter 4.1.1: Basic Latin (U+0020–U+007E), Latin-1 Supplement (U+00A0–U+00FF), Latin Extended-A (U+0100–U+017F), plus Ș ș Ț ț (U+0218–U+021B) and € (U+20AC); UTF-8 encoding. |

Consequence for B0: the code targets the data structure shared by v2.3 and v2.4. Because B0 is CHF only, the EUR restriction introduced by v2.4 does not apply; it is recorded in ADR-0001 for the day EUR is considered.

## 2. Style Guide

| | |
|---|---|
| Source | <https://www.six-group.com/dam/download/banking-services/standardization/qr-bill/style-guide-qr-bill-en.pdf> (linked from the SIX QR-bill page) |
| Read on | 2026-11-12 |
| Result | **Confirmed.** "Style Guide QR-bill, Version 1.1, valid from 01 January 2026" (cover). The SIX page lists it as published 19 December 2025. |
| Rules used for the visual check | Payment part with receipt 210 × 105 mm; receipt 62 × 105 mm, payment part 148 × 105 mm; Swiss QR Code printed at 46 × 46 mm with a quiet zone of at least 1.6 mm (5 mm recommended) and a 7 × 7 mm Swiss cross; permitted fonts Arial, Frutiger, Helvetica, Liberation Sans, black only; "Payment part" and "Receipt" titles 11 pt bold; headings 6–10 pt with values 2 pt larger (8 pt / 10 pt recommended on the payment part, 6 pt / 8 pt on the receipt); the grid sheet on page 24 is for checking a printout. |

## 3. Library compatibility: Codecrete.SwissQRBill.Generator

| | |
|---|---|
| Sources | NuGet: <https://www.nuget.org/packages/Codecrete.SwissQRBill.Generator> · Repository: <https://github.com/manuelbl/SwissQRBill.NET> · Releases: <https://github.com/manuelbl/SwissQRBill.NET/releases> · Source read: `Core/QRBill.cs`, `Core/Bill.cs`, `Core/Payments.cs` |
| Read on | 2026-11-12 |
| Result | **Confirmed.** Latest version **3.5.1**, published 8 September 2026, MIT licence, targets .NET Standard 2.0 (usable from .NET 10). The README states the library implements "versions 2.2 and 2.3 of the Swiss Implementation Guidelines QR-bill" (dated 20 November 2023). Release notes: v3.3.0 and v3.3.1 "ready for QR bill specification 2.3" (extended character set, combined address elements no longer allowed); v3.4.0 switches the default character set to Extended Latin and embeds a Liberation Sans font subset in PDFs; v3.5.0 faster QR generation; v3.5.1 "Force UTF-8 and medium ECC in QR code for better compatibility". |
| v2.4 | **Not mentioned** anywhere in the README, release notes, issues (search "2.4": no results) or the last 15 commits (latest: 8 September 2026). Since SIX states that v2.4 brings no technical adjustment for CHF and the payload version stays `0200`, a library ready for v2.3 is also correct for CHF bills under v2.4. The v2.4 EUR restriction (no QR-IBAN/QRR, no SCOR with message guarantee) is not enforced by the library; it is irrelevant to B0 (CHF only) and must be enforced by our own rules if EUR is ever added. |
| Verified by running | A spike on this machine with v3.5.1 and .NET SDK 10.0.114 (Linux): `QRBill.Validate` returns no errors or warnings for the IG Annex A example 2 data; `QRBill.EncodeQrCodeText` produces the 31-line payload with `SPC`/`0200`/`1` and `EPD`; `QRBill.Generate` produces a PDF (`%PDF` header) and, with the SkiaSharp Linux native asset, a PNG; `QRBill.DecodeQrCodeText` returns the same account, reference, amount and currency. |
| API used by B0 | `QRBill.Validate(Bill)`, `QRBill.EncodeQrCodeText(Bill)`, `QRBill.DecodeQrCodeText(string)`, `QRBill.Generate(Bill)`; `Payments.IsValidIban`, `Payments.IsQrIban`, `Payments.IsValidQrReference`, `Payments.IsValidIso11649Reference` are used only to cross-check our own implementation in tests. |
| Native dependency | PNG output goes through SkiaSharp 3.119.4. On Linux the package `SkiaSharp.NativeAssets.Linux.NoDependencies` (same version) must be present; PDF and SVG output need no native library. |

## 4. Available validation methods

| Method | Source | Read on | Result |
|---|---|---|---|
| (1) Library validation | `QRBill.Validate` (see 3) | 2026-11-12 | Available. Returns errors and warnings per field; B0 treats both as failures. |
| (2) Round trip | `QRBill.DecodeQrCodeText` (see 3) | 2026-11-12 | Available. B0 decodes the generated payload and compares it field by field with the input (`RoundTripTests`). |
| (3) SIX validation portal | Guide: <https://www.six-group.com/dam/download/banking-services/standardization/validation-swiss-qr-code-user-guide-en.pdf> · Portal: <https://validation.iso-payments.ch/> | 2026-11-12 | **Confirmed, with the correction already made in the specification.** The guide is still "Version 1.0, effective as of 21 April 2021" (no newer version is linked). "Registration is required in order to use the SIX Validation Portal. Users can log in … as soon as their accounts are activated." Chapter 4: "It is not possible to upload QR-bill in its entirety. If you have a complete QR-bill in a digital form, select and cut out the QR image and save it in one of the permitted image formats"; if the image fails, "the content of the QR Code needs to be transferred to .txt-file and re-validated". A green "Validation complete" means no errors; red "Validation complete" lists errors; "Validation failed" means no protocol. The portal login page offers "Neues Konto erstellen" (create a new account); no fee is mentioned in the guide or on the login page. The SIX page adds: "SIX does not offer any quality checks." |
| (4) Visual check against the Style Guide | Style Guide v1.1 (see 2) | 2026-11-12 | Available: render the payment part to PNG and compare the dimensions, sections and fonts with the Style Guide grid. |
| (5) Scan with a Swiss banking app | — | — | Optional, manual, outside CI. Cancel before paying. |

## 5. Currency in v0.1

| | |
|---|---|
| Result | **Confirmed: CHF only** (ADR-0001). IG v2.4: QR-IBAN and QR reference "can only be used for invoicing and payments in CHF"; the Creditor Reference (ISO 11649) "can only be used with an IBAN and for invoicing in CHF and EUR"; EUR bills "will be processed as SEPA Credit Transfers after November 2027 at the latest". Restricting B0 to CHF keeps all three reference variants (QRR, SCOR, none) available and avoids the EUR rules that change with v2.4. |

## Findings worth knowing

1. **IG v2.4 Annex A, example 4 contains an invalid Creditor Reference.** The example shows `RF720191230100405JSH0438`. The ISO 11649 check (move `RF72` to the end, replace letters by numbers, remainder modulo 97 must be 1) fails for this value; the correct check digits for the body `0191230100405JSH0438` are `24`. The library rejects the value for the same reason. The other IG examples (`RF18539007547034`, QR references `210000000003139471430009017` and `000008207791225857421286694`) are valid. B0 uses only the valid examples as synthetic data and keeps the invalid one as a negative test case.
2. **v2.4 enters into force two days after this reading.** Nothing in the B0 code depends on the date: the payload version is `0200` in both IG versions and the CHF rules are identical.

## Synthetic data used in B0

All IBANs, names and addresses in the samples and tests come from the IG v2.4 Annex A examples (sample company "Max Muster & Söhne", "Muster Stiftung", "Simon Muster", "Sarah Beispiel", Musterstrasse, 8000 Seldwyla, 3001 Bern) and are fictitious by construction:

| Account | Kind | IG example |
|---|---|---|
| `CH44 3199 9123 0008 8901 2` | QR-IBAN (QR-IID 31999) | Annex A, example 2; Style Guide page 13 |
| `CH58 0079 1123 0008 8901 2` | IBAN | Annex A, examples 4–6 |
| `CH52 0483 5012 3456 7100 0` | IBAN | Annex A, example 3 |
| `CH64 3196 1000 0044 2155 7` | QR-IBAN (QR-IID 31961) | Annex A, example 1 |

No real IBAN, name or address appears in this repository.
