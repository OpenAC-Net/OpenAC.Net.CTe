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

    internal string Register(string tenant, OpenAC.Net.DFe.Core.Http.DFeHttpTransportOptions options, Uri endpoint)
    {
        var name = "OpenAC.CTe:" + Hash(tenant + "|" + options.GetPoolKey(endpoint));
        lock (gate)
        {
            if (!registrations.ContainsKey(name))
                registrations[name] = new CTeHttpRegistration(new X509Certificate2(options.Certificate!), options);
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