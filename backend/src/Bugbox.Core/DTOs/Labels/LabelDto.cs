namespace Bugbox.Core.DTOs.Labels;

public record LabelDto(Guid Id, Guid ProjectId, string Name, string Color);
