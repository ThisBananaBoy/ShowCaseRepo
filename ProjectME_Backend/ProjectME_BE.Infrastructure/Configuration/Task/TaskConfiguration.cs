using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectME_BE.Domain.Model.Task;

namespace ProjectME_BE.Infrastructure.Configuration.Task;

public class TaskConfiguration : IEntityTypeConfiguration<TaskAggregate>
{
    public void Configure(EntityTypeBuilder<TaskAggregate> builder)
    {
        builder.ToTable("tasks");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).IsRequired().ValueGeneratedNever();

        builder.Property(t => t.UserId).IsRequired();
        builder.Property(t => t.ProjectId);
        builder.Property(t => t.MilestoneId);
        builder.Property(t => t.AssignedUserId);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(255);
        builder.Property(t => t.Status).HasMaxLength(50);
        builder.Property(t => t.Priority);
        builder.Property(t => t.StartTime);
        builder.Property(t => t.EndTime);
        builder.Property(t => t.DueDate);
        builder.Property(t => t.CompletedAt);

        //Deletes task if project is deleted
        builder
            .HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        //Sets milestone to null if milestone is deleted
        builder
            .HasOne(t => t.Milestone)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.MilestoneId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
