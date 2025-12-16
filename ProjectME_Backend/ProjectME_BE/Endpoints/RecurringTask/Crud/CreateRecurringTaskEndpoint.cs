using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;
using ProjectME_BE.Domain.Model.RecurringTask;
using ProjectME_BE.Domain.Primitives;

namespace ProjectME_BE.Endpoints.RecurringTask.Crud;

public class CreateRecurringTaskEndpoint : Endpoint<CreateRecurringTaskRequest, RecurringTaskResponse>
{
    private readonly IRepository<RecurringTaskAggregate> _recurringTaskRepository;
    private readonly IUserContextService _userContext;
    private readonly ILogger<CreateRecurringTaskEndpoint> _logger;

    public CreateRecurringTaskEndpoint(
        IRepository<RecurringTaskAggregate> recurringTaskRepository,
        IUserContextService userContext,
        ILogger<CreateRecurringTaskEndpoint> logger
    )
    {
        _recurringTaskRepository = recurringTaskRepository;
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/recurring-tasks");
        // Protected endpoint - requires authentication
    }

    public override async Task HandleAsync(CreateRecurringTaskRequest req, CancellationToken ct)
    {
        var userId = _userContext.GetUserId();
        if (!userId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            var recurringTask = new RecurringTaskAggregate(
                userId.Value,
                req.Name,
                req.AssignedDates ?? new List<DateTimeOffset>()
            );

            _recurringTaskRepository.Add(recurringTask);
            await _recurringTaskRepository.SaveChangesAsync(ct);

            var response = new RecurringTaskResponse
            {
                Id = recurringTask.Id,
                UserId = recurringTask.UserId,
                Name = recurringTask.Name,
                AssignedDates = recurringTask.AssignedDates.ToList()
            };

            await Send.OkAsync(response, ct);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid recurring task data");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create recurring task");
            await Send.ErrorsAsync(500, ct);
            return;
        }
    }
}

public class CreateRecurringTaskRequest
{
    public string Name { get; set; } = string.Empty;
    public List<DateTimeOffset>? AssignedDates { get; set; }
}
