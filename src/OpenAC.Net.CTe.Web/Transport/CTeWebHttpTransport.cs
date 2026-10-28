using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.CTe.Services;

namespace OpenAC.Net.CTe.Web;

internal sealed class CTeWebHttpTransport(
    string tenantId,
    IHttpClientFactory clients,
    CTeHttpClientRegistry registry,
    IOptions<CTeWebOptions> options,
    ILogger<CTeWebHttpTransport> logger) : ICTeHttpTransport
{
    private static readonly ActivitySource Activities = new("OpenAC.Net.CTe.Web");
    private static readonly Meter Meter = new("OpenAC.Net.CTe.Web");
    private static readonly Counter<long> Count = Meter.CreateCounter<long>("openac.cte.requests");

    private static readonly Histogram<double> Duration =
        Meter.CreateHistogram<double>("openac.cte.request.duration", "ms");

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CTeConfig configuration,
        X509Certificate2 certificate, CancellationToken cancellationToken = default)
    {
        var endpoint = request.RequestUri ?? throw new ArgumentException("Endpoint não informado.");
        if (!endpoint.IsAbsoluteUri || endpoint.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("O endpoint fiscal deve utilizar HTTPS.");
        ValidateCertificate(certificate, options.Value.MargemValidadeCertificado);
        var name = registry.Register(tenantId, certificate, (SslProtocols)(int)configuration.WebServices.Protocolos,
            endpoint);
        using var activity = Activities.StartActivity("CTe.SOAP", ActivityKind.Client);
        activity?.SetTag("server.address", endpoint.Host);
        activity?.SetTag("openac.tenant", CTeHttpClientRegistry.Hash(tenantId));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Value.TimeoutOperacao);
        var watch = Stopwatch.StartNew();
        try
        {
            using var client = clients.CreateClient(name);
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeout.Token)
                .ConfigureAwait(false);
            activity?.SetTag("http.response.status_code", (int)response.StatusCode);
            Count.Add(1,
                new KeyValuePair<string, object?>("outcome", response.IsSuccessStatusCode ? "success" : "http_error"));
            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or BrokenCircuitException or TimeoutRejectedException)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            Count.Add(1, new KeyValuePair<string, object?>("outcome", "transport_error"));
            logger.LogError(exception, "Falha SOAP CT-e para {Endpoint}, empresa {Tenant}", endpoint.Authority,
                CTeHttpClientRegistry.Hash(tenantId));
            throw new CTeTechnicalException("Falha no transporte SOAP do CT-e.", exception);
        }
        finally
        {
            Duration.Record(watch.Elapsed.TotalMilliseconds);
        }
    }

    internal static void ValidateCertificate(X509Certificate2 certificate, TimeSpan margin)
    {
        if (certificate is not { HasPrivateKey: true })
            throw new InvalidOperationException("Certificado com chave privada não informado.");
        var now = DateTime.UtcNow;
        if (certificate.NotBefore.ToUniversalTime() > now || certificate.NotAfter.ToUniversalTime() <= now + margin)
            throw new InvalidOperationException("Certificado fora do período de validade ou próximo do vencimento.");
    }
}