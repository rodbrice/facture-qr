using System.Text;
using FactureQr.Core.Billing;
using FactureQr.Core.Samples;

// Regenerates docs/samples from the synthetic samples in FactureQr.Core:
//   dotnet run --project tools/FactureQr.Samples -- docs/samples
// For each sample: the PDF (payment part with receipt), a PNG preview of it,
// the QR code alone as PNG (what the SIX validation portal accepts) and the
// QR code text (the alternative the portal accepts as a .txt file).
string outputDirectory = Path.GetFullPath(args.Length > 0 ? args[0] : "docs/samples");
Directory.CreateDirectory(outputDirectory);

foreach ((string id, QrBillRequest request) in SyntheticSamples.All)
{
    string basePath = Path.Combine(outputDirectory, $"sample-{id}");
    File.WriteAllBytes(basePath + ".pdf", QrBillRenderer.RenderPdf(request));
    File.WriteAllBytes(basePath + ".png", QrBillRenderer.RenderPng(request, resolutionDpi: 200));
    File.WriteAllBytes(basePath + ".qr.png", QrBillRenderer.RenderQrCodePng(request, resolutionDpi: 300));
    File.WriteAllText(basePath + ".qr.txt", QrBillRenderer.EncodeQrCodeText(request), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    Console.WriteLine($"sample-{id}: {request.Account.Formatted} {request.Reference} {request.Currency} {request.Amount}");
}

Console.WriteLine($"Written to {outputDirectory}");
