using NodaTime;
using SmoothAiStockAnalysis.Domain.Time;
using SmoothAiStockAnalysis.Host.Configuration;
using SmoothAiStockAnalysis.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure();
builder.Services.AddSingleton<IClock>(SystemClock.Instance);

DeliveryWindowOptions deliveryWindowOptions = DeliveryWindowOptions.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(deliveryWindowOptions.ToDeliveryWindow());

var app = builder.Build();

app.Run();

// Exposed so integration tests can target the entry point via WebApplicationFactory<Program>.
// Add composition (Serilog, OpenAPI/Scalar, health checks, AddApplication, endpoint mapping)
// here as features are implemented.
public partial class Program { }
