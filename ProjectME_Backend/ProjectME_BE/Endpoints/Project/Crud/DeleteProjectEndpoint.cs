using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;
using ProjectME_BE.Domain.Model.Project;
using ProjectME_BE.Domain.Primitives;

namespace ProjectME_BE.Endpoints.Project.Crud;

public class DeleteProjectEndpoint : Endpoint<DeleteProjectRequest>
{
    private readonly IRepository<ProjectAggregate> _projectRepository;
    private readonly IUserContextService _userContext;
    private readonly ILogger<DeleteProjectEndpoint> _logger;

    public DeleteProjectEndpoint(
        IRepository<ProjectAggregate> projectRepository,
        IUserContextService userContext,
        ILogger<DeleteProjectEndpoint> logger
    )
    {
        _projectRepository = projectRepository;
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Delete("/projects/{id}");
        // Protected endpoint - requires authentication
    }

    public override async Task HandleAsync(DeleteProjectRequest req, CancellationToken ct)
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
            _projectRepository.Delete(project);
            await _projectRepository.SaveChangesAsync(ct);
            await Send.NoContentAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete project");
            await Send.ErrorsAsync(500, ct);
            return;
        }
    }
}

public class DeleteProjectRequest
{
    public Guid Id { get; set; }
}
