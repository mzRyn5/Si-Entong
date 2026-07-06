using System;
using System.Threading;
using System.Threading.Tasks;
using Store.Contracts.AiChat;

namespace Store.Application.Services.AiChat;

public interface IAiDraftActionService
{
    Task<AiActionResponse> ExecuteAsync(AiActionRequest request, Guid userId, Guid storeId, CancellationToken cancellationToken = default);
}
