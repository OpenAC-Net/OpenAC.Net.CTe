using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.CTe.Storage;
using OpenAC.Net.DFe.Core.Storage;

namespace OpenAC.Net.CTe.Web;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenCTe(this IServiceCollection services, Action<CTeConfig> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.AddSingleton<ICTeConfigurationProvider<CTeConfig>>(new DefaultCTeConfigurationProvider(configure));
        services.TryAddSingleton<ICTeCertificateProvider, DefaultCTeCertificateProvider>();
        return AddInfrastructure(services);
    }

    public static IServiceCollection AddOpenCTeMultiTenant<TConfigurationProvider, TCertificateProvider>(
        this IServiceCollection services)
        where TConfigurationProvider : class, ICTeConfigurationProvider<CTeConfig>
        where TCertificateProvider : class, ICTeCertificateProvider
    {
        services.AddScoped<ICTeConfigurationProvider<CTeConfig>, TConfigurationProvider>();
        services.AddScoped<ICTeCertificateProvider, TCertificateProvider>();
        return AddInfrastructure(services);
    }

    private static IServiceCollection AddInfrastructure(IServiceCollection services)
    {
        services.TryAddSingleton<ICTeCertificadoProvider, CTeCertificadoProvider>();
        services.AddHttpClient();
        services.TryAddSingleton<CTeHttpClientRegistry>();
        services.TryAddEnumerable(ServiceDescriptor
            .Singleton<IConfigureOptions<HttpClientFactoryOptions>, CTeHttpClientOptions>());
        services.TryAddSingleton<IDFeDocumentStore, LocalDocumentStore>();
        services.TryAddScoped<IOpenCTeClientFactory, OpenCTeClientFactory>();
        return services;
    }
}