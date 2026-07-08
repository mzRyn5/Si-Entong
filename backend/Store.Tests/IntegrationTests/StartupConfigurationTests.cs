using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Store.Api.Configuration;
using Xunit;

namespace Store.Tests.IntegrationTests;

public class StartupConfigurationTests
{
    [Fact]
    public void Program_DoesNotResetDatabaseOnStartup()
    {
        var programSource = File.ReadAllText(GetRepositoryPath("Store.Api", "Program.cs"));

        programSource.Should().Contain("MigrateAsync");
        programSource.Should().NotContain("EnsureDeletedAsync");
        programSource.Should().NotContain("EnsureCreatedAsync");
    }

    [Fact]
    public void AppSettings_ConfiguresSerilogConsoleSink()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GetRepositoryPath("Store.Api", "appsettings.json")));

        var root = document.RootElement;
        root.TryGetProperty("Serilog", out var serilog).Should().BeTrue();
        var sinkNames = serilog.GetProperty("WriteTo")
            .EnumerateArray()
            .Select(element => element.GetProperty("Name").GetString())
            .ToList();

        sinkNames.Should().Contain("Console");
    }

    [Fact]
    public void AppSettings_DoesNotStoreDeploymentSecretsInBaseConfig()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GetRepositoryPath("Store.Api", "appsettings.json")));

        var root = document.RootElement;

        root.TryGetProperty("Gemini", out var gemini).Should().BeTrue();
        gemini.TryGetProperty("ApiKey", out var apiKey).Should().BeFalse("Gemini API keys must come from environment variables or user secrets");

        root.GetProperty("JwtSettings")
            .GetProperty("Secret")
            .GetString()
            .Should()
            .Be("${JWT_SECRET}", "the base config must not contain reusable JWT signing material");
    }

    [Fact]
    public void LaunchSettings_DoesNotStoreGeminiApiKey()
    {
        var launchSettings = File.ReadAllText(GetRepositoryPath("Store.Api", "Properties", "launchSettings.json"));

        launchSettings.Should().NotContain("Gemini_ApiKey");
        launchSettings.Should().NotContain("Gemini__ApiKey");
    }

    [Fact]
    public void DevelopmentAppSettings_ConfiguresLocalPostgresHost()
    {
        var appSettingsPath = GetRepositoryPath("Store.Api", "appsettings.Development.json");
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(appSettingsPath)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        connectionString.Should().Contain("Host=localhost");
        connectionString.Should().Contain("Database=store_management");
    }

    [Fact]
    public void GitIgnore_DoesNotContainMergeConflictMarkers()
    {
        var gitIgnore = File.ReadAllText(GetWorkspacePath(".gitignore"));

        gitIgnore.Should().NotContain("<<<<<<<");
        gitIgnore.Should().NotContain("=======");
        gitIgnore.Should().NotContain(">>>>>>>");
    }

    [Fact]
    public void AiChatRepository_CloseSessionFiltersBySessionAndUser()
    {
        var repositorySource = File.ReadAllText(GetRepositoryPath("Store.Infrastructure", "Persistence", "Repositories", "AiChatRepository.cs"));

        repositorySource.Should().Contain("s.Id == sessionId && s.UserId == userId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("${JWT_SECRET}")]
    [InlineData("development-secret-key-change-in-production-minimum-32-characters")]
    public void StartupSecurity_RejectsMissingPlaceholderOrDevelopmentJwtSecretOutsideDevelopment(string? secret)
    {
        var act = () => StartupSecurity.ValidateJwtSecret(secret, isDevelopment: false);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void StartupSecurity_AllowsDevelopmentSecretOnlyInDevelopment()
    {
        var secret = StartupSecurity.ValidateJwtSecret(
            "development-secret-key-change-in-production-minimum-32-characters",
            isDevelopment: true);

        secret.Should().Be("development-secret-key-change-in-production-minimum-32-characters");
    }

    [Fact]
    public void Program_ConfiguresSwaggerSchemaIdsWithNamespaces()
    {
        var programSource = File.ReadAllText(GetRepositoryPath("Store.Api", "Program.cs"));

        programSource.Should().Contain("CustomSchemaIds");
    }

    [Fact]
    public void Program_ConfiguresJsonStringEnumConverterForFrontendRequests()
    {
        var programSource = File.ReadAllText(GetRepositoryPath("Store.Api", "Program.cs"));

        programSource.Should().Contain("JsonStringEnumConverter");
    }

    [Theory]
    [InlineData("http://8080", null, "http://0.0.0.0:8080")]
    [InlineData("http://8080", "10000", "http://0.0.0.0:10000")]
    [InlineData("http://0.0.0.0:8080", null, null)]
    public void StartupHosting_NormalizesContainerUrlsThatCannotBind(string? urls, string? port, string? expected)
    {
        StartupHosting.NormalizeContainerUrls(urls, port).Should().Be(expected);
    }

    private static string GetRepositoryPath(params string[] parts)
    {
        var basePath = AppContext.BaseDirectory;
        var repoSrcPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", ".."));
        return Path.Combine(new[] { repoSrcPath }.Concat(parts).ToArray());
    }

    private static string GetWorkspacePath(params string[] parts)
    {
        var basePath = AppContext.BaseDirectory;
        var workspacePath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", "..", ".."));
        return Path.Combine(new[] { workspacePath }.Concat(parts).ToArray());
    }
}
