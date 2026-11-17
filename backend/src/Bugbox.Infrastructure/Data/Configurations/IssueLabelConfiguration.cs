using Bugbox.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bugbox.Infrastructure.Data.Configurations;

public class IssueLabelConfiguration : IEntityTypeConfiguration<IssueLabel>
{
    public void Configure(EntityTypeBuilder<IssueLabel> builder)
    {
        builder.ToTable("IssueLabels");

        builder.HasKey(il => new { il.IssueId, il.LabelId });

        builder.HasOne(il => il.Issue)
            .WithMany(i => i.IssueLabels)
            .HasForeignKey(il => il.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(il => il.Label)
            .WithMany(l => l.IssueLabels)
            .HasForeignKey(il => il.LabelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(il => il.IssueId);
        builder.HasIndex(il => il.LabelId);
    }
}
