using CollegeAdmin.Application.Connectivity;

namespace CollegeAdmin.Infrastructure.Connectivity;

/// <summary>
/// Attached to the IApiClient HttpClient pipeline (see DependencyInjection.AddInfrastructure), same
/// shape as AuthHeaderHandler. Observes every real request already flowing through the app and
/// reports success/failure to IConnectivityService — deliberately not a separate polling loop.
/// </summary>
public sealed class ConnectivityTrackingHandler(IConnectivityService connectivityService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            // Reaching the server at all (regardless of status code) means connectivity is fine —
            // a 401/404/500 is a server/business error, not a network problem, and ApiEnvelopeHttp
            // already distinguishes those separately.
            connectivityService.ReportSuccess();
            return response;
        }
        catch (HttpRequestException)
        {
            connectivityService.ReportFailure();
            throw;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            connectivityService.ReportFailure();
            throw;
        }
    }
}
