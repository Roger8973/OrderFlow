using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Customers.Commands.CreateCustomer;
using OrderFlow.Domain;

namespace OrderFlow.Api.Customers;

[ApiController]
[Route("customers")]
public class CustomersController(CreateCustomerHandler createCustomer) : ControllerBase
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

        // No Location header: there is no GET /customers/{id} yet.
        return StatusCode(StatusCodes.Status201Created, CustomerResponse.From(result.Customer!));
    }
}
