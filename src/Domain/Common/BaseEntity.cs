namespace TechSpherex.CleanArchitecture.Domain.Common;

/// <summary>
/// Lớp cơ sở trừu tượng cho tất cả thực thể (entity) trong Domain Layer.
/// Cung cấp khóa chính duy nhất dạng <see cref="Guid"/>.
/// </summary>
// Copyright (c) 2026 TechSpherex
public abstract class BaseEntity
{
    /// <summary>Mã định danh duy nhất của thực thể.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();
}
