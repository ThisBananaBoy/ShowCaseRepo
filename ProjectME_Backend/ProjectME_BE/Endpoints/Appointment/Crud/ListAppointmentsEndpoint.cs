using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;
using ProjectME_BE.Domain.Model.Appointment;
using ProjectME_BE.Domain.Primitives;

namespace ProjectME_BE.Endpoints.Appointment.Crud;

public class ListAppointmentsEndpoint : EndpointWithoutRequest<List<AppointmentResponse>>
{
    private readonly IRepository<AppointmentAggregate> _appointmentRepository;
    private readonly IUserContextService _userContext;
    private readonly ILogger<ListAppointmentsEndpoint> _logger;

    public ListAppointmentsEndpoint(
        IRepository<AppointmentAggregate> appointmentRepository,
        IUserContextService userContext,
        ILogger<ListAppointmentsEndpoint> logger
    )
    {
        _appointmentRepository = appointmentRepository;
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/appointments");
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

        var appointments = await _appointmentRepository
            .GetWhere(a => a.UserId == userId.Value)
            .ToListAsync(ct);

        var response = appointments.Select(a => new AppointmentResponse
        {
            Id = a.Id,
            UserId = a.UserId,
            Title = a.Title,
            Description = a.Description,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            Location = a.Location,
            ProjectId = a.ProjectId,
            TaskId = a.TaskId,
            Color = a.Color
        }).ToList();

        await Send.OkAsync(response, ct);
    }
}

public class AppointmentResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string? Location { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? TaskId { get; set; }
    public string? Color { get; set; }
}
