using Microsoft.Extensions.Logging;

namespace SmoothAiStockAnalysis.TestFramework.Logging;

public sealed record XUnitLoggerCategoryMinValue(string CategoryPrefix, LogLevel MinLevel);
