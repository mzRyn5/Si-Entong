using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Store.Application.Abstractions.Repositories;
using Store.Application.Abstractions.Services;
using Store.Application.Services.AiChat;
using Store.Contracts.AiChat;
using Store.Domain.Entities;
using Xunit;

namespace Store.Tests.UnitTests.AiChat;

public class AiResponseComposerTests
{
    private readonly Mock<IAiChatRepository> _chatRepoMock = new();
    private readonly AiResponseComposer _composer;

    public AiResponseComposerTests()
    {
        _composer = new AiResponseComposer(_chatRepoMock.Object);
    }

    [Fact]
    public async Task ComposeAsync_WhenGeminiReturnsText_ReturnsTextResponse()
    {
        // Arrange
        var result = new GeminiResult { Text = "Halo admin" };
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        _chatRepoMock.Setup(r => r.GetLatestActiveDraftAsync(sessionId, userId, storeId, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiActionDraft?)null);

        // Act
        var response = await _composer.ComposeAsync(result, sessionId, userId, storeId);

        // Assert
        response.Reply.Should().Be("Halo admin");
        response.ResponseType.Should().Be("text");
        response.RequiresConfirmation.Should().BeFalse();
    }

    [Fact]
    public async Task ComposeAsync_WhenGeminiCallsNavigatePage_MapsToWhitelistedRoute()
    {
        // Arrange
        var result = new GeminiResult
        {
            HasFunctionCall = true,
            FunctionName = "navigate_to_page",
            Arguments = "{\"pageKey\":\"products\"}"
        };
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        _chatRepoMock.Setup(r => r.GetLatestActiveDraftAsync(sessionId, userId, storeId, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiActionDraft?)null);

        // Act
        var response = await _composer.ComposeAsync(result, sessionId, userId, storeId);

        // Assert
        response.ResponseType.Should().Be("navigation");
        response.UiAction.Should().NotBeNull();
        response.UiAction!.Route.Should().Be("/products");
    }

    [Fact]
    public async Task ComposeAsync_WhenLatestDraftExists_AttachesQuickActions()
    {
        // Arrange
        var result = new GeminiResult { Text = "Ini draf penjualan Anda" };
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var draftId = Guid.NewGuid();

        var draft = new AiActionDraft
        {
            Id = draftId,
            SessionId = sessionId,
            UserId = userId,
            ActionName = "create_sale",
            Status = "pending",
            ExpiredAt = DateTime.UtcNow.AddMinutes(5),
            DraftPayload = "{}"
        };

        _chatRepoMock.Setup(r => r.GetLatestActiveDraftAsync(sessionId, userId, storeId, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(draft);

        // Act
        var response = await _composer.ComposeAsync(result, sessionId, userId, storeId);

        // Assert
        response.ResponseType.Should().Be("draft_preview");
        response.RequiresConfirmation.Should().BeTrue();
        response.DraftId.Should().Be(draftId.ToString());
        response.QuickActions.Should().HaveCount(2);
        response.QuickActions[0].Action.Should().Be("confirm_sale");
        response.QuickActions[1].Action.Should().Be("cancel_draft");
    }
}
