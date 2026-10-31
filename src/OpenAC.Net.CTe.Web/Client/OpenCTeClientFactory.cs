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
        _ = new OpenAC.Net.DFe.Core.Http.DFeHttpTransportOptions(configuration.WebServices);
        await certificates.ConfigureAsync(tenantId, configuration.Certificados, cancellationToken)
            .ConfigureAwait(false);
        
        using (var certificate = await certificateReader.ObterAsync(configuration.Certificados, cancellationToken)
                   .ConfigureAwait(false))
            configuration.Certificados.ValidarCertificado(certificate);
        
        configuration.HttpTransport = new CTeWebHttpTransport(tenantId, clients, registry, logger);
        configuration.DocumentStore = store;
        configuration.Arquivos.CriarDiretorios = false;
        if (configuration.Arquivos.Salvar || configuration.WebServices.Salvar)
        {
            var tenant = CTeHttpClientRegistry.Hash(tenantId);
            configuration.Arquivos.PathSalvar = Path.Combine(Path.GetFullPath(configuration.Arquivos.PathSalvar), tenant);
            if (!string.IsNullOrWhiteSpace(configuration.Arquivos.PathCTe))
                configuration.Arquivos.PathCTe = Path.Combine(Path.GetFullPath(configuration.Arquivos.PathCTe), tenant);
            if (!string.IsNullOrWhiteSpace(configuration.Arquivos.PathEvento))
                configuration.Arquivos.PathEvento = Path.Combine(Path.GetFullPath(configuration.Arquivos.PathEvento), tenant);
            if (!string.IsNullOrWhiteSpace(configuration.Arquivos.PathInu))
                configuration.Arquivos.PathInu = Path.Combine(Path.GetFullPath(configuration.Arquivos.PathInu), tenant);
        }

        return new OpenCTe(configuration, certificateReader);
    }
}