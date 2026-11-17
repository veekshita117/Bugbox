using Bugbox.Core.Enums;

namespace Bugbox.Core.Entities;

public class Issue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid? SprintId { get; set; }
    public Sprint? Sprint { get; set; }

    /// <summary>
    /// Sequential counter per project, e.g. BUG-1, BUG-2.
    /// </summary>
    public int Number { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IssueType Type { get; set; } = IssueType.Task;
    public IssuePriority Priority { get; set; } = IssuePriority.Medium;
    public IssueStatus Status { get; set; } = IssueStatus.ToDo;

    public Guid? AssigneeId { get; set; }
    public User? Assignee { get; set; }

    public Guid ReporterId { get; set; }
    public User Reporter { get; set; } = null!;

    public int BoardOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public ICollection<IssueLabel> IssueLabels { get; set; } = new List<IssueLabel>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
