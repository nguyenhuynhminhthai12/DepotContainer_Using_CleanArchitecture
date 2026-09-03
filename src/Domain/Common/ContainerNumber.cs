namespace TechSpherex.CleanArchitecture.Domain.Common;

    /// <summary>
    /// Số thùng hàng mạnh (strongly-typed) tuân thủ tiêu chuẩn BIC/ISO 6346.
    /// Định dạng: 11 ký tự — mã chủ sở hữu 3 chữ cái + mã loại 1 chữ cái + số seri 6 chữ số + 1 chữ số kiểm tra.
    /// Được xác thực bằng thuật toán Modulo 11 (xem <see cref="Domain.Common.Rules.ContainerNumberCheckDigitRule"/>).
    /// </summary>
    public readonly record struct ContainerNumber
    {
        /// <summary>Giá trị chuỗi 11 ký tự của số thùng hàng.</summary>
        public string Value { get; }

        /// <summary>
        /// Khởi tạo một <see cref="ContainerNumber"/> từ chuỗi đầu vào.
        /// Tự động chuẩn hóa (trim + viết hoa) và kiểm tra độ dài 11 ký tự.
        /// </summary>
        /// <param name="value">Số thùng hàng dưới dạng chuỗi (11 ký tự).</param>
        /// <exception cref="ArgumentNullException">Nếu <paramref name="value"/> là null.</exception>
        /// <exception cref="ArgumentException">Nếu độ dài đã chuẩn hóa khác 11.</exception>
        public ContainerNumber(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            var normalized = value.Trim().ToUpperInvariant();
            if (normalized.Length != 11)
                throw new ArgumentException("Container number must be exactly 11 characters.", nameof(value));
            Value = normalized;
        }

        /// <summary>Trả về giá trị chuỗi của số thùng hàng.</summary>
        public override string ToString() => Value;

        /// <summary>Chuyển đổi ngầm định từ <see cref="ContainerNumber"/> sang <see cref="string"/>.</summary>
        public static implicit operator string(ContainerNumber number) => number.Value;

        /// <summary>Chuyển đổi tường minh từ <see cref="string"/> sang <see cref="ContainerNumber"/>.</summary>
        public static explicit operator ContainerNumber(string value) => new(value);
    }