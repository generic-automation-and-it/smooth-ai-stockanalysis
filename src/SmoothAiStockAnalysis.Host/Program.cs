using SmoothAiStockAnalysis.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("SmoothAiStockAnalysis")
    ?? throw new InvalidOperationException("Connection string 'SmoothAiStockAnalysis' must be configured.");

builder.Services.AddInfrastructurePersistence(connectionString);

var app = builder.Build();

app.Run();

// Exposed so integration tests can target the entry point via WebApplicationFactory<Program>.
// Add composition (Serilog, OpenAPI/Scalar, health checks, AddApplication, endpoint mapping)
// here as features are implemented.
public partial class Program { }
