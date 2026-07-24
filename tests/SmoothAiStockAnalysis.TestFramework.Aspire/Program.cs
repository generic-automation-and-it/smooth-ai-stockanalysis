using System.Diagnostics.CodeAnalysis;
using SmoothAiStockAnalysis.TestFramework.Aspire;

[assembly: ExcludeFromCodeCoverage]

var builder = DistributedApplication.CreateBuilder(args);
builder.AddWireMockTestDependency();

builder.Build().Run();
