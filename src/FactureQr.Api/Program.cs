using System.Text.Json.Serialization;
using FactureQr.Api.Customers;
using FactureQr.Api.Infrastructure;
using FactureQr.Api.Invoices;
using FactureQr.Persistence;
using Microsoft.AspNetCore.HttpLogging;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Logs are written as one JSON object per line, with the message template's values as separate properties.
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
});

// One log entry per request: method, path, status code and duration. No headers or bodies (they carry addresses).
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestMethod | HttpLoggingFields.RequestPath | HttpLoggingFields.ResponseStatusCode | HttpLoggingFields.Duration;
    options.CombineLogs = true;
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    // A missing or null value for a non-nullable property is a 400, not a silent default.
    options.SerializerOptions.RespectNullableAnnotations = true;
    options.SerializerOptions.RespectRequiredConstructorParameters = true;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<SwissCalendar>();
builder.Services.AddSingleton(services => InvoicingSettings.Load(services.GetRequiredService<IConfiguration>()));
builder.Services.AddScoped(services => new FactureQrDbContext(FactureQrDbContext.CreateOptions(ConnectionString(services))));
builder.Services.AddScoped<InvoiceIssuer>();

WebApplication app = builder.Build();

// Fail at startup, not at the first PDF, when the creditor is missing or invalid.
app.Services.GetRequiredService<InvoicingSettings>();

app.UseHttpLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapCustomerEndpoints();
app.MapInvoiceEndpoints();

app.Run();

static string ConnectionString(IServiceProvider services) =>
    services.GetRequiredService<IConfiguration>().GetConnectionString("FactureQr")
    ?? throw new InvalidOperationException("The connection string 'ConnectionStrings:FactureQr' is not configured.");

/// <summary>The application entry point; public so that the integration tests can start the API in memory.</summary>
public partial class Program;
