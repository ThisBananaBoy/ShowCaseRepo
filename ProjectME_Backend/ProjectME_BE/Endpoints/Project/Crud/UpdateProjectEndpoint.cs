using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;
using ProjectME_BE.Domain.Common;
using ProjectME_BE.Domain.Model.Project;
using ProjectME_BE.Domain.Primitives;

namespace ProjectME_BE.Endpoints.Project.Crud;

public class UpdateProjectEndpoint : Endpoint<UpdateProjectRequest, ProjectResponse>
{
    private readonly IRepository<ProjectAggregate> _projectRepository;
    private readonly IUserContextService _userContext;
    private readonly ILogger<UpdateProjectEndpoint> _logger;

    public UpdateProjectEndpoint(
        IRepository<ProjectAggregate> projectRepository,
        IUserContextService userContext,
        ILogger<UpdateProjectEndpoint> logger
    )
    {
        _projectRepository = projectRepository;
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Put("/projects/{id}");
        // Protected endpoint - requires authentication
    }

    public override async Task HandleAsync(UpdateProjectRequest req, CancellationToken ct)
    {
        var userId = _userContext.GetUserId();
        if (!userId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var project = await _projectRepository.GetByIdAsync(req.Id, ct);
        if (project == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (project.UserId != userId.Value)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(req.Name))
            {
                project.UpdateName(req.Name);
            }

            if (!string.IsNullOrWhiteSpace(req.Description))
            {
                project.UpdateDescription(req.Description);
            }

            if (
                !string.IsNullOrWhiteSpace(req.Status)
                && Enum.TryParse<StatusTypes>(req.Status, true, out var status)
            )
            {
                project.UpdateStatus(status);
            }

            if (req.StartDate.HasValue && req.LastDeadlineDate.HasValue)
            {
                project.UpdateTimePeriod(
                    req.StartDate.Value,
                    req.LastDeadlineDate.Value,
                    req.DeadlineReason ?? "Updated"
                );
            }
            else if (req.StartDate.HasValue)
            {
                project.UpdateTimePeriod(
                    req.StartDate.Value,
                    null,
                    req.DeadlineReason ?? "Updated"
                );
            }

            _projectRepository.Update(project);
            await _projectRepository.SaveChangesAsync(ct);

            var response = new ProjectResponse
            {
                Id = project.Id,
                UserId = project.UserId,
                Name = project.Name,
                Description = project.Description,
                Status = project.Status.ToString(),
                StartDate = project.StartDate,
                LastDeadlineDate = project.LastDeadlineDate,
                CompletedAt = project.CompletedAt,
            };

            await Send.OkAsync(response, ct);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid project update data");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update project");
            await Send.ErrorsAsync(500, ct);
            return;
        }
    }
}

public class UpdateProjectRequest
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? LastDeadlineDate { get; set; }
    public string? DeadlineReason { get; set; }
}
