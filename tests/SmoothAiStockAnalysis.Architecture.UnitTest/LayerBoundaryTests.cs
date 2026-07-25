using System.Reflection;
using System.Text;
using NetArchTest.Rules;
using SmoothAiStockAnalysis.Application.Configuration;
using SmoothAiStockAnalysis.Domain.Entities;
using SmoothAiStockAnalysis.Infrastructure.Extensions;
using SmoothAiStockAnalysis.Infrastructure.Persistence;

namespace SmoothAiStockAnalysis.Architecture.UnitTest;

/// <summary>
/// Structural enforcement of LADR-001 / NFR-090 layer dependency rules.
/// Failure messages name the rule and every offending type or assembly.
/// </summary>
public sealed class LayerBoundaryTests
{
    private static readonly Assembly DomainAssembly = typeof(User).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ISettingsResolver).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(SmoothAiStockAnalysisDbContext).Assembly;
    private static readonly Assembly HostAssembly = typeof(Program).Assembly;

    private static readonly HashSet<string> DomainAllowedExternalAssemblies = new(StringComparer.Ordinal)
    {
        "NodaTime",
        // BCL facades commonly referenced by net10 class libraries.
        "System.Runtime",
        "System.Collections",
        "System.Linq",
        "System.Threading",
        "System.Threading.Tasks",
        "System.Private.CoreLib",
        "netstandard",
        "mscorlib",
    };

    [Fact]
    public void DomainMustNotDependOnApplicationInfrastructureOrHost()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationAssembly.GetName().Name!,
                InfrastructureAssembly.GetName().Name!,
                HostAssembly.GetName().Name!)
            .GetResult();

        AssertNetArchSuccess(
            result,
            "NFR-090 / LADR-001: Domain must not depend on Application, Infrastructure, or Host");
    }

    [Fact]
    public void DomainMayOnlyReferenceNodaTimeOutsideBcl()
    {
        // Assembly-reference check is more reliable than NetArchTest OnlyHaveDependenciesOn
        // for BCL facades, and matches the AGENTS.md wording (package graph, not type graph).
        List<string> forbidden = DomainAssembly.GetReferencedAssemblies()
            .Select(static name => name.Name ?? string.Empty)
            .Where(static name => !string.IsNullOrEmpty(name))
            .Where(static name => !name.StartsWith("System.", StringComparison.Ordinal))
            .Where(static name => !DomainAllowedExternalAssemblies.Contains(name))
            .Where(static name => !string.Equals(name, DomainAssembly.GetName().Name, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        if (forbidden.Count == 0)
        {
            // NodaTime must remain the sole intentional external package.
            DomainAssembly.GetReferencedAssemblies()
                .Select(static name => name.Name)
                .ShouldContain("NodaTime");
            return;
        }

        Assert.Fail(
            "NFR-090 / AGENTS.md: Domain's only allowed external value-semantics dependency is NodaTime. "
            + "Forbidden assembly references: "
            + string.Join(", ", forbidden));
    }

    [Fact]
    public void ApplicationMustNotDependOnInfrastructureOrHost()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                InfrastructureAssembly.GetName().Name!,
                HostAssembly.GetName().Name!)
            .GetResult();

        AssertNetArchSuccess(
            result,
            "NFR-090 / LADR-001: Application must not depend on Infrastructure or Host");
    }

    [Fact]
    public void InfrastructureMustNotDependOnHost()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(HostAssembly.GetName().Name!)
            .GetResult();

        AssertNetArchSuccess(
            result,
            "NFR-090 / LADR-001: Infrastructure must not depend on Host");
    }

    [Fact]
    public void HostMustNotDependOnEntityFrameworkCoreTypes()
    {
        // Mechanically checkable slice of "Host holds no persistence logic":
        // Host must not reference EF Core types directly (DbContext belongs in Infrastructure).
        // NetArchTest matches dependencies by name prefix, so this one entry also covers
        // Microsoft.EntityFrameworkCore.Relational and .Sqlite.
        NetArchTest.Rules.TestResult result = Types.InAssembly(HostAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        AssertNetArchSuccess(
            result,
            "NFR-090 / HOST_AGENTS.md: Host must not reference EF Core types directly (no DbContext bypass)");
    }

    [Fact]
    public void ArchitectureTestLoadsAllFourLayerAssemblies()
    {
        DomainAssembly.GetName().Name.ShouldBe("SmoothAiStockAnalysis.Domain");
        ApplicationAssembly.GetName().Name.ShouldBe("SmoothAiStockAnalysis.Application");
        InfrastructureAssembly.GetName().Name.ShouldBe("SmoothAiStockAnalysis.Infrastructure");
        HostAssembly.GetName().Name.ShouldBe("SmoothAiStockAnalysis.Host");

        // Touch an Infrastructure extension type so the reference is not trimmed as unused.
        typeof(ServiceCollectionExtensions).Assembly.ShouldBe(InfrastructureAssembly);
    }

    private static void AssertNetArchSuccess(NetArchTest.Rules.TestResult result, string rule)
    {
        if (result.IsSuccessful)
        {
            return;
        }

        List<string> offenders = result.FailingTypes is null
            ? []
            : result.FailingTypes
                .Select(static type => type.FullName ?? type.Name)
                .Order(StringComparer.Ordinal)
                .ToList();

        var message = new StringBuilder();
        message.Append(rule);
        message.Append(". Offending types: ");
        message.Append(offenders.Count > 0 ? string.Join(", ", offenders) : "(none reported)");
        Assert.Fail(message.ToString());
    }
}
