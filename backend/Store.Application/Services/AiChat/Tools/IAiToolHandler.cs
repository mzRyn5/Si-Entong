using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Store.Application.Services.AiChat.Tools;

public interface IAiToolHandler
{
    IReadOnlyCollection<string> FunctionNames { get; }
    object GetDeclaration(string functionName);
    Task<object> ExecuteAsync(AiToolExecutionContext context, CancellationToken cancellationToken = default);
}
