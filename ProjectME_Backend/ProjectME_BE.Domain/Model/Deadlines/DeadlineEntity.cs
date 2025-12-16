using ProjectME_BE.Domain.Common;
using ProjectME_BE.Domain.Model.Milestone;
using ProjectME_BE.Domain.Model.Project;
using ProjectME_BE.Domain.Model.Task;

namespace ProjectME_BE.Domain.Model.Deadlines;

public class DeadlineEntity : BaseEntity
{
    public DateTimeOffset DeadlineDate { get; private set; }
    public string? Reason { get; private set; }
    public DeadlineType Type { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid? MilestoneId { get; private set; }
    public Guid? TaskId { get; private set; }

    public virtual ProjectAggregate? Project { get; private set; } = null;
    public virtual MilestoneEntity? Milestone { get; private set; } = null;
    public virtual TaskAggregate? Task { get; private set; } = null;

    private DeadlineEntity() { }

    // 🔓 Internal Constructor - nur im selben Assembly!
    internal DeadlineEntity(
        DateTimeOffset deadlineDate,
        string reason,
        DeadlineType type,
        Guid? projectId = null,
        Guid? milestoneId = null,
        Guid? taskId = null
    )
    {
        DeadlineDate = deadlineDate;
        Reason = reason;
        Type = type;

        ProjectId = projectId;
        MilestoneId = milestoneId;
        TaskId = taskId;

        // Validation - nur zur Sicherheit!
        var count =
            (ProjectId.HasValue ? 1 : 0)
            + (MilestoneId.HasValue ? 1 : 0)
            + (TaskId.HasValue ? 1 : 0);

        if (count != 1)
            throw new InvalidOperationException(
                "Internal error: Deadline must reference exactly one entity"
            );
    }
}
