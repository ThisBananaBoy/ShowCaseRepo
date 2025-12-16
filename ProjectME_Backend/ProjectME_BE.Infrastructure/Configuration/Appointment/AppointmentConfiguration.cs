using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectME_BE.Domain.Model.Appointment;

namespace ProjectME_BE.Infrastructure.Configuration.Appointment;

public class AppointmentConfiguration : IEntityTypeConfiguration<AppointmentAggregate>
{
    public void Configure(EntityTypeBuilder<AppointmentAggregate> builder)
    {
        builder.ToTable("appointments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).IsRequired().ValueGeneratedNever();

        builder.Property(a => a.UserId).IsRequired();
        builder.Property(a => a.Title).IsRequired().HasMaxLength(255);
        builder.Property(a => a.Description).HasMaxLength(1000);
        builder.Property(a => a.StartTime).IsRequired();
        builder.Property(a => a.EndTime).IsRequired();
        builder.Property(a => a.Location).HasMaxLength(255);
        builder.Property(a => a.ProjectId);
        builder.Property(a => a.TaskId);
        builder.Property(a => a.Color);

        
    }
}

