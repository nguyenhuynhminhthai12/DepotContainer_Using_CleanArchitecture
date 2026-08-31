namespace TechSpherex.CleanArchitecture.Domain.Common.Rules;

/// <summary>
/// Represents a business rule that can be evaluated against an entity.
/// Rules are composable building blocks for the Rule Engine.
/// </summary>
public interface IBusinessRule
{
#pragma warning disable S1135 // False positive: XML doc example contains 'Todo' string, not a TODO comment
    /// <summary>Unique rule identifier (e.g. "Todo.TitleRequired").</summary>
    string RuleCode { get; }
#pragma warning restore S1135 // False positive: XML doc example contains 'Todo' string, not a TODO comment

    /// <summary>Human-readable error message when the rule is violated.</summary>
    string Message { get; }

    /// <summary>Evaluation priority – lower values execute first.</summary>
    int Priority => 0;

    /// <summary>Evaluates the rule against the current context.</summary>
    bool IsBroken();
}
