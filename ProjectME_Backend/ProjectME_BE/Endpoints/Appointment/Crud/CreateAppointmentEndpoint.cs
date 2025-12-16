using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;
using ProjectME_BE.Domain.Model.Appointment;
using ProjectME_BE.Domain.Primitives;

namespace ProjectME_BE.Endpoints.Appointment.Crud;

public class CreateAppointmentEndpoint : Endpoint<CreateAppointmentRequest, AppointmentResponse>
{
    private readonly IRepository<AppointmentAggregate> _appointmentRepository;
    private readonly IUserContextService _userContext;
    private readonly ILogger<CreateAppointmentEndpoint> _logger;

    public CreateAppointmentEndpoint(
        IRepository<AppointmentAggregate> appointmentRepository,
        IUserContextService userContext,
        ILogger<CreateAppointmentEndpoint> logger
    )
    {
        _appointmentRepository = appointmentRepository;
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/appointments");
        // Protected endpoint - requires authentication
    }

    public override async Task HandleAsync(CreateAppointmentRequest req, CancellationToken ct)
    {
        var userId = _userContext.GetUserId();
        if (!userId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            var appointment = new AppointmentAggregate(
                userId.Value,
                req.Title,
                req.Description,
                req.StartTime,
                req.EndTime,
                req.Location,
                req.ProjectId,
                req.TaskId,
                req.Color
            );

            _appointmentRepository.Add(appointment);
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
                Color = appointment.Color,
            };

            await Send.OkAsync(response, ct);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid appointment data");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create appointment");
            await Send.ErrorsAsync(500, ct);
            return;
        }
    }
}

public class CreateAppointmentRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string? Location { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? TaskId { get; set; }
    public string? Color { get; set; }
}
