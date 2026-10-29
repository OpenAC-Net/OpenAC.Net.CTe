using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using OpenAC.Net.CTe.Configuracao;

namespace OpenAC.Net.CTe;

/// <summary>Fornece um certificado por operação. O cliente descarta o certificado retornado após o uso.</summary>
public interface ICTeCertificadoProvider
{
    Task<X509Certificate2> ObterAsync(CTeConfigCertificados configuracao, CancellationToken cancellationToken = default);
}
