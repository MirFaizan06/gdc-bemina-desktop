using Microsoft.Extensions.DependencyInjection;

namespace CollegeAdmin.Application;

public static class DependencyInjection
{
    /// <summary>
    /// No use cases exist yet — this is Stage 2 (bootstrap) scaffolding only. Stage 6 onward
    /// register real application services here as they're built, so the composition root in
    /// CollegeAdmin.Desktop never needs to change its shape.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services) => services;
}
