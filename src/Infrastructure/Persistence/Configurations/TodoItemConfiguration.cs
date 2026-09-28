using TechSpherex.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TechSpherex.CleanArchitecture.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình ánh xạ Entity Framework Core cho thực thể Công việc mẫu <see cref="TodoItem"/>.
/// </summary>
public sealed class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    /// <inheritdoc/>
#pragma warning disable CA1822, S2325 // Configure must implement IEntityTypeConfiguration interface method
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        builder.HasIndex(t => t.IsCompleted);
    }
}
