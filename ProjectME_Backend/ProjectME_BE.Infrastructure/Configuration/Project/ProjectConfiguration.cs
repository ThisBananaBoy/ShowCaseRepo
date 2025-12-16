using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectME_BE.Domain.Model.Project;

namespace ProjectME_BE.Infrastructure.Configuration.Project;

public class ProjectConfiguration : IEntityTypeConfiguration<ProjectAggregate>
{
    public void Configure(EntityTypeBuilder<ProjectAggregate> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).IsRequired().ValueGeneratedNever();

        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.Name).IsRequired().HasMaxLength(255);
        builder.Property(p => p.Description).IsRequired().HasMaxLength(1000);
        builder.Property(p => p.Status).IsRequired();
        builder.Property(p => p.StartDate).IsRequired();
        builder.Property(p => p.LastDeadlineDate);
        builder.Property(p => p.CompletedAt);

        builder.HasMany(p => p.Milestones).WithOne().HasForeignKey(m => m.ProjectId);
        builder.HasMany(p => p.Deadlines).WithOne().HasForeignKey(d => d.ProjectId);
        
    }
}
