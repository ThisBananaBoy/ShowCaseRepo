using FastEndpoints;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;
using ProjectME_BE.Domain.Common;
using ProjectME_BE.Domain.Model.Project;
using ProjectME_BE.Domain.Primitives;

namespace ProjectME_BE.Endpoints.Project.Crud;

public class CreateProjectEndpoint : Endpoint<CreateProjectRequest, ProjectResponse>
{
    private readonly IRepository<ProjectAggregate> _projectRepository;
    private readonly IUserContextService _userContext;
    private readonly ILogger<CreateProjectEndpoint> _logger;

    public CreateProjectEndpoint(
        IRepository<ProjectAggregate> projectRepository,
        IUserContextService userContext,
        ILogger<CreateProjectEndpoint> logger
    )
    {
        _projectRepository = projectRepository;
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/projects");
        // Protected endpoint - requires authentication
    }

    public override async Task HandleAsync(CreateProjectRequest req, CancellationToken ct)
    {
        var userId = _userContext.GetUserId();
        if (!userId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            if (!Enum.TryParse<StatusTypes>(req.Status, true, out var status))
            {
                await Send.ErrorsAsync(400, ct);
                return;
            }

            var project = new ProjectAggregate(
                userId.Value,
                req.Name,
                req.Description,
                status,
                req.StartDate,
                req.LastDeadlineDate,
                req.CompletedAt
            );

            _projectRepository.Add(project);
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
                CompletedAt = project.CompletedAt
            };

            await Send.OkAsync(response, ct);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid project data");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create project");
            await Send.ErrorsAsync(500, ct);
            return;
        }
    }
}

public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? LastDeadlineDate { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
