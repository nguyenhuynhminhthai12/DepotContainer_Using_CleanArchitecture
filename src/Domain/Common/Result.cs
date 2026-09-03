namespace TechSpherex.CleanArchitecture.Domain.Common;

/// <summary>
/// Mẫu kết quả (Result Pattern) dùng để biểu diễn thành công hoặc thất bại của một thao tác
/// mà không ném ngoại lệ. Cung cấp qua <see cref="Error"/> cho các lỗi nghiệp vụ.
/// </summary>
// Copyright (c) 2026 TechSpherex
public class Result
{
    /// <summary>
    /// Khởi tạo một <see cref="Result"/> với trạng thái thành công/thất bại và lỗi tương ứng.
    /// </summary>
    /// <param name="isSuccess">Cho biết kết quả có thành công không.</param>
    /// <param name="error">Lỗi đi kèm — phải là null khi thành công, không được null khi thất bại.</param>
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException("A successful result cannot have an error.");
        if (!isSuccess && error is null)
            throw new InvalidOperationException("A failed result must have an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Cho biết thao tác đã thành công hay không.</summary>
    public bool IsSuccess { get; }

    /// <summary>Cho biết thao tác đã thất bại hay không (ngược lại với <see cref="IsSuccess"/>).</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Lỗi chi tiết khi thao tác thất bại — null khi thành công.</summary>
    public Error? Error { get; }

    /// <summary>Tạo một <see cref="Result{T}"/> thành công chứa giá trị kiểu <typeparamref name="T"/>.</summary>
    public static Result<T> Success<T>(T value) => new(value, true, null);

    /// <summary>Tạo một <see cref="Result"/> thành công không mang giá trị.</summary>
    public static Result Success() => new(true, null);

    /// <summary>Tạo một <see cref="Result{T}"/> thất bại với lỗi <paramref name="error"/>.</summary>
    public static Result<T> Failure<T>(Error error) => new(default, false, error);

    /// <summary>Tạo một <see cref="Result"/> thất bại với lỗi <paramref name="error"/>.</summary>
    public static Result Failure(Error error) => new(false, error);
}

/// <summary>
/// Phiên bản có giá trị của <see cref="Result"/> — đồng thời mang theo một giá trị kiểu <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Kiểu dữ liệu của giá trị trả về khi thành công.</typeparam>
public class Result<T> : Result
{
    /// <summary>Khởi tạo <see cref="Result{T}"/> với giá trị, trạng thái thành công và lỗi.</summary>
    internal Result(T? value, bool isSuccess, Error? error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    /// <summary>Giá trị trả về khi thành công — null khi thất bại.</summary>
    public T? Value { get; }
}
