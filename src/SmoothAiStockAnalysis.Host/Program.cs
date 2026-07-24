using SmoothAiStockAnalysis.Application.Extensions;
using SmoothAiStockAnalysis.Host.Configuration;
using SmoothAiStockAnalysis.Host.Extensions;
using SmoothAiStockAnalysis.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

DefaultUserOptions defaultUser = DefaultUserOptions.FromConfiguration(builder.Configuration);
Guid defaultUserUniqueIdentifier = defaultUser.GetValidatedUniqueIdentifier();

builder.Services.AddInfrastructure(defaultUserUniqueIdentifier);
builder.Services.AddConfiguration(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddSingleton<NodaTime.IClock>(NodaTime.SystemClock.Instance);

var app = builder.Build();

app.Run();

// Exposed so integration tests can target the entry point via WebApplicationFactory<Program>.
// Add composition (Serilog, OpenAPI/Scalar, health checks, endpoint mapping) here as features
// are implemented.
public partial class Program { }
