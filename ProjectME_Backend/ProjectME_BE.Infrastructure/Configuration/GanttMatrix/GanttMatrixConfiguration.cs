using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectME_BE.Domain.Model.GanttMatrix;

namespace ProjectME_BE.Infrastructure.Configuration.GanttMatrix;

public class GanttMatrixConfiguration : IEntityTypeConfiguration<GanttMatrixAggregate>
{
    public void Configure(EntityTypeBuilder<GanttMatrixAggregate> builder)
    {
        
    }
}

