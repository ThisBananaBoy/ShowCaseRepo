using ProjectME_BE.Domain.Common;

namespace ProjectME_BE.Domain.Model.Appointment;

public class AppointmentAggregate : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset EndTime { get; private set; }
    public string? Location { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid? TaskId { get; private set; }
    public string? Color { get; private set; }

    private AppointmentAggregate() { }

    public AppointmentAggregate(
        Guid userId,
        string title,
        string? description,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string? location,
        Guid? projectId,
        Guid? taskId,
        string? color
    )
    {
        if (endTime <= startTime)
        {
            throw new ArgumentException("EndTime must be after StartTime", nameof(endTime));
        }

        UserId = userId;
        Title = title;
        Description = description;
        StartTime = startTime;
        EndTime = endTime;
        Location = location;
        ProjectId = projectId;
        TaskId = taskId;
        Color = color;
    }

    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        Title = title;
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
    }

    public void UpdateTime(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        if (endTime <= startTime)
        {
            throw new ArgumentException("EndTime must be after StartTime", nameof(endTime));
        }
        StartTime = startTime;
        EndTime = endTime;
    }

    public void UpdateLocation(string? location)
    {
        Location = location;
    }

    public void AssignToProject(Guid projectId)
    {
        ProjectId = projectId;
    }

    public void UnassignFromProject()
    {
        ProjectId = null;
    }

    public void AssignToTask(Guid taskId)
    {
        TaskId = taskId;
    }

    public void UnassignFromTask()
    {
        TaskId = null;
    }

    public void UpdateColor(string? color)
    {
        Color = color;
    }
}
