using OrderFlow.Api.ExceptionHandling;
using OrderFlow.Api.HealthChecks;
using OrderFlow.Infrastructure.Database;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .WriteTo.Console(new CompactJsonFormatter()));

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.DocumentFilter<HealthCheckDocumentFilter>());

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")!, name: "postgresql");

try
{
    DatabaseMigrator.Migrate(connectionString);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Database migration failed: {ex}");
    Environment.Exit(1);
}

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteAsync,
})
    .WithMetadata(new Microsoft.AspNetCore.Routing.HttpMethodMetadata(["GET"]))
    .AllowAnonymous();

app.Run();

public partial class Program;
