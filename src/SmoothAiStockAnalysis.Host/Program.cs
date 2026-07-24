using NodaTime;
using SmoothAiStockAnalysis.Domain.Time;
using SmoothAiStockAnalysis.Host.Configuration;
using SmoothAiStockAnalysis.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

DefaultUserOptions defaultUser = DefaultUserOptions.FromConfiguration(builder.Configuration);
Guid defaultUserUniqueIdentifier = defaultUser.GetValidatedUniqueIdentifier();

builder.Services.AddInfrastructure(defaultUserUniqueIdentifier);
builder.Services.AddSingleton<IClock>(SystemClock.Instance);

var deliveryWindow = DeliveryWindowOptions.FromConfiguration(builder.Configuration).ToDeliveryWindow();
builder.Services.AddSingleton(deliveryWindow);

var app = builder.Build();

app.Run();

// Exposed so integration tests can target the entry point via WebApplicationFactory<Program>.
// Add composition (Serilog, OpenAPI/Scalar, health checks, AddApplication, endpoint mapping)
// here as features are implemented.
public partial class Program { }
