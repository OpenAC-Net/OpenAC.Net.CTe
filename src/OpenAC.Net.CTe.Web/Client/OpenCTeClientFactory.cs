using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.DFe.Core.Storage;

namespace OpenAC.Net.CTe.Web;

internal sealed class OpenCTeClientFactory(
    ICTeConfigurationProvider<CTeConfig> configurations,
    ICTeCertificateProvider certificates,
    ICTeCertificadoProvider certificateReader,
    IHttpClientFactory clients,
    CTeHttpClientRegistry registry,
    IOptions<CTeWebOptions> options,
    ILogger<CTeWebHttpTransport> logger,
    IDFeDocumentStore store) : IOpenCTeClientFactory
{
    public async ValueTask<IOpenCTeClient> CriarAsync(string tenantId = "default",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Empresa não informada.", nameof(tenantId));
        cancellationToken.ThrowIfCancellationRequested();
        var configuration =
            await configurations.GetConfigurationAsync(tenantId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("O provider retornou uma configuração nula.");
        
        configuration = configuration.Copiar();
        await certificates.ConfigureAsync(tenantId, configuration.Certificados, cancellationToken)
            .ConfigureAwait(false);
        
        using (var certificate = await certificateReader.ObterAsync(configuration.Certificados, cancellationToken)
                   .ConfigureAwait(false))
            CTeWebHttpTransport.ValidateCertificate(certificate, options.Value.MargemValidadeCertificado);
        
        configuration.HttpTransport = new CTeWebHttpTransport(tenantId, clients, registry, options, logger);
        configuration.DocumentStore = store;
        configuration.Arquivos.CriarDiretorios = false;
        configuration.WebServices.TimeoutOperacao = options.Value.TimeoutOperacao;
        configuration.Arquivos.Salvar = options.Value.PersistenciaHabilitada;
        configuration.WebServices.Salvar = options.Value.PersistenciaHabilitada;
        if (!options.Value.PersistenciaHabilitada) 
            return new OpenCTe(configuration, certificateReader);
        
        var path = Path.Combine(Path.GetFullPath(options.Value.DiretorioDocumentos),
            CTeHttpClientRegistry.Hash(tenantId));
        
        configuration.Arquivos.PathSalvar = path;
        configuration.Arquivos.PathCTe = path;
        configuration.Arquivos.PathEvento = Path.Combine(path, "Eventos");
        configuration.Arquivos.PathInu = Path.Combine(path, "Inutilizacoes");

        return new OpenCTe(configuration, certificateReader);
    }
}