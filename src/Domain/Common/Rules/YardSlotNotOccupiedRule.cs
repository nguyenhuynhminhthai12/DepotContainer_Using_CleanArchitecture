namespace TechSpherex.CleanArchitecture.Domain.Common.Rules;

/// <summary>
/// Một YardSlot chỉ chứa tối đa một container tại một thời điểm.
/// </summary>
public sealed class YardSlotNotOccupiedRule(bool isOccupied) : IBusinessRule
{
    /// <summary>Mã quy tắc: "Yard.SlotNotOccupied".</summary>
    public string RuleCode => "Yard.SlotNotOccupied";

    /// <summary>Thông điệp lỗi: "Yard slot is already occupied by another container."</summary>
    public string Message => "Yard slot is already occupied by another container.";

    /// <summary>Độ ưu tiên: 1.</summary>
    public int Priority => 1;

    /// <summary>
    /// Đánh giá: trả về True nếu slot đã bị chiếm (quy tắc bị vi phạm).
    /// </summary>
    public bool IsBroken() => isOccupied;
}