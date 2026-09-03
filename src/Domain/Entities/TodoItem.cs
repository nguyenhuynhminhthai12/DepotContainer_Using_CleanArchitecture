using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Domain.Entities;

/// <summary>
/// Thực thể Công việc — một mục công việc cần được thực hiện.
/// </summary>
public sealed class TodoItem : AuditableEntity
{
    /// <summary>Tiêu đề / tên gọn của công việc.</summary>
    public string Title { get; set; } = default!;

    /// <summary>Mô tả chi tiết công việc (tùy chọn).</summary>
    public string? Description { get; set; }

    /// <summary>Cho biết công việc đã hoàn thành chưa.</summary>
    public bool IsCompleted { get; private set; }

    /// <summary>Thời điểm công việc được đánh dấu hoàn thành.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Đánh dấu công việc là đã hoàn thành.</summary>
    public void MarkAsCompleted()
    {
        if (IsCompleted) return;
        IsCompleted = true;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Đánh dấu công việc là chưa hoàn thành.</summary>
    public void MarkAsIncomplete()
    {
        IsCompleted = false;
        CompletedAt = null;
    }
}
