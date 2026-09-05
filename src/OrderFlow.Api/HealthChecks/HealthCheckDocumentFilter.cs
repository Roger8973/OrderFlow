using System.Net.Http;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OrderFlow.Api.HealthChecks;

/// <summary>
/// MapHealthChecks endpoints don't participate in ApiExplorer, so Swashbuckle never
/// discovers them on its own; this filter adds /health to the generated document by hand.
/// </summary>
public sealed class HealthCheckDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Paths.Add("/health", new OpenApiPathItem
        {
            Operations = new Dictionary<HttpMethod, OpenApiOperation>
            {
                [HttpMethod.Get] = new OpenApiOperation
                {
                    Summary = "Reports API and PostgreSQL health.",
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse { Description = "Healthy" },
                        ["503"] = new OpenApiResponse { Description = "Unhealthy" },
                    },
                },
            },
        });
    }
}
