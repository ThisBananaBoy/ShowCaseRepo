using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;
using ProjectME_BE.Domain.Model.RecurringTask;
using ProjectME_BE.Domain.Primitives;

namespace ProjectME_BE.Endpoints.RecurringTask.Crud;

public class ListRecurringTasksEndpoint : EndpointWithoutRequest<List<RecurringTaskResponse>>
{
    private readonly IRepository<RecurringTaskAggregate> _recurringTaskRepository;
    private readonly IUserContextService _userContext;
    private readonly ILogger<ListRecurringTasksEndpoint> _logger;

    public ListRecurringTasksEndpoint(
        IRepository<RecurringTaskAggregate> recurringTaskRepository,
        IUserContextService userContext,
        ILogger<ListRecurringTasksEndpoint> logger
    )
    {
        _recurringTaskRepository = recurringTaskRepository;
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/recurring-tasks");
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

        var recurringTasks = await _recurringTaskRepository
            .GetWhere(rt => rt.UserId == userId.Value)
            .ToListAsync(ct);

        var response = recurringTasks.Select(rt => new RecurringTaskResponse
        {
            Id = rt.Id,
            UserId = rt.UserId,
            Name = rt.Name,
            AssignedDates = rt.AssignedDates.ToList()
        }).ToList();

        await Send.OkAsync(response, ct);
    }
}

public class RecurringTaskResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<DateTimeOffset> AssignedDates { get; set; } = new();
}
