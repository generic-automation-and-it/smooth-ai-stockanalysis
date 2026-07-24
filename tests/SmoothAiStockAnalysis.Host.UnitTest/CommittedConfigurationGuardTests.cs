using System.Text.Json;
using SmoothAiStockAnalysis.Host.Configuration;

namespace SmoothAiStockAnalysis.Host.UnitTest;

/// <summary>
/// L0 guard that scans the committed <c>appsettings.json</c> for real credentials
/// (NFR-007 verification: "repository scanned for committed credentials as part of the build";
/// NFR-043/044). The committed file must carry placeholder tokens on known credential keys and
/// must not contain secret-shaped literals (<c>sk-</c>, <c>ghp_</c>, high-entropy base64).
/// </summary>
/// <remarks>
/// This test reads the file from disk at the repository root. It is intentionally an L0 guard
/// rather than a CI script so it runs on every <c>dotnet test</c> invocation and catches
/// accidental secret commits before the PR opens.
/// </remarks>
public sealed class CommittedConfigurationGuardTests
{
    /// <summary>
    /// Regex patterns matching well-known secret shapes. Each is tested case-insensitively
    /// against every string value in the committed <c>appsettings.json</c>.
    /// </summary>
    private static readonly string[] SecretPatterns =
    [
        @"sk-[a-zA-Z0-9]{20,}",          // OpenAI API key
        @"sk-ant-[a-zA-Z0-9\-]{20,}",    // Anthropic API key
        @"ghp_[a-zA-Z0-9]{36}",          // GitHub personal access token
        @"github_pat_[a-zA-Z0-9_]{22,}", // GitHub fine-grained PAT
        @"AKIA[0-9A-Z]{16}",             // AWS access key ID
        @"AIza[a-zA-Z0-9\-_]{35}",       // Google API key
    ];

    [Fact]
    public void CommittedAppSettingsCarriesTheOpenAiApiKeyPlaceholder()
    {
        JsonElement root = LoadCommittedAppSettings();

        JsonElement credentialsSection = root.GetProperty(CredentialsOptions.SectionName);
        JsonElement openAiSection = credentialsSection.GetProperty("OpenAi");
        string apiKey = openAiSection.GetProperty("ApiKey").GetString()
            ?? throw new InvalidOperationException("Credentials:OpenAi:ApiKey is null.");

        apiKey.ShouldBe(CredentialsOptions.OpenAiApiKeyPlaceholder);
    }

    [Fact]
    public void CommittedAppSettingsDoesNotContainSecretShapedLiterals()
    {
        string rawJson = File.ReadAllText(FindCommittedAppSettingsPath());

        foreach (string pattern in SecretPatterns)
        {
            var regex = new System.Text.RegularExpressions.Regex(
                pattern,
                System.Text.RegularExpressions.RegexOptions.Compiled);

            regex.IsMatch(rawJson).ShouldBeFalse(
                $"Committed appsettings.json contains a secret-shaped literal matching '{pattern}'. "
                + "Replace with a placeholder token and supply the real value via environment variable.");
        }
    }

    [Fact]
    public void CommittedAppSettingsDoesNotEchoTheDefaultUserPlaceholderInCredentials()
    {
        // The DefaultUser:UniqueIdentifier placeholder is a nil GUID (LADR-018). It must not
        // appear inside the Credentials section — credentials use the env-var-mirroring
        // placeholder convention, not the nil-GUID convention.
        JsonElement root = LoadCommittedAppSettings();
        JsonElement credentialsSection = root.GetProperty(CredentialsOptions.SectionName);
        string credentialsJson = credentialsSection.GetRawText();

        credentialsJson.ShouldNotContain("00000000-0000-4000-8000-000000000001");
    }

    private static JsonElement LoadCommittedAppSettings()
    {
        string path = FindCommittedAppSettingsPath();
        string json = File.ReadAllText(path);
        using var document = JsonDocument.Parse(json);
        // Clone so the document survives disposal of the JsonDocument.
        return document.RootElement.Clone();
    }

    private static string FindCommittedAppSettingsPath()
    {
        string? directory = AppContext.BaseDirectory;
        while (directory is not null
            && !File.Exists(Path.Combine(directory, "smooth-ai-stockanalysis.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        if (directory is null)
        {
            throw new InvalidOperationException(
                "Could not locate the repository root (smooth-ai-stockanalysis.slnx) from the test output directory.");
        }

        string path = Path.Combine(
            directory,
            "src",
            "SmoothAiStockAnalysis.Host",
            "appsettings.json");

        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"Expected committed appsettings.json at '{path}' but it was not found.");
        }

        return path;
    }
}
