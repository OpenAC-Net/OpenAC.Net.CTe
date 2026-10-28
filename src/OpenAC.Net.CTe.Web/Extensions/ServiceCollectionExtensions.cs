using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.CTe.Storage;

namespace OpenAC.Net.CTe.Web;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCTe(this IServiceCollection services, Action<CTeConfig> configure,
        Action<CTeWebOptions>? infrastructure = null)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.AddSingleton<ICTeConfigurationProvider<CTeConfig>>(new DefaultCTeConfigurationProvider(configure));
        services.TryAddSingleton<ICTeCertificateProvider, DefaultCTeCertificateProvider>();
        return AddInfrastructure(services, infrastructure);
    }

    public static IServiceCollection AddOpenCTeWebMultiTenant<TConfigurationProvider, TCertificateProvider>(
        this IServiceCollection services, Action<CTeWebOptions>? infrastructure = null)
        where TConfigurationProvider : class, ICTeConfigurationProvider<CTeConfig>
        where TCertificateProvider : class, ICTeCertificateProvider
    {
        services.AddScoped<ICTeConfigurationProvider<CTeConfig>, TConfigurationProvider>();
        services.AddScoped<ICTeCertificateProvider, TCertificateProvider>();
        return AddInfrastructure(services, infrastructure);
    }

    private static IServiceCollection AddInfrastructure(IServiceCollection services, Action<CTeWebOptions>? configure)
    {
        var options = services.AddOptions<CTeWebOptions>();
        if (configure != null) options.Configure(configure);
        options.Validate(value =>
                value.TimeoutOperacao > TimeSpan.Zero && value.TimeoutTentativa > TimeSpan.Zero && value.MargemValidadeCertificado >= TimeSpan.Zero
                                                      && value.TempoVidaHandler >= TimeSpan.FromSeconds(1) &&
                                                      value.TempoVidaConexaoPool > TimeSpan.Zero
                                                      && (!value.PersistenciaHabilitada ||
                                                          !string.IsNullOrWhiteSpace(value.DiretorioDocumentos)),
            "Opções de infraestrutura CT-e inválidas.");
        services.AddHttpClient();
        services.TryAddSingleton<CTeHttpClientRegistry>();
        services.TryAddEnumerable(ServiceDescriptor
            .Singleton<IConfigureOptions<HttpClientFactoryOptions>, CTeHttpClientOptions>());
        services.TryAddSingleton<ICTeDocumentStore, DesktopCTeDocumentStore>();
        services.TryAddScoped<IOpenCTeClientFactory, OpenCTeClientFactory>();
        return services;
    }
}