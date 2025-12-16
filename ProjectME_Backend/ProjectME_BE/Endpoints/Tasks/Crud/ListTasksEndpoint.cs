using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;
using ProjectME_BE.Domain.Model.Task;
using ProjectME_BE.Domain.Primitives;

namespace ProjectME_BE.Endpoints.Tasks.Crud;

public class ListTasksEndpoint : EndpointWithoutRequest<List<TaskResponse>>
{
    private readonly IRepository<TaskAggregate> _taskRepository;
    private readonly IUserContextService _userContext;
    private readonly ILogger<ListTasksEndpoint> _logger;

    public ListTasksEndpoint(
        IRepository<TaskAggregate> taskRepository,
        IUserContextService userContext,
        ILogger<ListTasksEndpoint> logger
    )
    {
        _taskRepository = taskRepository;
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/tasks");
        // Protected endpoint - requires authentication
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = _userContext.GetUserId();
        if (!userId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var tasks = await _taskRepository.GetWhere(t => t.UserId == userId.Value).ToListAsync(ct);

        var response = tasks
            .Select(t => new TaskResponse
            {
                Id = t.Id,
                UserId = t.UserId,
                ProjectId = t.ProjectId,
                MilestoneId = t.MilestoneId,
                AssignedUserId = t.AssignedUserId,
                Name = t.Name,
                Status = t.Status,
                Priority = t.Priority,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                DueDate = t.DueDate,
                CompletedAt = t.CompletedAt,
            })
            .ToList();

        await Send.OkAsync(response, ct);
    }
}

public class TaskResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? MilestoneId { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Status { get; set; }
    public int? Priority { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
