# Third-party notices

Facture QR is released under the MIT licence (see `LICENSE`). It depends on the following third-party software, whose licences are reproduced below.

## SwissQRBill.NET (Codecrete.SwissQRBill.Generator, Codecrete.SwissQRBill.Core)

<https://github.com/manuelbl/SwissQRBill.NET> — MIT License

```
MIT License

Copyright (c) 2018 Manuel Bleichenbacher

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## QR Code generator (Net.Codecrete.QrCodeGenerator)

<https://github.com/manuelbl/QrCodeGenerator> — MIT License, Copyright (c) 2018 Manuel Bleichenbacher. Same licence text as above.

## SkiaSharp (SkiaSharp, SkiaSharp.NativeAssets.*)

<https://github.com/mono/SkiaSharp> — MIT License

```
Copyright (c) 2015-2016 Xamarin, Inc.
Copyright (c) 2017-2018 Microsoft Corporation.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
```

## Entity Framework Core (Microsoft.EntityFrameworkCore.*)

<https://github.com/dotnet/efcore> — MIT License, © Microsoft Corporation. Same licence text as above, with that copyright line.

## Npgsql and its EF Core provider (Npgsql, Npgsql.EntityFrameworkCore.PostgreSQL)

<https://github.com/npgsql/npgsql>, <https://github.com/npgsql/efcore.pg> — PostgreSQL License, © The Npgsql Development Team: <https://github.com/npgsql/efcore.pg/blob/main/LICENSE>.

## EFCore.NamingConventions

<https://github.com/efcore/EFCore.NamingConventions> — Apache License 2.0, © Shay Rojansky: <https://www.apache.org/licenses/LICENSE-2.0>.

## Testcontainers for .NET (Testcontainers, Testcontainers.PostgreSql; tests only)

<https://github.com/testcontainers/testcontainers-dotnet> — MIT License, © Andre Hofmeister and other authors. Same licence text as above, with that copyright line. Used only by the test project and not distributed with the library.

## ASP.NET Core and its OpenAPI support (Microsoft.AspNetCore.OpenApi, Microsoft.AspNetCore.Mvc.Testing)

<https://github.com/dotnet/aspnetcore> — MIT License, © .NET Foundation and Contributors. Same licence text as above, with that copyright line. `Microsoft.AspNetCore.Mvc.Testing` is used only by the API tests.

## PdfPig (tests only)

<https://github.com/UglyToad/PdfPig> — Apache License 2.0: <https://www.apache.org/licenses/LICENSE-2.0>. Used only by the API tests, to read the text and the drawing of the served PDF.

## ZXing.Net (tests only)

<https://github.com/micjahn/ZXing.Net> — Apache License 2.0: <https://www.apache.org/licenses/LICENSE-2.0>. Used only by the API tests, to decode the QR code redrawn from the served PDF.

## React, React DOM and scheduler (front end)

<https://github.com/facebook/react> — MIT License, Copyright (c) Meta Platforms, Inc. and affiliates. Same licence text as above, with that copyright line. Bundled into the built front end.

## React Router (react-router, with @remix-run/route-pattern and cookie-es; front end)

<https://github.com/remix-run/react-router> — MIT License, Copyright (c) React Training LLC 2015-2019, Copyright (c) Remix Software Inc. 2020-2021, Copyright (c) Shopify Inc. 2022-2023. Its dependencies <https://github.com/remix-run/remix> (`@remix-run/route-pattern`, Copyright (c) 2025 Shopify Inc.) and <https://github.com/unjs/cookie-es> (Copyright (c) Pooya Parsa, Copyright (c) 2012-2014 Roman Shtylman) are also MIT. Same licence text as above, with those copyright lines. Bundled into the built front end.

## Front-end build and test tools (not distributed)

Used only to build, check and test `web/`, and not part of the built front end: Vite and @vitejs/plugin-react (MIT), Vitest (MIT), Testing Library (MIT), jsdom (MIT), oxlint (MIT), TypeScript (Apache License 2.0), Playwright (Apache License 2.0). The full list with versions is in `web/package-lock.json`.

## Liberation Sans (font subset embedded in generated PDFs)

SwissQRBill.NET embeds a subset of the Liberation Sans font in the PDF files it produces. Liberation Fonts are licensed under the SIL Open Font License, Version 1.1: <https://github.com/liberationfonts/liberation-fonts/blob/main/LICENSE>.

## QR-bill specifications

The QR-bill standard (Implementation Guidelines and Style Guide) is published by SIX Interbank Clearing Ltd: <https://www.six-group.com/en/products-services/banking-services/payment-standardization/standards/qr-bill.html>. The synthetic sample data in this repository is taken from the examples in Annex A of the Implementation Guidelines.
