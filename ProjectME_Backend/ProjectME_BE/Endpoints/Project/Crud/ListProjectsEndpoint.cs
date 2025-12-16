using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;
using ProjectME_BE.Domain.Model.Project;
using ProjectME_BE.Domain.Primitives;

namespace ProjectME_BE.Endpoints.Project.Crud;

public class ListProjectsEndpoint : EndpointWithoutRequest<List<ProjectResponse>>
{
    private readonly IRepository<ProjectAggregate> _projectRepository;
    private readonly IUserContextService _userContext;
    private readonly ILogger<ListProjectsEndpoint> _logger;

    public ListProjectsEndpoint(
        IRepository<ProjectAggregate> projectRepository,
        IUserContextService userContext,
        ILogger<ListProjectsEndpoint> logger
    )
    {
        _projectRepository = projectRepository;
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/projects");
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

        var projects = await _projectRepository
            .GetWhere(p => p.UserId == userId.Value)
            .ToListAsync(ct);

        var response = projects
            .Select(p => new ProjectResponse
            {
                Id = p.Id,
                UserId = p.UserId,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status.ToString(),
                StartDate = p.StartDate,
                LastDeadlineDate = p.LastDeadlineDate,
                CompletedAt = p.CompletedAt,
            })
            .ToList();

        await Send.OkAsync(response, ct);
    }
}

public class ProjectResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? LastDeadlineDate { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
