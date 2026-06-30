using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Application.Services.Store;
using Store.Contracts.Responses.Common;

namespace Store.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class StoreController : BaseApiController
{
    private readonly IStoreService _storeService;

    public StoreController(IStoreService storeService)
    {
        _storeService = storeService;
    }

    /// <summary>
    /// Get store profile
    /// </summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var result = await _storeService.GetProfileAsync(cancellationToken);
        return SuccessResponse(result, "Profil toko berhasil diambil.");
    }

    /// <summary>
    /// Update store profile (Owner only)
    /// </summary>
    [HttpPut("profile")]
    [Authorize(Policy = "OwnerOnly")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateStoreProfileRequest request, CancellationToken cancellationToken)
    {
        if (!IsOwner)
        {
            return ForbiddenResponse();
        }

        var result = await _storeService.UpdateProfileAsync(request, CurrentUserId, cancellationToken);
        return SuccessResponse(result, "Profil toko berhasil diperbarui.");
    }

    /// <summary>
    /// Get store settings
    /// </summary>
    [HttpGet("settings")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var result = await _storeService.GetSettingsAsync(cancellationToken);
        return SuccessResponse(result, "Pengaturan toko berhasil diambil.");
    }

    /// <summary>
    /// Update store settings (Owner only)
    /// </summary>
    [HttpPut("settings")]
    [Authorize(Policy = "OwnerOnly")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateStoreSettingsRequest request, CancellationToken cancellationToken)
    {
        if (!IsOwner)
        {
            return ForbiddenResponse();
        }

        var result = await _storeService.UpdateSettingsAsync(request, CurrentUserId, cancellationToken);
        return SuccessResponse(result, "Pengaturan toko berhasil diperbarui.");
    }
}
