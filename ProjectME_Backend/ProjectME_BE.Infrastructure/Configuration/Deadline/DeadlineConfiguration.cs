using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectME_BE.Domain.Model.Deadlines;

namespace ProjectME_BE.Infrastructure.Configuration.Deadline;

public class DeadlineConfiguration : IEntityTypeConfiguration<DeadlineEntity>
{
    public void Configure(EntityTypeBuilder<DeadlineEntity> builder)
    {
        builder.ToTable("deadlines");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).IsRequired().ValueGeneratedNever();

        builder.Property(d => d.DeadlineDate).IsRequired();
        builder.Property(d => d.Reason).HasMaxLength(1000);
        builder.Property(d => d.Type).IsRequired();
        builder.Property(d => d.ProjectId);
        builder.Property(d => d.MilestoneId);
        builder.Property(d => d.TaskId);

        builder
            .HasOne(d => d.Project)
            .WithMany(p => p.Deadlines)
            .HasForeignKey(d => d.ProjectId)
            .IsRequired(false);
            
        builder
            .HasOne(d => d.Milestone)
            .WithMany(m => m.Deadlines)
            .HasForeignKey(d => d.MilestoneId)
            .IsRequired(false);
    }
}
