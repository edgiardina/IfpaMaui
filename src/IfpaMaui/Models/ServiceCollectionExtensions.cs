using Microsoft.Extensions.DependencyInjection;

namespace Ifpa.Models
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAllFromNamespace<T>(this IServiceCollection serviceCollection)
        {
            return serviceCollection.Scan(selector => selector
                .FromAssemblies(typeof(T).Assembly)
                .AddClasses(filter => filter.InNamespaceOf(typeof(T)))
                .AsSelf()
                .WithTransientLifetime());
        }
    }
}
