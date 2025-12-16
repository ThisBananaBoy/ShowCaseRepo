using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;
using ProjectME_BE.Domain.Model.RecurringTask;
using ProjectME_BE.Domain.Primitives;

namespace ProjectME_BE.Endpoints.RecurringTask.Crud;

public class UpdateRecurringTaskEndpoint : Endpoint<UpdateRecurringTaskRequest, RecurringTaskResponse>
{
    private readonly IRepository<RecurringTaskAggregate> _recurringTaskRepository;
    private readonly IUserContextService _userContext;
    private readonly ILogger<UpdateRecurringTaskEndpoint> _logger;

    public UpdateRecurringTaskEndpoint(
        IRepository<RecurringTaskAggregate> recurringTaskRepository,
        IUserContextService userContext,
        ILogger<UpdateRecurringTaskEndpoint> logger
    )
    {
        _recurringTaskRepository = recurringTaskRepository;
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Put("/recurring-tasks/{id}");
        // Protected endpoint - requires authentication
    }

    public override async Task HandleAsync(UpdateRecurringTaskRequest req, CancellationToken ct)
    {
        var userId = _userContext.GetUserId();
        if (!userId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var recurringTask = await _recurringTaskRepository.GetByIdAsync(req.Id, ct);
        if (recurringTask == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (recurringTask.UserId != userId.Value)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(req.Name))
            {
                recurringTask.UpdateName(req.Name);
            }

            if (req.AssignedDates != null)
            {
                recurringTask.UpdateAssignedDates(req.AssignedDates);
            }

            _recurringTaskRepository.Update(recurringTask);
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
            _logger.LogWarning(ex, "Invalid recurring task update data");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update recurring task");
            await Send.ErrorsAsync(500, ct);
            return;
        }
    }
}

public class UpdateRecurringTaskRequest
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public List<DateTimeOffset>? AssignedDates { get; set; }
}
