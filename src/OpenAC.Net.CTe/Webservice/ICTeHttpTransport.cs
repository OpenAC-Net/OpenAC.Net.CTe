using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using OpenAC.Net.CTe.Configuracao;

namespace OpenAC.Net.CTe.Services;

/// <summary>Transporte substituível. A resposta pertence ao chamador; o certificado permanece com a operação.</summary>
public interface ICTeHttpTransport
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CTeConfig configuration,
        X509Certificate2 certificate, CancellationToken cancellationToken = default);
}
