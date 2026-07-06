using System;
using System.Collections.Generic;
using System.Linq;

namespace Store.Application.Services.AiChat.Tools;

public sealed class AiToolRegistry
{
    private readonly Dictionary<string, IAiToolHandler> _handlers;

    public AiToolRegistry(IEnumerable<IAiToolHandler> handlers)
    {
        _handlers = new Dictionary<string, IAiToolHandler>();
        foreach (var handler in handlers)
        {
            foreach (var name in handler.FunctionNames)
            {
                _handlers[name] = handler;
            }
        }
    }

    public IAiToolHandler Resolve(string functionName)
    {
        if (_handlers.TryGetValue(functionName, out var handler))
        {
            return handler;
        }
        throw new InvalidOperationException($"Fungsi '{functionName}' tidak didukung oleh sistem.");
    }

    public object[] GetWhitelistedDeclarations()
    {
        return _handlers.Keys
            .Select(name => _handlers[name].GetDeclaration(name))
            .ToArray();
    }
}
