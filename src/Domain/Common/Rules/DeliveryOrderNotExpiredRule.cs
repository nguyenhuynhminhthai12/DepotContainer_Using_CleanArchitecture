namespace TechSpherex.CleanArchitecture.Domain.Common.Rules;

/// <summary>
/// Delivery Order phải còn hiệu lực (chưa hết hạn) để cho phép thực hiện Gate Out.
/// </summary>
public sealed class DeliveryOrderNotExpiredRule(DateTimeOffset expiryDate, DateTimeOffset now) : IBusinessRule
{
    /// <summary>Mã quy tắc: "DeliveryOrder.NotExpired".</summary>
    public string RuleCode => "DeliveryOrder.NotExpired";

    /// <summary>Thông điệp lỗi: "Delivery order has expired and cannot authorise Gate Out."</summary>
    public string Message => "Delivery order has expired and cannot authorise Gate Out.";

    /// <summary>Độ ưu tiên: 1.</summary>
    public int Priority => 1;

    /// <summary>
    /// Đánh giá: trả về True nếu đơn hàng đã hết hạn so với thời điểm hiện tại.
    /// </summary>
    public bool IsBroken() => expiryDate < now;
}