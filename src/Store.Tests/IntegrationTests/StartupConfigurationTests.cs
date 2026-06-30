using System.Text.Json;
using FluentAssertions;
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

    private static string GetRepositoryPath(params string[] parts)
    {
        var basePath = AppContext.BaseDirectory;
        var repoSrcPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", ".."));
        return Path.Combine(new[] { repoSrcPath }.Concat(parts).ToArray());
    }
}
