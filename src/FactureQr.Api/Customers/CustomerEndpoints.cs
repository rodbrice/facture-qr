using FactureQr.Api.Infrastructure;
using FactureQr.Core.Billing;
using FactureQr.Core.Invoicing;
using FactureQr.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace FactureQr.Api.Customers;

/// <summary>The <c>/customers</c> endpoints.</summary>
public static class CustomerEndpoints
{
    private const string LogCategory = "FactureQr.Api.Customers";

    /// <summary>Maps the endpoints.</summary>
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/customers").WithTags("Customers");

        group.MapPost("/", Create)
            .WithSummary("Creates a customer.")
            .ProducesValidationProblem();

        group.MapGet("/", List)
            .WithSummary("Lists the customers by name.");

        group.MapGet("/{id:guid}", Get)
            .WithSummary("Returns one customer.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Results<Created<CustomerResponse>, ValidationProblem>> Create(
        AddressDto address, FactureQrDbContext db, ILoggerFactory loggers, CancellationToken cancellationToken)
    {
        Customer customer;
        try
        {
            customer = new Customer(address.ToStructuredAddress());
        }
        catch (QrBillValidationException e)
        {
            return Problems.Invalid(Problems.JsonName(e.Field), e.Message);
        }

        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);
        ILogger logger = loggers.CreateLogger(LogCategory);
        ApiLog.CustomerCreated(logger, customer.Id);

        return TypedResults.Created($"/customers/{customer.Id}", CustomerResponse.From(customer));
    }

    private static async Task<Ok<List<CustomerResponse>>> List(FactureQrDbContext db, CancellationToken cancellationToken)
    {
        List<Customer> customers = await db.Customers.AsNoTracking()
            .OrderBy(c => c.Address.Name).ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);
        return TypedResults.Ok(customers.Select(CustomerResponse.From).ToList());
    }

    private static async Task<Results<Ok<CustomerResponse>, ProblemHttpResult>> Get(Guid id, FactureQrDbContext db, CancellationToken cancellationToken)
    {
        Customer? customer = await db.Customers.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        return customer is null
            ? Problems.NotFound($"No customer with the identifier {id}.")
            : TypedResults.Ok(CustomerResponse.From(customer));
    }
}
