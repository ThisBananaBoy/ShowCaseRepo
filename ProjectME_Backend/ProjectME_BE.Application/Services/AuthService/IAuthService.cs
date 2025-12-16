namespace ProjectME_BE.Application.Services.AuthService;

public interface IAuthService
{
    Task<TResponse?> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken = default
    );

    Task<TResponse?> GetAsync<TResponse>(
        string endpoint,
        CancellationToken cancellationToken = default
    );

    Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default);
}
