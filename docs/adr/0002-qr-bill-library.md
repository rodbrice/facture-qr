# ADR-0002: Use SwissQRBill.NET to render the payment part

**Status:** Accepted · **Date:** 2026-11-12

## Context

A QR-bill payment part has three layers: the data (31 lines of text with precise rules), the Swiss QR Code (ISO 18004 with error correction level M and a Swiss cross overlay) and the printed layout (210 × 105 mm, fixed sections, permitted fonts and sizes, five languages). Implementing all three from scratch is possible but would be the bulk of the project and would duplicate work that has been done and reviewed in the open for years.

## Options considered

| Option | Assessment |
|---|---|
| **SwissQRBill.NET** (`Codecrete.SwissQRBill.Generator`) | MIT, maintained since 2018, 3.5.1 released on 8 September 2026, declares support for IG 2.2 and 2.3, Extended Latin character set by default, PDF with an embedded font subset, SVG and PNG output, decoder for the QR text, validator with per-field messages. Only dependency for PDF output is the author's QR code generator; PNG output needs SkiaSharp (native library). |
| Own implementation of data + QR code + PDF | Full control, no dependency. Estimated at several times the size of the rest of B0, with the layout rules (fonts, sizes, five languages, scissors line, online variant) as the main cost. Rejected for the first version; kept as the fallback if the library is abandoned. |
| Generic PDF library (for example QuestPDF) + own QR code | Still requires implementing the data rules and the layout; QuestPDF's licence is free only below a revenue threshold. Rejected. |

## Decision

Use `Codecrete.SwissQRBill.Generator` 3.5.1, pinned in `FactureQr.Core.csproj`, behind a single class, `QrBillRenderer`. Our own code owns the domain rules that matter for the business (IBAN and QR-IBAN detection, modulo 10 recursive and ISO 11649 check digits, structured address fields, the links between account kind and reference type, CHF only) and validates them before the library is called. The library is then asked to validate again, and any warning (a value it would clip or clean) is treated as an error. The one cleaning the library performs without a warning, collapsing runs of spaces into one, is applied by our own validation first (`SpsCharacters.Validate`), so the printed bill always equals the validated input.

Compatibility with the standard in force was verified on 12 November 2026 and is recorded in `docs/validation/B0-norma.md`: IG v2.4 (valid from 14 November 2026) brings no technical change for CHF, the QR payload version stays `0200`, and the library's v2.3 support therefore covers CHF bills under v2.4. The library does not enforce the EUR restrictions of v2.4; this does not matter while the tool is CHF only (ADR-0001).

## Consequences

- One file (`QrBillRenderer.cs`) maps our model to the library's; replacing the library means rewriting that file and keeping the tests.
- The tests cross-check our check-digit implementations against the library's on hundreds of generated references, so a divergence in either would surface immediately.
- PNG output (used for the QR image accepted by the SIX validation portal and for previews) requires the SkiaSharp native library. On Linux the package `SkiaSharp.NativeAssets.Linux.NoDependencies` is referenced by the test and tool projects; PDF output has no native dependency.
- Licences of the library and its dependencies are reproduced in `THIRD-PARTY-NOTICES.md`.
