using Microsoft.Extensions.Http.Resilience;
using Polly;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace OpenAC.Net.CTe.Web;

internal sealed class CTeHttpClientOptions(CTeHttpClientRegistry registry)
    : IConfigureNamedOptions<HttpClientFactoryOptions>
{
    public void Configure(HttpClientFactoryOptions value)
    {
    }

    public void Configure(string? name, HttpClientFactoryOptions value)
    {
        if (name == null || !registry.TryGet(name, out var registration)) return;
        value.HandlerLifetime = registration.Options.TempoVidaHandler;
        value.HttpClientActions.Add(client => client.Timeout = Timeout.InfiniteTimeSpan);
        value.HttpMessageHandlerBuilderActions.Add(builder =>
        {
            builder.PrimaryHandler = new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.All,
                PooledConnectionLifetime = registration.Options.TempoVidaConexaoPool,
                SslOptions = new SslClientAuthenticationOptions
                {
                    EnabledSslProtocols = registration.Protocols,
                    ClientCertificates = new X509CertificateCollection { registration.Certificate }
                }
            };
            builder.AdditionalHandlers.Add(new ResilienceHandler(
                OpenAC.Net.DFe.Core.Http.DFeHttpResilience.CreatePipeline(registration.Options)));
        });
    }
}