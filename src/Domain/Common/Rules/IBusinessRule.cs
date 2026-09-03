namespace TechSpherex.CleanArchitecture.Domain.Common.Rules;

    /// <summary>
    /// Đại diện cho một quy tắc nghiệp vụ (business rule) có thể được đánh giá trên thực thể.
    /// Các quy tắc là khối xây dựng có thể kết hợp cho Rule Engine.
    /// </summary>
    public interface IBusinessRule
    {
#pragma warning disable S1135 // False positive: XML doc example contains 'Todo' string, not a TODO comment
        /// <summary>Mã định danh duy nhất của quy tắc (ví dụ: "Todo.TitleRequired").</summary>
        string RuleCode { get; }
#pragma warning restore S1135 // False positive: XML doc example contains 'Todo' string, not a TODO comment

        /// <summary>Thông điệp lỗi khi quy tắc bị vi phạm.</summary>
        string Message { get; }

        /// <summary>Độ ưu tiên thực thi — giá trị nhỏ hơn sẽ được thực thi trước.</summary>
        int Priority => 0;

        /// <summary>Đánh giá quy tắc trên ngữ cảnh hiện tại.</summary>
        /// <returns>True nếu quy tắc bị vi phạm, False nếu đạt.</returns>
        bool IsBroken();
    }
