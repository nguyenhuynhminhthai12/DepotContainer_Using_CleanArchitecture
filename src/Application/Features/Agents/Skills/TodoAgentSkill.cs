using TechSpherex.CleanArchitecture.Application.Abstractions.Agents;
using TechSpherex.CleanArchitecture.Application.Abstractions.Data;
using TechSpherex.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace TechSpherex.CleanArchitecture.Application.Features.Agents.Skills;

/// <summary>
/// Skill agent mẫu quản lý các mục việc thông qua lệnh ngôn ngữ tự nhiên.
/// Minh họa cách tích hợp skill agent với các CQRS handler hiện có.
/// </summary>
public sealed class TodoAgentSkill(IAppDbContext dbContext) : ISkillAgent
{
    /// <inheritdoc/>
    public string SkillId => "todo-manager";

    /// <inheritdoc/>
    public string Name => "Todo Manager";

    /// <inheritdoc/>
    public string Description => "Quản lý các mục việc — tạo, liệt kê, đánh dấu hoàn thành và xóa bằng ngôn ngữ tự nhiên.";

    /// <inheritdoc/>
    public IReadOnlyList<string> ExamplePrompts =>
    [
        "Hiển thị tất cả việc cần làm",
        "Tạo một mục việc: Review PR #42",
        "Đánh dấu mục việc là đã hoàn thành",
        "Xóa tất cả việc đã hoàn thành",
        "Tôi có bao nhiêu mục việc?"
    ];

    /// <inheritdoc/>
    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var prompt = context.Prompt.ToLowerInvariant().Trim();

        // Phát hiện intent đơn giản (trong môi trường sản xuất, dùng LLM)
        return prompt switch
        {
            var p when p.Contains("list") || p.Contains("show") || p.Contains("all") =>
                await ListTodosAsync(cancellationToken),

            var p when p.Contains("create") || p.Contains("add") || p.Contains("new") =>
                await CreateTodoAsync(context, cancellationToken),

            var p when p.Contains("complete") || p.Contains("done") || p.Contains("finish") =>
                await CompleteTodoAsync(context, cancellationToken),

            var p when p.Contains("delete") || p.Contains("remove") =>
                await DeleteCompletedAsync(cancellationToken),

            var p when p.Contains("count") || p.Contains("how many") =>
                await CountTodosAsync(cancellationToken),

            _ => AgentResult.NeedsMoreInfo(
                "Tôi có thể giúp bạn quản lý mục việc. Thử:\n" +
                "- 'Hiển thị tất cả việc cần làm'\n" +
                "- 'Tạo một mục việc: <tiêu đề>'\n" +
                "- 'Hoàn thành mục việc: <tiêu đề>'\n" +
                "- 'Xóa việc đã hoàn thành'\n" +
                "- 'Tôi có bao nhiêu mục việc?'")
        };
    }

    /// <summary>
    /// Trích xuất tiêu đề từ prompt dựa trên vị trí dấu phẩy (colon).
    /// </summary>
    /// <param name="prompt">Prompt gốc của người dùng.</param>
    /// <param name="colonIndex">Vị trí chỉ số của dấu phẩy.</param>
    /// <returns>Tiêu đề đã trích xuất, hoặc null nếu không tìm thấy.</returns>
    private static string? ExtractTitle(string prompt, int colonIndex)
    {
        if (colonIndex >= 0)
            return prompt[(colonIndex + 1)..].Trim();

        var parts = prompt.Split(' ');
        return parts.Length > 2 ? string.Join(' ', parts.Skip(2)) : null;
    }

    /// <summary>
    /// Liệt kê tối đa 20 mục việc gần đây nhất.
    /// </summary>
    private async Task<AgentResult> ListTodosAsync(CancellationToken ct)
    {
        var todos = await dbContext.Todos.OrderByDescending(t => t.CreatedAt).Take(20).ToListAsync(ct);

        if (todos.Count == 0)
            return AgentResult.Success("Bạn chưa có mục việc nào. Dùng 'Tạo một mục việc: <tiêu đề>' để thêm.");

        var list = todos.Select((t, i) =>
            $"{i + 1}. [{(t.IsCompleted ? "✓" : " ")}] {t.Title}").ToList();

        return AgentResult.Success(
            $"Tìm thấy {todos.Count} mục việc:",
            new { Todos = list, Total = todos.Count });
    }

    /// <summary>
    /// Tạo mới một mục việc từ prompt.
    /// </summary>
    private async Task<AgentResult> CreateTodoAsync(AgentContext context, CancellationToken ct)
    {
        var prompt = context.Prompt;
        var colonIndex = prompt.IndexOf(':');
        var title = ExtractTitle(prompt, colonIndex);

        if (string.IsNullOrWhiteSpace(title))
            return AgentResult.NeedsMoreInfo("Vui lòng cung cấp tiêu đề. Ví dụ: 'Tạo một mục việc: Review PR #42'");

        var todo = new TodoItem { Title = title };
        dbContext.Todos.Add(todo);
        await dbContext.SaveChangesAsync(ct);

        return AgentResult.Success(
            $"Đã tạo mục việc: \"{title}\"",
            new { todo.Id, Title = title });
    }

    /// <summary>
    /// Đánh dấu một mục việc chưa hoàn thành là đã hoàn thành.
    /// </summary>
    private async Task<AgentResult> CompleteTodoAsync(AgentContext context, CancellationToken ct)
    {
        var prompt = context.Prompt;
        var colonIndex = prompt.IndexOf(':');
        var search = colonIndex >= 0 ? prompt[(colonIndex + 1)..].Trim() : null;

        if (string.IsNullOrWhiteSpace(search))
            return AgentResult.NeedsMoreInfo("Vui lòng chỉ rõ mục việc cần hoàn thành. Ví dụ: 'Hoàn thành mục việc: Review PR #42'");

        var todo = await dbContext.Todos
            .FirstOrDefaultAsync(t => t.Title.Contains(search, StringComparison.OrdinalIgnoreCase) && !t.IsCompleted, ct);

        if (todo is null)
            return AgentResult.Failure($"Không tìm thấy mục việc chưa hoàn thành nào khớp: \"{search}\"");

        todo.MarkAsCompleted();
        await dbContext.SaveChangesAsync(ct);

        return AgentResult.Success($"Đã hoàn thành: \"{todo.Title}\" ✓");
    }

    /// <summary>
    /// Xóa tất cả các việc đã hoàn thành.
    /// </summary>
    private async Task<AgentResult> DeleteCompletedAsync(CancellationToken ct)
    {
        var completed = await dbContext.Todos.Where(t => t.IsCompleted).ToListAsync(ct);

        if (completed.Count == 0)
            return AgentResult.Success("Không có việc đã hoàn thành để xóa.");

        dbContext.Todos.RemoveRange(completed);
        await dbContext.SaveChangesAsync(ct);

        return AgentResult.Success($"Đã xóa {completed.Count} việc đã hoàn thành.");
    }

    /// <summary>
    /// Đếm tổng số mục việc, số đã hoàn thành và số chưa hoàn thành.
    /// </summary>
    private async Task<AgentResult> CountTodosAsync(CancellationToken ct)
    {
        var total = await dbContext.Todos.CountAsync(ct);
        var completed = await dbContext.Todos.CountAsync(t => t.IsCompleted, ct);
        var pending = total - completed;

        return AgentResult.Success(
            $"Bạn có {total} mục việc: {pending} chưa hoàn thành, {completed} đã hoàn thành.",
            new { Total = total, Pending = pending, Completed = completed });
    }
}