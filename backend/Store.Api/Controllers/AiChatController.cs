using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Store.Api.Controllers;
using Store.Application.Services.AiChat;
using Store.Contracts.AiChat;
using Store.Contracts.Responses.Common;

namespace Store.Api.Controllers;

[Authorize(Policy = "OwnerOrAdmin")]
[ApiController]
[Route("api/ai-chat")]
[Route("api/v1/ai-chat")]
public class AiChatController : BaseApiController
{
    private readonly IAiChatService _aiChatService;

    public AiChatController(IAiChatService aiChatService)
    {
        _aiChatService = aiChatService;
    }

    [HttpPost("message")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("AiMessagePolicy")]
    [ProducesResponseType(typeof(ApiResponse<AiChatResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendMessage([FromBody] AiChatRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Pesan tidak boleh kosong.");
        }

        // Get current user details and store details
        var userId = CurrentUserId;
        var storeId = CurrentStoreId;

        var result = await _aiChatService.HandleMessageAsync(request, userId, storeId);
        return SuccessResponse(result, "Pesan berhasil diproses.");
    }

    [HttpPost("action")]
    [ProducesResponseType(typeof(ApiResponse<AiActionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExecuteAction([FromBody] AiActionRequest request)
    {
        if (request == null)
        {
            return BadRequest("Request tidak boleh kosong.");
        }

        var result = await _aiChatService.ExecuteActionAsync(request, CurrentUserId, CurrentStoreId);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return SuccessResponse(result, result.Message);
    }

    [HttpGet("session/{sessionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<AiChatMessageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionHistory(Guid sessionId)
    {
        var result = await _aiChatService.GetSessionHistoryAsync(sessionId, CurrentUserId);
        return SuccessResponse(result, "Riwayat chat berhasil diambil.");
    }

    [HttpDelete("session/{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloseSession(Guid sessionId)
    {
        await _aiChatService.CloseSessionAsync(sessionId, CurrentUserId);
        return NoContent();
    }

    private Guid CurrentStoreId
    {
        get
        {
            var storeIdClaim = User.FindFirst("store_id")?.Value;
            if (Guid.TryParse(storeIdClaim, out var storeId))
            {
                return storeId;
            }
            return Guid.Empty;
        }
    }
}
