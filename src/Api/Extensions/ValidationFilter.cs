using FluentValidation;
namespace TechSpherex.CleanArchitecture.Api.Extensions;

/// <summary>
/// Endpoint filter kiểm tra xác thực FluentValidation trước khi handler được gọi.
/// Nếu validation thất bại, trả về phản hồi 400 Validation Problem ngay lập tức.
/// </summary>
/// <typeparam name="T">Kiểu dữ liệu cần xác thực.</typeparam>
/// <param name="validator">Validator FluentValidation tương ứng.</param>
public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter where T : class
{
    /// <summary>
    /// Thực thi filter: kiểm tra validation trước khi chuyển request đến endpoint handler tiếp theo.
    /// </summary>
    /// <param name="context">Ngữ cảnh endpoint filter.</param>
    /// <param name="next">Delegate gọi endpoint handler tiếp theo.</param>
    /// <returns>Phản hồi validation lỗi hoặc kết quả của handler tiếp theo.</returns>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
            return await next(context);

        var validationResult = await validator.ValidateAsync(argument);
        if (validationResult.IsValid)
            return await next(context);

        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return Results.ValidationProblem(errors);
    }
}
