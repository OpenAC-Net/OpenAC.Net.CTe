using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAC.Net.DFe.Core.Http;

namespace OpenAC.Net.CTe.Web;

internal sealed class CTeWebHttpTransport(
    string tenantId,
    IHttpClientFactory clients,
    CTeHttpClientRegistry registry,
    ILogger<CTeWebHttpTransport> logger) : IDFeHttpTransport
{
    private static readonly ActivitySource Activities = new("OpenAC.Net.CTe.Web");
    private static readonly Meter Meter = new("OpenAC.Net.CTe.Web");
    private static readonly Counter<long> Count = Meter.CreateCounter<long>("openac.cte.requests");

    private static readonly Histogram<double> Duration =
        Meter.CreateHistogram<double>("openac.cte.request.duration", "ms");

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        DFeHttpTransportOptions transportOptions,
        CancellationToken cancellationToken = default)
    {
        var endpoint = request.RequestUri ?? throw new ArgumentException("Endpoint não informado.");
        if (!endpoint.IsAbsoluteUri || endpoint.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("O endpoint fiscal deve utilizar HTTPS.");
        
        var certificate = transportOptions.Certificate
                          ?? throw new InvalidOperationException("Certificado com chave privada não informado.");
        
        OpenAC.Net.DFe.Core.Common.DFeCertificadosConfigBase.ValidarCertificado(certificate, transportOptions.MargemValidadeCertificado);
        
        var name = registry.Register(tenantId, transportOptions, endpoint);
        using var activity = Activities.StartActivity("CTe.SOAP", ActivityKind.Client);
        activity?.SetTag("server.address", endpoint.Host);
        activity?.SetTag("openac.tenant", CTeHttpClientRegistry.Hash(tenantId));
        
        var watch = Stopwatch.StartNew();
        
        try
        {
            using var client = clients.CreateClient(name);
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken)
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
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException
                                              or BrokenCircuitException or TimeoutRejectedException)
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

}
