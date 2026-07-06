using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Store.Application.Services.AiChat.Tools;
using Store.Contracts.AiChat;
using Store.Infrastructure.Services.Gemini;
using Xunit;

namespace Store.Tests.UnitTests.Gemini;

public class GeminiClientServiceTests
{
    private static IOptions<GeminiOptions> CreateOptions(string apiKey, bool requireLive, bool allowMockFallback, string model = "gemini-2.5-flash")
    {
        return Options.Create(new GeminiOptions
        {
            ApiKey = apiKey,
            Model = model,
            RequireLive = requireLive,
            AllowMockFallback = allowMockFallback,
            TimeoutSeconds = 15
        });
    }

    [Fact]
    public async Task GenerateContentWithToolsAsync_WhenRequireLiveAndApiKeyMissing_ThrowsConfigurationError()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var httpClient = new HttpClient(handlerMock.Object);
        var options = CreateOptions("", requireLive: true, allowMockFallback: false);
        var registry = new AiToolRegistry(new List<IAiToolHandler>());
        var service = new GeminiClientService(httpClient, options, registry);

        // Act
        Func<Task> act = () => service.GenerateContentWithToolsAsync(new List<AiChatMessageDto>(), "/dashboard", null);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Gemini live mode requires Gemini:ApiKey.");
    }

    [Fact]
    public async Task GenerateContentWithToolsAsync_WhenFallbackAllowedAndApiKeyMissing_ReturnsFallbackResult()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var httpClient = new HttpClient(handlerMock.Object);
        var options = CreateOptions("", requireLive: false, allowMockFallback: true);
        var registry = new AiToolRegistry(new List<IAiToolHandler>());
        var service = new GeminiClientService(httpClient, options, registry);

        // Act
        var result = await service.GenerateContentWithToolsAsync(new List<AiChatMessageDto>(), "/dashboard", null);

        // Assert
        result.UsedFallback.Should().BeTrue();
        result.FallbackReason.Should().Contain("Gemini fallback enabled by configuration");
    }

    [Fact]
    public async Task GenerateContentWithToolsAsync_WhenFallbackDisabledAndHttpFails_DoesNotReturnMockResult()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Error payload")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var options = CreateOptions("live-key-xyz", requireLive: false, allowMockFallback: false);
        var registry = new AiToolRegistry(new List<IAiToolHandler>());
        var service = new GeminiClientService(httpClient, options, registry);

        // Act
        Func<Task> act = () => service.GenerateContentWithToolsAsync(new List<AiChatMessageDto> { new AiChatMessageDto { Role = "user", Message = "halo" } }, "/dashboard", null);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
