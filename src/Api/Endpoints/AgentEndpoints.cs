using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using TechSpherex.CleanArchitecture.Application.Abstractions.Agents;
using TechSpherex.CleanArchitecture.Application.Abstractions.Identity;
using TechSpherex.CleanArchitecture.Application.Abstractions.Tenancy;

namespace TechSpherex.CleanArchitecture.Api.Endpoints;

/// <summary>
/// Nhóm các endpoint REST cho chức năng Skill Agent (thực thi agent bằng ngôn ngữ tự nhiên).
/// </summary>
public static class AgentEndpoints
{
    /// <summary>
    /// Đăng ký tất cả endpoint Agent vào <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">Route builder để đăng ký endpoint.</param>
    public static void MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/agents")
            .WithTags("Skill Agents")
            .RequireAuthorization();

        group.MapPost("/execute", Execute)
            .WithName("ExecuteAgent")
            .WithSummary("Thực thi skill agent bằng prompt ngôn ngữ tự nhiên");

        group.MapPost("/execute/{skillId}", ExecuteSkill)
            .WithName("ExecuteSpecificSkill")
            .WithSummary("Thực thi một skill agent cụ thể theo ID");

        group.MapGet("/skills", ListSkills)
            .WithName("ListSkills")
            .WithSummary("Liệt kê tất cả skill agent khả dụng")
            .AllowAnonymous();
    }

    /// <summary>Xử lý POST /api/agents/execute — thực thi agent với prompt.</summary>
    private static async Task<IResult> Execute(
        AgentExecuteRequest request,
        IAgentOrchestrator orchestrator,
        ICurrentUser currentUser,
        ITenantProvider tenantProvider,
        CancellationToken cancellationToken)
    {
        var context = new AgentContext
        {
            Prompt = request.Prompt,
            Parameters = request.Parameters ?? [],
            UserId = currentUser.UserId,
            TenantId = tenantProvider.TenantId,
            ConversationHistory = request.History?.Select(h =>
                new AgentMessage(h.Role, h.Content, h.Timestamp ?? DateTimeOffset.UtcNow)).ToList() ?? []
        };

        var result = await orchestrator.ExecuteAsync(context, cancellationToken);

        return result.Status switch
        {
            AgentResultStatus.Success => TypedResults.Ok(new AgentExecuteResponse(result.Status.ToString(), result.Message, result.Data, result.Metadata)),
            AgentResultStatus.NeedsMoreInfo => TypedResults.Ok(new AgentExecuteResponse(result.Status.ToString(), result.Message, null, result.Metadata)),
            AgentResultStatus.PartialSuccess => TypedResults.Ok(new AgentExecuteResponse(result.Status.ToString(), result.Message, result.Data, result.Metadata)),
            _ => TypedResults.UnprocessableEntity(new AgentExecuteResponse(result.Status.ToString(), result.Message, null, result.Metadata))
        };
    }

    /// <summary>Xử lý POST /api/agents/execute/{skillId} — thực thi skill cụ thể.</summary>
    private static async Task<IResult> ExecuteSkill(
        string skillId,
        AgentExecuteRequest request,
        IAgentOrchestrator orchestrator,
        ICurrentUser currentUser,
        ITenantProvider tenantProvider,
        CancellationToken cancellationToken)
    {
        var context = new AgentContext
        {
            Prompt = request.Prompt,
            Parameters = request.Parameters ?? [],
            UserId = currentUser.UserId,
            TenantId = tenantProvider.TenantId
        };

        var result = await orchestrator.ExecuteSkillAsync(skillId, context, cancellationToken);

        return result.Status == AgentResultStatus.Failure
            ? TypedResults.UnprocessableEntity(new AgentExecuteResponse(result.Status.ToString(), result.Message, null, result.Metadata))
            : TypedResults.Ok(new AgentExecuteResponse(result.Status.ToString(), result.Message, result.Data, result.Metadata));
    }

    /// <summary>Xử lý GET /api/agents/skills — liệt kê skill khả dụng.</summary>
    private static Microsoft.AspNetCore.Http.HttpResults.Ok<IReadOnlyList<SkillInfo>> ListSkills(IAgentOrchestrator orchestrator)
    {
        var skills = orchestrator.GetAvailableSkills();
        return TypedResults.Ok(skills);
    }
}

/// <summary>
/// Yêu cầu thực thi agent (dùng cho /execute và /execute/{skillId}).
/// </summary>
/// <param name="Prompt">Prompt ngôn ngữ tự nhiên của người dùng.</param>
/// <param name="Parameters">Các tham số có cấu trúc (tùy chọn).</param>
/// <param name="History">Lịch sử hội thoại để tương tác đa lượt (tùy chọn).</param>
public sealed record AgentExecuteRequest(
    string Prompt,
    Dictionary<string, object?>? Parameters = null,
    List<AgentMessageDto>? History = null);

/// <summary>
/// Tin nhắn trong lịch sử hội thoại của agent.
/// </summary>
/// <param name="Role">Vai trò (user/assistant/system).</param>
/// <param name="Content">Nội dung tin nhắn.</param>
/// <param name="Timestamp">Thời điểm gửi tin nhắn (tùy chọn).</param>
public sealed record AgentMessageDto(string Role, string Content, DateTimeOffset? Timestamp);

/// <summary>
/// Phản hồi từ endpoint thực thi agent.
/// </summary>
/// <param name="Status">Trạng thái kết quả.</param>
/// <param name="Message">Tin nhắn mô tả.</param>
/// <param name="Data">Dữ liệu trả về (tùy chọn).</param>
/// <param name="Metadata">Metadata bổ sung (tùy chọn).</param>
public sealed record AgentExecuteResponse(
    string Status,
    string Message,
    object? Data,
    Dictionary<string, object?>? Metadata);
