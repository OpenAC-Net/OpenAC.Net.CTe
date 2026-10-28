using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenAC.Net.CTe.DACTe.PDFSharp.Configuracao;

namespace OpenAC.Net.CTe.DACTe.PDFSharp.Web;

public static class DACTeServiceCollectionExtensions
{
    public static IServiceCollection AddDACTe(this IServiceCollection services,
        Action<DACTeConfig>? configure = null)
    {
        services.TryAddSingleton<OpenAC.Net.CTe.Web.ICTeConfigurationProvider<DACTeConfig>>(
            new DACTeConfigurationProvider(configure ?? (_ => { })));
        services.TryAddScoped<IOpenDACTeWeb, OpenDACTeWeb>();
        return services;
    }
}