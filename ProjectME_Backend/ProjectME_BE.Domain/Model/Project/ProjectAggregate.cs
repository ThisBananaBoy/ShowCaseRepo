using ProjectME_BE.Domain.Common;
using ProjectME_BE.Domain.Model.Deadlines;
using ProjectME_BE.Domain.Model.Milestone;
using ProjectME_BE.Domain.Model.Task;

namespace ProjectME_BE.Domain.Model.Project;

public class ProjectAggregate : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public StatusTypes Status { get; private set; }
    public DateTimeOffset StartDate { get; private set; }
    public DateTimeOffset? LastDeadlineDate { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public virtual ICollection<MilestoneEntity> Milestones { get; private set; } = [];
    public virtual ICollection<TaskAggregate> Tasks { get; private set; } = [];
    public virtual ICollection<DeadlineEntity> Deadlines { get; private set; } = [];

    private ProjectAggregate() { }

    public ProjectAggregate(
        Guid userId,
        string name,
        string description,
        StatusTypes status,
        DateTimeOffset startDate,
        DateTimeOffset? lastDeadlineDate,
        DateTimeOffset? completedAt
    )
    {
        UserId = userId;
        Name = name;
        Description = description;
        Status = status;
        StartDate = startDate;
        LastDeadlineDate = lastDeadlineDate;
        CompletedAt = completedAt;
    }

    public void AddMilestone(
        string name,
        string description,
        DateTimeOffset? startDate,
        DateTimeOffset? lastDeadlineDate,
        DateTimeOffset? completedAt
    )
    {
        Milestones.Add(
            new MilestoneEntity(Id, name, description, startDate, lastDeadlineDate, completedAt)
        );
    }

    public void AddDeadline(DateTimeOffset deadlineDate, string reason)
    {
        var deadline = new DeadlineEntity(
            deadlineDate,
            reason,
            DeadlineType.Project,
            projectId: Id
        );
        LastDeadlineDate = deadlineDate;
        Deadlines.Add(deadline);
    }

    public void UpdateTimePeriod(DateTimeOffset startDate, DateTimeOffset? endDate, string reason)
    {
        StartDate = startDate;

        if (!endDate.HasValue)
        {
            LastDeadlineDate = null;
            return;
        }

        var oldLastDeadlineDate = LastDeadlineDate;

        if (endDate > LastDeadlineDate || LastDeadlineDate == null)
        {
            // Nach hinten verschoben: Neue Deadline erstellen
            LastDeadlineDate = endDate;
            var deadline = new DeadlineEntity(
                endDate.Value,
                reason,
                DeadlineType.Project,
                projectId: Id
            );
            Deadlines.Add(deadline);
        }
        else if (endDate < LastDeadlineDate)
        {
            // Nach vorne verschoben: Deadlines danach löschen
            var deadlinesToRemove = Deadlines.Where(d => d.DeadlineDate > endDate.Value).ToList();

            foreach (var deadline in deadlinesToRemove)
            {
                Deadlines.Remove(deadline);
            }

            // Neue Deadline hinzufügen
            LastDeadlineDate = endDate;
            var newDeadline = new DeadlineEntity(
                endDate.Value,
                reason,
                DeadlineType.Project,
                projectId: Id
            );
            Deadlines.Add(newDeadline);
        }
    }

    public void RemoveMilestone(Guid milestoneId)
    {
        var milestone = Milestones.FirstOrDefault(m => m.Id == milestoneId);
        if (milestone != null)
        {
            Milestones.Remove(milestone);
        }
    }

    public void MoveMilestone(
        Guid milestoneId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        string reason
    )
    {
        var milestone = Milestones.FirstOrDefault(m => m.Id == milestoneId);
        if (milestone != null)
        {
            milestone.UpdateDates(startDate, endDate, reason);
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

    public void UpdateStatus(StatusTypes status)
    {
        Status = status;
    }

    public void MarkAsCompleted()
    {
        Status = StatusTypes.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsActive()
    {
        Status = StatusTypes.Active;
        CompletedAt = null;
    }

    public void MarkAsPaused()
    {
        Status = StatusTypes.Paused;
    }

    public void MarkAsArchived()
    {
        Status = StatusTypes.Archived;
    }
}
