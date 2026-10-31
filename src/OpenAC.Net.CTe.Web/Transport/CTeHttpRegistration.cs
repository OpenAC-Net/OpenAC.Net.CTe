using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using OpenAC.Net.DFe.Core.Http;

namespace OpenAC.Net.CTe.Web;

internal sealed class CTeHttpRegistration(X509Certificate2 certificate, DFeHttpTransportOptions options)
{
    internal X509Certificate2 Certificate { get; } = certificate;
    internal SslProtocols Protocols => Options.Protocols;
    internal DFeHttpTransportOptions Options { get; } = options;
}
