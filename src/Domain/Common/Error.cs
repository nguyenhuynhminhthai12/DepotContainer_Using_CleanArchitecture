namespace TechSpherex.CleanArchitecture.Domain.Common;

/// <summary>
/// Đại diện cho một lỗi miền (domain error) chứa mã lỗi, thông điệp và kiểu lỗi.
/// Thường được trả về trong <see cref="Result"/> để mô tả lỗi nghiệp vụ.
/// </summary>
/// <param name="Code">Mã định danh lỗi (ví dụ: "Customer.NotFound").</param>
/// <param name="Message">Thông điệp mô tả chi tiết lỗi.</param>
/// <param name="Type">Phân loại lỗi — mặc định là <see cref="ErrorType.Failure"/>.</param>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    /// <summary>Tạo lỗi kiểu <see cref="ErrorType.NotFound"/> — tài nguyên không tìm thấy.</summary>
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    /// <summary>Tạo lỗi kiểu <see cref="ErrorType.Validation"/> — lỗi xác thực dữ liệu.</summary>
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    /// <summary>Tạo lỗi kiểu <see cref="ErrorType.Conflict"/> — xung đột dữ liệu (trùng lặp, trạng thái mâu thuẫn).</summary>
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    /// <summary>Tạo lỗi kiểu <see cref="ErrorType.Failure"/> — lỗi chung, thất bại không xác định.</summary>
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);

    /// <summary>Tạo lỗi kiểu <see cref="ErrorType.Unauthorized"/> — người dùng chưa xác thực hoặc thiếu quyền.</summary>
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
}

/// <summary>
/// Liệt kê các kiểu lỗi hệ thống dùng trong <see cref="Error"/>.
/// </summary>
public enum ErrorType
{
    /// <summary>Lỗi chung, thất bại không xác định.</summary>
    Failure,

    /// <summary>Lỗi xác thực dữ liệu (FluentValidation, business rules...).</summary>
    Validation,

    /// <summary>Lỗi không tìm thấy tài nguyên.</summary>
    NotFound,

    ///summary>Lỗi xung đột dữ liệu (trùng lặp, trạng thái mâu thun).</summary>
    Conflict,

    /// <summary>Lỗi không được phép truy cập (chưa xác thực / thiếu quyền).</summary>
    Unauthorized
}
