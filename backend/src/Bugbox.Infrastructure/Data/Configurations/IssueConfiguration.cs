using Bugbox.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bugbox.Infrastructure.Data.Configurations;

public class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        builder.ToTable("Issues");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Number)
            .IsRequired();

        // Unique per-project issue counter, e.g. BUG-1, BUG-2
        builder.HasIndex(i => new { i.ProjectId, i.Number })
            .IsUnique();

        builder.Property(i => i.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Description)
            .HasMaxLength(5000);

        builder.Property(i => i.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.BoardOrder)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.Property(i => i.UpdatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(i => i.Project)
            .WithMany(p => p.Issues)
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Sprint)
            .WithMany(s => s.Issues)
            .HasForeignKey(i => i.SprintId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.Assignee)
            .WithMany(u => u.AssignedIssues)
            .HasForeignKey(i => i.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.Reporter)
            .WithMany(u => u.ReportedIssues)
            .HasForeignKey(i => i.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        // Required indexes: foreign keys and (ProjectId, Status)
        builder.HasIndex(i => new { i.ProjectId, i.Status });
        builder.HasIndex(i => i.ProjectId);
        builder.HasIndex(i => i.SprintId);
        builder.HasIndex(i => i.AssigneeId);
        builder.HasIndex(i => i.ReporterId);
    }
}
