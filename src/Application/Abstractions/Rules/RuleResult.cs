namespace TechSpherex.CleanArchitecture.Application.Abstractions.Rules;

/// <summary>
/// Kết quả đánh giá của rule engine.
/// Chứa kết quả (hợp lệ/không hợp lệ) và danh sách các vi phạm tìm thấy.
/// </summary>
public sealed class RuleResult
{
    /// <summary>Cho biết kết quả có hợp lệ không (không có vi phạm nào).</summary>
    public bool IsValid => Violations.Count == 0;

    /// <summary>Danh sách các vi phạm quy tắc.</summary>
    public IReadOnlyList<RuleViolation> Violations { get; }

    /// <summary>Khởi tạo RuleResult với danh sách vi phạm.</summary>
    private RuleResult(IReadOnlyList<RuleViolation> violations) => Violations = violations;

    /// <summary>Tạo một RuleResult hợp lệ (không có vi phạm).</summary>
    public static RuleResult Pass() => new([]);

    /// <summary>Tạo một RuleResult không hợp lệ với danh sách vi phạm.</summary>
    public static RuleResult Fail(IReadOnlyList<RuleViolation> violations) => new(violations);

    /// <summary>Tạo một RuleResult không hợp lệ với một vi phạm duy nhất.</summary>
    public static RuleResult Fail(string ruleCode, string message) =>
        new([new RuleViolation(ruleCode, message)]);
}

/// <summary>
/// Một vi phạm quy tắc duy nhất gồm mã và thông điệp.
/// </summary>
/// <param name="RuleCode">Mã định danh của quy tắc bị vi phạm.</param>
/// <param name="Message">Thông điệp mô tả vi phạm.</param>
public sealed record RuleViolation(string RuleCode, string Message);
