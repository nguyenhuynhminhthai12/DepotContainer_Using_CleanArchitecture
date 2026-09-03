namespace TechSpherex.CleanArchitecture.Application.Abstractions.Rules;

/// <summary>
/// Lớp trừu tượng engine quy tắc nghiệp vụ ở tầng Application.
/// Đánh giá các quy tắc nghiệp vụ có thể cấu hình trước dựa trên đối tượng bối cảnh.
/// Hỗ trợ định nghĩa quy tắc động được tải từ cấu hình.
/// </summary>
public interface IRuleEngine
{
    /// <summary>
    /// Đánh giá tất cả quy tắc trong một rule set chống lại bối cảnh cung cấp.
    /// </summary>
    /// <param name="ruleSetName">Tên rule set (ví dụ: "TodoCreation", "OrderApproval").</param>
    /// <param name="context">Từ điển các giá trị bối cảnh (facts) để đánh giá quy tắc.</param>
    /// <returns><see cref="RuleResult"/> chứa kết quả và danh sách vi phạm.</returns>
    RuleResult Evaluate(string ruleSetName, IDictionary<string, object?> context);

    /// <summary>
    /// Đánh giá một biểu thức quy tắc đơn lẻ trên bối cảnh đã cho.
    /// </summary>
    /// <param name="expression">Biểu thức quy tắc (ví dụ: "Amount > 1000").</param>
    /// <param name="context">Từ điển các giá trị bối cảnh (facts).</param>
    /// <returns>True nếu quy tắc thỏa mãn, False nếu không.</returns>
    bool EvaluateExpression(string expression, IDictionary<string, object?> context);

    /// <summary>
    /// Lấy danh sách tất cả tên rule set khả dụng.
    /// </summary>
    IReadOnlyList<string> GetRuleSetNames();
}
