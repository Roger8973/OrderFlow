using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Customers.Commands.CreateCustomer;
using OrderFlow.Application.Customers.Queries.GetCustomerById;
using OrderFlow.Domain;

namespace OrderFlow.Api.Customers;

[ApiController]
[Route("customers")]
public class CustomersController(
    CreateCustomerHandler createCustomer,
    GetCustomerByIdHandler getCustomerById) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        CreateCustomerResult result;

        try
        {
            result = await createCustomer.HandleAsync(
                new CreateCustomerCommand(request.Name, request.Email, request.Phone),
                cancellationToken);
        }
        catch (DomainValidationException ex)
        {
            return ValidationProblem(new ValidationProblemDetails(ex.Errors.ToDictionary(e => e.Key, e => e.Value)));
        }

        if (result.EmailAlreadyInUse)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "A customer with this email already exists.");
        }

        var customer = result.Customer!;
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, CustomerResponse.From(customer));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var customer = await getCustomerById.HandleAsync(new GetCustomerByIdQuery(id), cancellationToken);

        if (customer is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Customer not found.");
        }

        return Ok(CustomerResponse.From(customer));
    }
}
