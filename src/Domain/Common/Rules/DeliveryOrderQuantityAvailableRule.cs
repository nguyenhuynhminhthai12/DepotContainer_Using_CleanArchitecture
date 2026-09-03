namespace TechSpherex.CleanArchitecture.Domain.Common.Rules;

/// <summary>
/// Một dòng Delivery Order chỉ được giải phóng (release) tối đa số lượng đã yêu cầu.
/// </summary>
public sealed class DeliveryOrderQuantityAvailableRule(int requestedQuantity, int deliveredQuantity) : IBusinessRule
{
    /// <summary>Mã quy tắc: "DeliveryOrder.QuantityAvailable".</summary>
    public string RuleCode => "DeliveryOrder.QuantityAvailable";

    /// <summary>Thông điệp lỗi: "Delivery order has no remaining quantity for this container type."</summary>
    public string Message => "Delivery order has no remaining quantity for this container type.";

    /// <summary>Độ ưu tiên: 2.</summary>
    public int Priority => 2;

    /// <summary>
    /// Đánh giá: trả về True nếu số lượng đã giao >= số lượng yêu cầu (hết số lượng).
    /// </summary>
    public bool IsBroken() => deliveredQuantity >= requestedQuantity;
}