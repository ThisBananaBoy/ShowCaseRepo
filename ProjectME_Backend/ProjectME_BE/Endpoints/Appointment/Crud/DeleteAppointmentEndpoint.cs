using FastEndpoints;
using Microsoft.Extensions.Logging;
using ProjectME_BE.Application.Services.UserContext;
using ProjectME_BE.Domain.Model.Appointment;
using ProjectME_BE.Domain.Primitives;

namespace ProjectME_BE.Endpoints.Appointment.Crud;

public class DeleteAppointmentEndpoint : Endpoint<DeleteAppointmentRequest>
{
    private readonly IRepository<AppointmentAggregate> _appointmentRepository;
    private readonly IUserContextService _userContext;
    private readonly ILogger<DeleteAppointmentEndpoint> _logger;

    public DeleteAppointmentEndpoint(
        IRepository<AppointmentAggregate> appointmentRepository,
        IUserContextService userContext,
        ILogger<DeleteAppointmentEndpoint> logger
    )
    {
        _appointmentRepository = appointmentRepository;
        _userContext = userContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Delete("/appointments/{id}");
        // Protected endpoint - requires authentication
    }

    public override async Task HandleAsync(DeleteAppointmentRequest req, CancellationToken ct)
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
            _appointmentRepository.Delete(appointment);
            await _appointmentRepository.SaveChangesAsync(ct);
            await Send.NoContentAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete appointment");
            await Send.ErrorsAsync(500, ct);
            return;
        }
    }
}

public class DeleteAppointmentRequest
{
    public Guid Id { get; set; }
}
