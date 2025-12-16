using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;
using ProjectME_BE.Domain.Model.Task;
using ProjectME_BE.Domain.Primitives;

namespace ProjectME_BE.Endpoints.Tasks.Crud;

public class UpdateTaskEndpoint : Endpoint<UpdateTaskRequest, TaskResponse>
{
    private readonly IRepository<TaskAggregate> _taskRepository;
    private readonly IUserContextService _userContext;
    private readonly ILogger<UpdateTaskEndpoint> _logger;

    public UpdateTaskEndpoint(
        IRepository<TaskAggregate> taskRepository,
        IUserContextService userContext,
        ILogger<UpdateTaskEndpoint> logger
    )
    {
        _taskRepository = taskRepository;
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Put("/tasks/{id}");
        // Protected endpoint - requires authentication
    }

    public override async Task HandleAsync(UpdateTaskRequest req, CancellationToken ct)
    {
        var userId = _userContext.GetUserId();
        if (!userId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var task = await _taskRepository.GetByIdAsync(req.Id, ct);
        if (task == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (task.UserId != userId.Value)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(req.Name))
            {
                task.Rename(req.Name);
            }

            if (!string.IsNullOrWhiteSpace(req.Status))
            {
                task.ChangeStatus(req.Status);
            }

            if (req.Priority.HasValue)
            {
                task.SetPriority(req.Priority.Value);
            }

            if (req.ProjectId.HasValue)
            {
                task.AssignToProject(req.ProjectId.Value);
            }
            else if (req.ProjectId == Guid.Empty)
            {
                task.UnassignFromProject();
            }

            if (req.MilestoneId.HasValue)
            {
                task.AssignToMilestone(req.MilestoneId.Value);
            }
            else if (req.MilestoneId == Guid.Empty)
            {
                task.UnassignFromMilestone();
            }

            if (req.AssignedUserId.HasValue)
            {
                task.AssignToUser(req.AssignedUserId.Value);
            }
            else if (req.AssignedUserId == Guid.Empty)
            {
                task.UnassignUser();
            }

            if (req.DueDate.HasValue)
            {
                task.SetDueDate(req.DueDate.Value);
            }
            else if (req.DueDate == DateTimeOffset.MinValue)
            {
                task.ClearDueDate();
            }

            if (req.StartTime.HasValue || req.EndTime.HasValue)
            {
                task.UpdateTimeRange(req.StartTime, req.EndTime);
            }

            if (req.MarkCompleted == true)
            {
                task.MarkAsCompleted();
            }
            else if (req.MarkCompleted == false)
            {
                task.MarkAsIncomplete();
            }

            _taskRepository.Update(task);
            await _taskRepository.SaveChangesAsync(ct);

            var response = new TaskResponse
            {
                Id = task.Id,
                UserId = task.UserId,
                ProjectId = task.ProjectId,
                MilestoneId = task.MilestoneId,
                AssignedUserId = task.AssignedUserId,
                Name = task.Name,
                Status = task.Status,
                Priority = task.Priority,
                StartTime = task.StartTime,
                EndTime = task.EndTime,
                DueDate = task.DueDate,
                CompletedAt = task.CompletedAt
            };

            await Send.OkAsync(response, ct);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid task update data");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update task");
            await Send.ErrorsAsync(500, ct);
            return;
        }
    }
}

public class UpdateTaskRequest
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Status { get; set; }
    public int? Priority { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? MilestoneId { get; set; }
    public Guid? AssignedUserId { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public bool? MarkCompleted { get; set; }
}
