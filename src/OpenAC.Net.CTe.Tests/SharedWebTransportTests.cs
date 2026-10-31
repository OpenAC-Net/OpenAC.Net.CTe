using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.CTe.Web;

namespace OpenAC.Net.CTe.Tests;

public class SharedWebTransportTests
{
    [Test]
    public async Task SharedServicesKeepTenantIsolationAndHostTechnicalErrors()
    {
        using var certificate = InfrastructureTests.CreateCertificate();
        var pfx = certificate.Export(X509ContentType.Pfx, "test");
        var clients = new RecordingClientFactory();
        var services = new ServiceCollection();
        services.AddSingleton<IHttpClientFactory>(clients);
        services.AddOpenCTe(config =>
        {
            config.Certificados.CertificadoBytes = pfx;
            config.Certificados.Senha = "test";
            config.WebServices.TempoVidaHandler = TimeSpan.FromMinutes(7);
            config.WebServices.TempoVidaConexaoPool = TimeSpan.FromMinutes(3);
            config.Arquivos.PathSchemas = Path.Combine(AppContext.BaseDirectory, "Schemas");
            config.WebServices.Enderecos[TipoServicoCTe.CTeStatusServico] = "https://example.test/status";
        });
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IOpenCTeClientFactory>();
        var first = await factory.CriarAsync("empresa-a");
        var second = await factory.CriarAsync("empresa-b");
        await Assert.That((await first.ConsultarSituacaoServicoAsync()).Resultado.CStat).IsEqualTo(107);
        await Assert.That((await second.ConsultarSituacaoServicoAsync()).Resultado.CStat).IsEqualTo(107);
        await Assert.That((await first.ConsultarSituacaoServicoAsync()).Resultado.CStat).IsEqualTo(107);
        await Assert.That(clients.Names[0]).IsNotEqualTo(clients.Names[1]);
        await Assert.That(clients.Names[0]).IsEqualTo(clients.Names[2]);
        var handlerOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.Extensions.Http.HttpClientFactoryOptions>>()
            .Get(clients.Names[0]);
        await Assert.That(handlerOptions.HandlerLifetime).IsEqualTo(TimeSpan.FromMinutes(7));
        var handler = provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(clients.Names[0]);
        while (handler is DelegatingHandler delegating) handler = delegating.InnerHandler!;
        await Assert.That(((SocketsHttpHandler)handler).PooledConnectionLifetime).IsEqualTo(TimeSpan.FromMinutes(3));
        clients.Fail = true;
        await Assert.ThrowsExactlyAsync<CTeTechnicalException>(() => first.ConsultarSituacaoServicoAsync());
        await Assert.That(clients.Calls).IsEqualTo(4);
    }

    private sealed class RecordingClientFactory : IHttpClientFactory
    {
        internal List<string> Names { get; } = [];
        internal bool Fail;
        internal int Calls;
        public HttpClient CreateClient(string name)
        {
            Names.Add(name);
            return new HttpClient(new Handler(this));
        }
        private sealed class Handler(RecordingClientFactory owner) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                owner.Calls++;
                if (owner.Fail) throw new HttpRequestException("Falha no transporte de teste");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent("<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body><cteStatusServicoCTResult><retConsStatServCTe xmlns=\"http://www.portalfiscal.inf.br/cte\" versao=\"4.00\"><tpAmb>2</tpAmb><cStat>107</cStat><xMotivo>Serviço em operação</xMotivo></retConsStatServCTe></cteStatusServicoCTResult></s:Body></s:Envelope>") });
            }
        }
    }
}
