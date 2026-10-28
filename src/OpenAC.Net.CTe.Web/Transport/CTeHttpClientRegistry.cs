using System.Collections.Concurrent;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OpenAC.Net.CTe.Web;

internal sealed class CTeHttpClientRegistry : IDisposable
{
    private readonly ConcurrentDictionary<string, CTeHttpRegistration> registrations = new();
    private readonly object gate = new();

    internal static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));

    internal string Register(string tenant, X509Certificate2 certificate, SslProtocols protocols, Uri endpoint)
    {
        var name = "OpenAC.CTe:" +
                   Hash(
                       $"{tenant}|{certificate.Thumbprint}|{(int)protocols}|{endpoint.GetLeftPart(UriPartial.Authority)}");
        lock (gate)
        {
            if (!registrations.ContainsKey(name))
                registrations[name] = new CTeHttpRegistration(new X509Certificate2(certificate), protocols);
        }

        return name;
    }

    internal bool TryGet(string name, out CTeHttpRegistration registration) =>
        registrations.TryGetValue(name, out registration!);

    public void Dispose()
    {
        foreach (var registration in registrations.Values) registration.Certificate.Dispose();
        registrations.Clear();
    }
}