using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Application.Features.Containers;

public sealed record ContainerResponse(
    Guid Id,
    string ContainerNumber,
    Guid ContainerTypeId,
    string IsoCode,
    int SizeFeet,
    decimal MaxWeightKg,
    decimal TareWeightKg,
    DateTimeOffset ManufactureDate,
    string Owner,
    string Condition);

public sealed record CreateContainerCommand(
    string ContainerNumber,
    Guid ContainerTypeId,
    string IsoCode,
    int SizeFeet,
    decimal MaxWeightKg,
    decimal TareWeightKg,
    DateTimeOffset ManufactureDate,
    string Owner,
    string Condition) : ICommand<Result<ContainerResponse>>;

public sealed record GetContainerByNumberQuery(string ContainerNumber)
    : IQuery<Result<ContainerResponse>>;

public sealed record GetContainersQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? LineOperatorId = null,
    string? Condition = null,
    string? Search = null) : IQuery<Result<PagedResult<ContainerResponse>>>;