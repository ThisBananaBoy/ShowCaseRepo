using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectME_BE.Domain.Model.Milestone;

namespace ProjectME_BE.Infrastructure.Configuration.Milestone;

public class MilestoneConfiguration : IEntityTypeConfiguration<MilestoneEntity>
{
    public void Configure(EntityTypeBuilder<MilestoneEntity> builder)
    {
        builder.ToTable("milestones");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).IsRequired().ValueGeneratedNever();

        builder.Property(m => m.ProjectId).IsRequired();
        builder.Property(m => m.Name).IsRequired().HasMaxLength(255);
        builder.Property(m => m.Description).IsRequired().HasMaxLength(1000);
        builder.Property(m => m.StartDate);
        builder.Property(m => m.LastDeadlineDate);
        builder.Property(m => m.CompletedAt);

        builder
            .HasOne(m => m.Project)
            .WithMany(p => p.Milestones)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
