using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.CTe.Storage;

namespace OpenAC.Net.CTe.Web;

internal sealed class OpenCTeClientFactory(
    ICTeConfigurationProvider<CTeConfig> configurations,
    ICTeCertificateProvider certificates,
    IHttpClientFactory clients,
    CTeHttpClientRegistry registry,
    IOptions<CTeWebOptions> options,
    ILogger<CTeWebHttpTransport> logger,
    ICTeDocumentStore store) : IOpenCTeClientFactory
{
    // Compartilhada entre escopos: detecta providers que reutilizam configuração mutável.
    private static readonly ConditionalWeakTable<CTeConfig, object> Issued = new();

    public async ValueTask<OpenCTe> CreateAsync(string tenantId = "default",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Empresa não informada.", nameof(tenantId));
        cancellationToken.ThrowIfCancellationRequested();
        var configuration =
            await configurations.GetConfigurationAsync(tenantId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("O provider retornou uma configuração nula.");
        try
        {
            Issued.Add(configuration, new object());
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("O provider deve retornar uma nova configuração a cada chamada.");
        }

        await certificates.ConfigureAsync(tenantId, configuration.Certificados, cancellationToken)
            .ConfigureAwait(false);
        using (var certificate = configuration.Certificados.ObterCertificado())
            CTeWebHttpTransport.ValidateCertificate(certificate, options.Value.MargemValidadeCertificado);
        configuration.HttpTransport = new CTeWebHttpTransport(tenantId, clients, registry, options, logger);
        configuration.DocumentStore = store;
        configuration.Arquivos.CriarDiretorios = false;
        configuration.WebServices.TimeoutOperacao = options.Value.TimeoutOperacao;
        configuration.Arquivos.Salvar = options.Value.PersistenciaHabilitada;
        configuration.WebServices.Salvar = options.Value.PersistenciaHabilitada;
        if (options.Value.PersistenciaHabilitada)
        {
            var path = Path.Combine(Path.GetFullPath(options.Value.DiretorioDocumentos),
                CTeHttpClientRegistry.Hash(tenantId));
            configuration.Arquivos.PathSalvar = path;
            configuration.Arquivos.PathCTe = path;
            configuration.Arquivos.PathEvento = Path.Combine(path, "Eventos");
            configuration.Arquivos.PathInu = Path.Combine(path, "Inutilizacoes");
        }

        return new OpenCTe(configuration);
    }
}