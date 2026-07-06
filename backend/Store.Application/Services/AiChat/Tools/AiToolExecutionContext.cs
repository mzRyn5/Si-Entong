using System;
using System.Text.Json;

namespace Store.Application.Services.AiChat.Tools;

public sealed record AiToolExecutionContext(
    Guid SessionId,
    Guid UserId,
    Guid StoreId,
    string FunctionName,
    JsonElement Arguments);
