using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using OpenAC.Net.CTe.Configuracao;

namespace OpenAC.Net.CTe.Services;

/// <summary>Transporte desktop com pool por certificado e TLS configurado no handler.</summary>
public sealed class DesktopCTeHttpTransport : ICTeHttpTransport
{
    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CTeConfig configuration,
        X509Certificate2 certificate, CancellationToken cancellationToken = default)
    {
        using var lease = DesktopCTeHttpClientPool.Instancia.ObterCliente(certificate,
            (SslProtocols)(int)configuration.WebServices.Protocolos);
        return await lease.Http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
    }
}
