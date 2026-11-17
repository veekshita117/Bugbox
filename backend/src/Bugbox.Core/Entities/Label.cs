namespace Bugbox.Core.Entities;

public class Label
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6366F1"; // Default indigo hex

    // Navigation properties
    public ICollection<IssueLabel> IssueLabels { get; set; } = new List<IssueLabel>();
}
