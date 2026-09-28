using CollegeAdmin.Application.Auth;
using CollegeAdmin.Application.Caching;
using CollegeAdmin.Application.Connectivity;
using CollegeAdmin.Application.Notifications;
using CollegeAdmin.Application.Updates;
using CollegeAdmin.Infrastructure.Api;
using CollegeAdmin.Infrastructure.Auth;
using CollegeAdmin.Infrastructure.Caching;
using CollegeAdmin.Infrastructure.Connectivity;
using CollegeAdmin.Infrastructure.Notifications;
using CollegeAdmin.Infrastructure.Updates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeAdmin.Infrastructure;

public static class DependencyInjection
{
    /// <summary>currentAppVersion is passed in rather than read here via
    /// Assembly.GetEntryAssembly() because that call only resolves correctly from the real
    /// executable's own composition root (App.xaml.cs) — inside a test host it would return the
    /// test runner's version instead, silently breaking update-check tests.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string currentAppVersion)
    {
        var baseUrl = configuration["Api:BaseUrl"]
            ?? throw new InvalidOperationException("Configuration key 'Api:BaseUrl' is required but was not set.");

        // AuthSessionService is registered once and exposed through both interfaces it
        // implements, so the app has exactly one session/token instance regardless of which
        // interface a consumer asks for.
        services.AddSingleton<ISecureTokenStore, DpapiTokenStore>();
        services.AddSingleton<AuthSessionService>();
        services.AddSingleton<IAuthSessionService>(sp => sp.GetRequiredService<AuthSessionService>());
        services.AddSingleton<IAccessTokenProvider>(sp => sp.GetRequiredService<AuthSessionService>());

        // Unauthenticated client for AuthSessionService's own calls (login/refresh/me/logout) and
        // for UpdateService's version check — deliberately has no AuthHeaderHandler attached; see
        // AuthSessionService's remarks. Both must work before login, so neither can require a token.
        services.AddHttpClient("AuthApi", client => client.BaseAddress = new Uri(baseUrl));

        var githubOwner = configuration["Updates:GitHubOwner"]
            ?? throw new InvalidOperationException("Configuration key 'Updates:GitHubOwner' is required but was not set.");
        var githubRepo = configuration["Updates:GitHubRepo"]
            ?? throw new InvalidOperationException("Configuration key 'Updates:GitHubRepo' is required but was not set.");
        services.AddSingleton<IUpdateService>(sp =>
            new UpdateService(sp.GetRequiredService<IHttpClientFactory>(), currentAppVersion, githubOwner, githubRepo));

        // Final Convergence Phase P1-16/P1-17: connectivity/toast/lookup-cache infrastructure.
        // ConnectivityService/ToastService are plain in-memory singletons (no HTTP of their own);
        // ConnectivityTrackingHandler observes the real authenticated-client traffic below.
        services.AddSingleton<IConnectivityService, ConnectivityService>();
        services.AddSingleton<IToastService, ToastService>();
        services.AddSingleton<ILookupCache, LookupCache>();
        services.AddTransient<ConnectivityTrackingHandler>();

        // Authenticated client for everything else (IApiClient) — AuthHeaderHandler attaches
        // whatever token AuthSessionService currently holds to every request automatically;
        // ConnectivityTrackingHandler then observes the real response/failure for that same call.
        services.AddTransient<AuthHeaderHandler>();
        services.AddHttpClient<IApiClient, ApiClient>(client => client.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<AuthHeaderHandler>()
            .AddHttpMessageHandler<ConnectivityTrackingHandler>();

        return services;
    }
}
