using System.Net.Http.Headers;

namespace CollegeAdmin.Infrastructure.Api;

/// <summary>
/// Attached to the IApiClient HttpClient pipeline (see DependencyInjection.AddInfrastructure).
/// Every business-endpoint call goes through this transparently; no ApiClient method needs to
/// know about tokens at all.
/// </summary>
public sealed class AuthHeaderHandler(IAccessTokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetAccessTokenAsync(cancellationToken);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
