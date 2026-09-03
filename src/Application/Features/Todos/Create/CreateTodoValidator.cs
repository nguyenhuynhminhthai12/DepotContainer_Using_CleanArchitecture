using FluentValidation;

namespace TechSpherex.CleanArchitecture.Application.Features.Todos.Create;

/// <summary>
/// Validator cho <see cref="CreateTodoCommand"/> — xác thực tiêu đề và mô tả khi tạo Todo.
/// </summary>
public sealed class CreateTodoValidator : AbstractValidator<CreateTodoCommand>
{
    public CreateTodoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }
}
