using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;
using ProjectME_BE.Domain.Model.Appointment;
using ProjectME_BE.Domain.Primitives;

namespace ProjectME_BE.Endpoints.Appointment.Crud;

public class UpdateAppointmentEndpoint : Endpoint<UpdateAppointmentRequest, AppointmentResponse>
{
    private readonly IRepository<AppointmentAggregate> _appointmentRepository;
    private readonly IUserContextService _userContext;
    private readonly ILogger<UpdateAppointmentEndpoint> _logger;

    public UpdateAppointmentEndpoint(
        IRepository<AppointmentAggregate> appointmentRepository,
        IUserContextService userContext,
        ILogger<UpdateAppointmentEndpoint> logger
    )
    {
        _appointmentRepository = appointmentRepository;
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Put("/appointments/{id}");
        // Protected endpoint - requires authentication
    }

    public override async Task HandleAsync(UpdateAppointmentRequest req, CancellationToken ct)
    {
        var userId = _userContext.GetUserId();
        if (!userId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var appointment = await _appointmentRepository.GetByIdAsync(req.Id, ct);
        if (appointment == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (appointment.UserId != userId.Value)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(req.Title))
            {
                appointment.UpdateTitle(req.Title);
            }

            if (req.Description != null)
            {
                appointment.UpdateDescription(req.Description);
            }

            if (req.StartTime.HasValue && req.EndTime.HasValue)
            {
                appointment.UpdateTime(req.StartTime.Value, req.EndTime.Value);
            }

            if (req.Location != null)
            {
                appointment.UpdateLocation(req.Location);
            }

            if (req.ProjectId.HasValue)
            {
                appointment.AssignToProject(req.ProjectId.Value);
            }
            else if (req.ProjectId == Guid.Empty)
            {
                appointment.UnassignFromProject();
            }

            if (req.TaskId.HasValue)
            {
                appointment.AssignToTask(req.TaskId.Value);
            }
            else if (req.TaskId == Guid.Empty)
            {
                appointment.UnassignFromTask();
            }

            if (req.Color != null)
            {
                appointment.UpdateColor(req.Color);
            }

            _appointmentRepository.Update(appointment);
            await _appointmentRepository.SaveChangesAsync(ct);

            var response = new AppointmentResponse
            {
                Id = appointment.Id,
                UserId = appointment.UserId,
                Title = appointment.Title,
                Description = appointment.Description,
                StartTime = appointment.StartTime,
                EndTime = appointment.EndTime,
                Location = appointment.Location,
                ProjectId = appointment.ProjectId,
                TaskId = appointment.TaskId,
                Color = appointment.Color
            };

            await Send.OkAsync(response, ct);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid appointment update data");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update appointment");
            await Send.ErrorsAsync(500, ct);
            return;
        }
    }
}

public class UpdateAppointmentRequest
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public string? Location { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? TaskId { get; set; }
    public string? Color { get; set; }
}
