namespace TechSpherex.CleanArchitecture.Domain.Common.Rules;

/// <summary>
/// Xác thực sự rằng tính chẵn/lẻ của Bay khớp với kích thước container.
/// Bay lẻ (1, 3, 5…) chứa container 20 feet.
/// Bay chẵn (2, 4, 6…) chứa container 40 feet.
/// </summary>
public sealed class BayParityMatchesContainerSizeRule(int bay, int containerSizeFeet) : IBusinessRule
{
    /// <summary>Mã quy tắc: "Yard.BayParityMatchesContainerSize".</summary>
    public string RuleCode => "Yard.BayParityMatchesContainerSize";

    /// <summary>Thông điệp lỗi: "Odd Bays host 20 ft containers; even Bays host 40 ft containers."</summary>
    public string Message => "Odd Bays host 20 ft containers; even Bays host 40 ft containers.";

    /// <summary>Độ ưu tiên: 1.</summary>
    public int Priority => 1;

    /// <summary>
    /// Đánh giá: trả về True nếu Bay và kích thước container không khớp.
    /// Bay lẻ chỉ chứa container 20 feet, bay chẵn chỉ chứa 40 feet.
    /// </summary>
    public bool IsBroken()
    {
        var bayIsOdd = bay % 2 != 0;
        var sizeIs20 = containerSizeFeet == 20;
        return bayIsOdd != sizeIs20;
    }
}