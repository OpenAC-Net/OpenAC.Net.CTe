using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace OpenAC.Net.CTe.Web;

internal sealed record CTeHttpRegistration(X509Certificate2 Certificate, SslProtocols Protocols);