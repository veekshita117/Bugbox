using Bugbox.Core.Enums;

namespace Bugbox.Core.DTOs.Issues;

public record MoveIssueRequest(IssueStatus Status, int BoardOrder);
