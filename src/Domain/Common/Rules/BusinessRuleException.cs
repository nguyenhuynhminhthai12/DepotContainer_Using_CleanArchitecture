namespace TechSpherex.CleanArchitecture.Domain.Common.Rules;

/// <summary>
/// Ngoại lệ được ném khi một quy tắc nghiệp vụ (business rule) bị vi phạm.
/// Có thể bắt bởi global exception handler để trả về phản hồi API nhất quán.
/// </summary>
public sealed class BusinessRuleException : Exception
{
    /// <summary>Quy tắc bị vi phạm.</summary>
    public IBusinessRule? BrokenRule { get; }

    /// <summary>Mã quy tắc của quy tắc bị vi phạm.</summary>
    public string RuleCode => BrokenRule?.RuleCode ?? "BusinessRule.Violation";

    /// <summary>Khởi tạo ngoại lệ mặc định.</summary>
    public BusinessRuleException() : base("A business rule was violated.") { }

    /// <summary>Khởi tạo ngoại lệ với thông điệp lỗi.</summary>
    /// <param name="message">Thông điệp mô tả lỗi.</param>
    public BusinessRuleException(string message) : base(message) { }

    /// <summary>Khởi tạo ngoại lệ với thông điệp lỗi và ngoại lệ gốc.</summary>
    /// <param name="message">Thông điệp mô tả lỗi.</param>
    /// <param name="innerException">Ngoại lệ gốc.</param>
    public BusinessRuleException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>Khởi tạo ngoại lệ với quy tắc bị vi phạm.</summary>
    /// <param name="brokenRule">Quy tắc nghiệp vụ bị vi phạm.</param>
    public BusinessRuleException(IBusinessRule brokenRule) : base(brokenRule.Message)
    {
        BrokenRule = brokenRule;
    }
}
