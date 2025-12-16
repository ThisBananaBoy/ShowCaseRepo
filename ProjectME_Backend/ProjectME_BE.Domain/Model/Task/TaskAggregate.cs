using ProjectME_BE.Domain.Common;
using ProjectME_BE.Domain.Model.Milestone;
using ProjectME_BE.Domain.Model.Project;

namespace ProjectME_BE.Domain.Model.Task;

public class TaskAggregate : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid? MilestoneId { get; private set; }
    public Guid? AssignedUserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Status { get; private set; }
    public int? Priority { get; private set; }
    public DateTimeOffset? StartTime { get; private set; }
    public DateTimeOffset? EndTime { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    // Navigation properties
    public virtual ProjectAggregate? Project { get; private set; } = null;
    public virtual MilestoneEntity? Milestone { get; private set; } = null;

    private TaskAggregate() { }

    public TaskAggregate(
        Guid userId,
        Guid? projectId,
        Guid? milestoneId,
        Guid? assignedUserId,
        string name,
        string? status,
        int? priority,
        DateTimeOffset? startTime,
        DateTimeOffset? endTime,
        DateTimeOffset? dueDate,
        DateTimeOffset? completedAt = null
    )
    {
        if (milestoneId.HasValue && !projectId.HasValue)
        {
            throw new ArgumentException(
                "MilestoneId cannot be set without ProjectId",
                nameof(milestoneId)
            );
        }

        UserId = userId;
        ProjectId = projectId;
        MilestoneId = milestoneId;
        AssignedUserId = assignedUserId;
        Name = name;
        Status = status;
        Priority = priority;
        StartTime = startTime;
        EndTime = endTime;
        DueDate = dueDate;
        CompletedAt = completedAt;
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name cannot be empty");
        Name = newName;
    }

    public void ChangeStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status cannot be empty");
        Status = status;
    }

    public void SetPriority(int priority)
    {
        Priority = priority;
    }

    public void AssignToProject(Guid projectId)
    {
        ProjectId = projectId;
    }

    public void AssignToMilestone(Guid milestoneId)
    {
        if (!ProjectId.HasValue)
        {
            throw new ArgumentException(
                "MilestoneId cannot be set without ProjectId",
                nameof(milestoneId)
            );
        }

        MilestoneId = milestoneId;
    }

    public void UnassignFromMilestone()
    {
        MilestoneId = null;
    }

    public void UnassignFromProject()
    {
        ProjectId = null;
    }

    public void ConvertToFloatingTask()
    {
        ProjectId = null;
        MilestoneId = null;
    }

    public void AssignToUser(Guid userId)
    {
        AssignedUserId = userId;
    }

    public void UnassignUser()
    {
        AssignedUserId = null;
    }

    public void SetDueDate(DateTimeOffset dueDate)
    {
        DueDate = dueDate;
    }

    public void ClearDueDate()
    {
        DueDate = null;
    }

    public void UpdateStartTime(DateTimeOffset? startTime)
    {
        StartTime = startTime;
    }

    public void UpdateEndTime(DateTimeOffset? endTime)
    {
        EndTime = endTime;
    }

    public void UpdateTimeRange(DateTimeOffset? startTime, DateTimeOffset? endTime)
    {
        if (endTime.HasValue && startTime.HasValue && endTime.Value <= startTime.Value)
        {
            throw new ArgumentException("EndTime must be after StartTime", nameof(endTime));
        }
        StartTime = startTime;
        EndTime = endTime;
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
