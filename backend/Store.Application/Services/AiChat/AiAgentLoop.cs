using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Store.Application.Abstractions.Repositories;
using Store.Application.Abstractions.Services;
using Store.Contracts.AiChat;

namespace Store.Application.Services.AiChat;

public sealed class AiAgentLoop
{
    private readonly IGeminiClient _geminiClient;
    private readonly IAiToolExecutor _toolExecutor;
    private readonly IAiChatRepository _chatRepo;

    public AiAgentLoop(
        IGeminiClient geminiClient,
        IAiToolExecutor toolExecutor,
        IAiChatRepository chatRepo)
    {
        _geminiClient = geminiClient;
        _toolExecutor = toolExecutor;
        _chatRepo = chatRepo;
    }

    public async Task<GeminiResult> RunAsync(
        Guid sessionId,
        List<AiChatMessageDto> history,
        AiChatRequest request,
        Guid userId,
        Guid storeId,
        StoreContextSnapshot context,
        CancellationToken cancellationToken = default)
    {
        int loopCount = 0;
        bool waitingForAi = true;
        bool usedFallbackDuringLoop = false;
        string? fallbackReasonDuringLoop = null;
        GeminiResult? aiResult = null;

        while (waitingForAi && loopCount < AiChatOptions.MaxAgentLoops)
        {
            try
            {
                aiResult = await _geminiClient.GenerateContentWithToolsAsync(
                    history,
                    request.CurrentRoute,
                    request.ActiveFormKey,
                    context,
                    cancellationToken);

                if (aiResult.UsedFallback)
                {
                    usedFallbackDuringLoop = true;
                    fallbackReasonDuringLoop ??= aiResult.FallbackReason;
                }

                if (aiResult.HasFunctionCall && !string.IsNullOrEmpty(aiResult.FunctionName))
                {
                    // Save model's function call in db history
                    await _chatRepo.SaveMessageAsync(
                        sessionId,
                        "assistant",
                        "",
                        intent: null,
                        toolCalls: aiResult.Arguments,
                        toolResults: null,
                        functionName: aiResult.FunctionName,
                        cancellationToken: cancellationToken);

                    // Execute tool
                    var toolResult = await _toolExecutor.ExecuteAsync(
                        sessionId,
                        aiResult.FunctionName,
                        aiResult.Arguments ?? "{}",
                        userId,
                        storeId);

                    // Save tool execution result in db
                    var toolResultJson = JsonSerializer.Serialize(toolResult);
                    await _chatRepo.SaveMessageAsync(
                        sessionId,
                        "tool",
                        "",
                        intent: null,
                        toolCalls: null,
                        toolResults: toolResultJson,
                        functionName: aiResult.FunctionName,
                        cancellationToken: cancellationToken);

                    // Add tool calls and responses to loop context history
                    history.Add(new AiChatMessageDto
                    {
                        Role = "assistant",
                        Message = aiResult.Arguments ?? "{}",
                        FunctionName = aiResult.FunctionName,
                        CreatedAt = DateTime.UtcNow
                    });

                    history.Add(new AiChatMessageDto
                    {
                        Role = "tool",
                        Message = toolResultJson,
                        FunctionName = aiResult.FunctionName,
                        CreatedAt = DateTime.UtcNow
                    });

                    loopCount++;
                }
                else
                {
                    waitingForAi = false;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        if (loopCount >= AiChatOptions.MaxAgentLoops || aiResult == null)
        {
            throw new InvalidOperationException("Maaf, permintaan Anda terlalu kompleks untuk diproses dalam satu sesi. Coba pecah perintah Anda.");
        }

        if (usedFallbackDuringLoop)
        {
            aiResult.UsedFallback = true;
            aiResult.FallbackReason ??= fallbackReasonDuringLoop;
        }

        return aiResult;
    }
}
