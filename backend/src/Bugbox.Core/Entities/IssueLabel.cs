namespace Bugbox.Core.Entities;

public class IssueLabel
{
    public Guid IssueId { get; set; }
    public Issue Issue { get; set; } = null!;

    public Guid LabelId { get; set; }
    public Label Label { get; set; } = null!;
}
