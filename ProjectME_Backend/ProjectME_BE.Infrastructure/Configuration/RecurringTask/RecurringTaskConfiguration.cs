using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectME_BE.Domain.Model.RecurringTask;

namespace ProjectME_BE.Infrastructure.Configuration.RecurringTask;

public class RecurringTaskConfiguration : IEntityTypeConfiguration<RecurringTaskAggregate>
{
    public void Configure(EntityTypeBuilder<RecurringTaskAggregate> builder)
    {
        builder.ToTable("recurring_tasks");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).IsRequired().ValueGeneratedNever();

        builder.Property(r => r.UserId).IsRequired();
        builder.Property(r => r.Name).IsRequired().HasMaxLength(255);
        builder.Property(r => r.AssignedDates);
    }
}

