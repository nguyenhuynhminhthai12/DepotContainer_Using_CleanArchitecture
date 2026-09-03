using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TechSpherex.CleanArchitecture.Domain.Common.Rules;

namespace TechSpherex.CleanArchitecture.Api.Extensions;

/// <summary>
/// Global exception handler — bắt mọi ngoại lệ không được xử lý và trả về phản hồi Problem Details nhất quán.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>
    /// Xử lý ngoại lệ và trả về phản hồi Problem Details tương ứng.
    /// </summary>
    /// <param name="httpContext">Ngữ cảnh HTTP.</param>
    /// <param name="exception">Ngoại lệ cần xử lý.</param>
    /// <param name="cancellationToken">Token hủy.</param>
    /// <returns>True nếu đã xử lý thành công, False nếu ngoại lệ không thuộc phạm vi.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var problemDetails = exception switch
        {
            BusinessRuleException brEx => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Business rule violated",
                Detail = brEx.Message,
                Extensions = { ["ruleCode"] = brEx.RuleCode }
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred",
                Detail = httpContext.RequestServices
                    .GetRequiredService<IHostEnvironment>()
                    .IsDevelopment()
                        ? exception.Message
                        : "Please try again later or contact support."
            }
        };

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
