using Microsoft.Extensions.DependencyInjection;

namespace WPF.Helpers
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection Tap(this IServiceCollection services, Action<IServiceCollection> configure)
        {
            configure(services);
            return services;
        }
    }
}
