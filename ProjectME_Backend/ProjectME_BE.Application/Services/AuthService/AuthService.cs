using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace ProjectME_BE.Application.Services.AuthService;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IHttpClientFactory httpClientFactory, ILogger<AuthService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("AuthServer");
        _logger = logger;
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogInformation("Calling Auth Server: {Endpoint}", endpoint);

            var response = await _httpClient.PostAsJsonAsync(endpoint, request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Auth Server returned error status {StatusCode} for endpoint {Endpoint}. Response body: {ErrorBody}",
                    (int)response.StatusCode,
                    endpoint,
                    errorBody
                );
                response.EnsureSuccessStatusCode();
            }

            var result = await response.Content.ReadFromJsonAsync<TResponse>(
                cancellationToken: cancellationToken
            );
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Auth Server endpoint: {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<TResponse?> GetAsync<TResponse>(
        string endpoint,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogInformation("Calling Auth Server: {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(endpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Auth Server returned error status {StatusCode} for endpoint {Endpoint}. Response body: {ErrorBody}",
                    (int)response.StatusCode,
                    endpoint,
                    errorBody
                );
                response.EnsureSuccessStatusCode();
            }

            var result = await response.Content.ReadFromJsonAsync<TResponse>(
                cancellationToken: cancellationToken
            );
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Auth Server endpoint: {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(
        string endpoint,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogInformation("Calling Auth Server: {Endpoint}", endpoint);

            var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Auth Server returned error status {StatusCode} for endpoint {Endpoint}. Response body: {ErrorBody}",
                    (int)response.StatusCode,
                    endpoint,
                    errorBody
                );
                response.EnsureSuccessStatusCode();
            }

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Auth Server endpoint: {Endpoint}", endpoint);
            throw;
        }
    }
}
