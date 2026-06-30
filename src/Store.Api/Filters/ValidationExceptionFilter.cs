using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Store.Api.Filters;

public class ValidationExceptionFilter : IAsyncActionFilter
{
    private readonly IValidator _validator;

    public ValidationExceptionFilter(IValidator validator)
    {
        _validator = validator;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => ToCamelCase(kvp.Key),
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToList()
                );

            var response = new
            {
                success = false,
                message = "Validasi gagal.",
                error = new
                {
                    code = "VALIDATION_ERROR",
                    details = errors.SelectMany(e => e.Value.Select(v => new { field = e.Key, message = v }))
                }
            };

            context.Result = new BadRequestObjectResult(response);
            return;
        }

        await next();
    }

    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
            return str;

        return char.ToLowerInvariant(str[0]) + str[1..];
    }
}
