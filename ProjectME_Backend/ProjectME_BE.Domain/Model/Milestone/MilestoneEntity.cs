using ProjectME_BE.Domain.Common;
using ProjectME_BE.Domain.Model.Deadlines;
using ProjectME_BE.Domain.Model.Project;
using ProjectME_BE.Domain.Model.Task;

namespace ProjectME_BE.Domain.Model.Milestone;

public class MilestoneEntity : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset? StartDate { get; private set; }
    public DateTimeOffset? LastDeadlineDate { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    // Navigation properties
    public virtual ProjectAggregate Project { get; private set; } = null!;
    public virtual ICollection<TaskAggregate> Tasks { get; private set; } = [];
    public virtual ICollection<DeadlineEntity> Deadlines { get; private set; } = [];

    private MilestoneEntity() { }

    public MilestoneEntity(
        Guid projectId,
        string name,
        string description,
        DateTimeOffset? startDate,
        DateTimeOffset? lastDeadlineDate,
        DateTimeOffset? completedAt
    )
    {
        ProjectId = projectId;
        Name = name;
        Description = description;
        StartDate = startDate;
        LastDeadlineDate = lastDeadlineDate;
        CompletedAt = completedAt;
    }

    public void UpdateDates(DateTimeOffset startDate, DateTimeOffset endDate, string reason)
    {
        StartDate = startDate;
        var oldLastDeadlineDate = LastDeadlineDate;

        if (endDate > LastDeadlineDate || LastDeadlineDate == null)
        {
            // Nach hinten verschoben: Neue Deadline erstellen
            LastDeadlineDate = endDate;
            var deadline = new DeadlineEntity(
                endDate,
                reason,
                DeadlineType.Milestone,
                milestoneId: Id
            );
            Deadlines.Add(deadline);
        }
        else if (endDate < LastDeadlineDate)
        {
            // Nach vorne verschoben: Deadlines danach löschen
            var deadlinesToRemove = Deadlines.Where(d => d.DeadlineDate > endDate).ToList();

            foreach (var deadline in deadlinesToRemove)
            {
                Deadlines.Remove(deadline);
            }

            // Neue Deadline hinzufügen
            LastDeadlineDate = endDate;
            var newDeadline = new DeadlineEntity(
                endDate,
                reason,
                DeadlineType.Milestone,
                milestoneId: Id
            );
            Deadlines.Add(newDeadline);
        }
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        Name = name;
    }

    public void UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));
        Description = description;
    }

    public void MarkAsCompleted()
    {
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsIncomplete()
    {
        CompletedAt = null;
    }
}
