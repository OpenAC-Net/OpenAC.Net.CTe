using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.DFe.Core.Common;
using OpenAC.Net.DFe.Core.Http;

namespace OpenAC.Net.CTe.Tests;

public class StatelessClientTests
{
    [Test]
    public async Task IndependentOperationsOverlapOnTheSameClientWithSeparateCertificates()
    {
        using var certificate = InfrastructureTests.CreateCertificate();
        var certificates = new CertificateProvider(certificate);
        var transport = new StatusTransport { Pending = true };
        var config = Configuration(transport);
        var client = new OpenCTe(config, certificates);
        var operations = Enumerable.Range(0, 16).Select(_ => client.ConsultarSituacaoServicoAsync()).ToArray();
        await transport.AllStarted(16).WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.That(operations.All(task => !task.IsCompleted)).IsTrue();
        transport.Release.TrySetResult(true);
        var results = await Task.WhenAll(operations);
        await Assert.That(results.All(result => result.Resultado.CStat == 107)).IsTrue();
        await Assert.That(certificates.Issued.Count).IsEqualTo(16);
        await Assert.That(certificates.Issued.Distinct(ReferenceEqualityComparer.Instance).Count()).IsEqualTo(16);
        // Todos os certificados emprestados ao transporte são liberados ao terminar a operação.
        await Assert.That(certificates.Issued.All(item => item.Handle == IntPtr.Zero)).IsTrue();
    }

    [Test]
    public async Task ConfigurationChangesAfterConstructionDoNotAffectTheClient()
    {
        using var certificate = InfrastructureTests.CreateCertificate();
        var transport = new StatusTransport();
        var config = Configuration(transport);
        config.Certificados.CertificadoBytes = certificate.Export(X509ContentType.Pfx, "test");
        config.Certificados.Senha = "test";
        config.WebServices.Enderecos[TipoServicoCTe.CTeStatusServico] = "https://empresa-a.test/status";
        config.WebServices.TimeoutOperacao = TimeSpan.FromSeconds(30);
        var client = new OpenCTe(config);
        config.WebServices.Enderecos[TipoServicoCTe.CTeStatusServico] = "https://empresa-b.test/status";
        config.WebServices.TimeoutOperacao = TimeSpan.FromTicks(1);
        config.WebServices.Ambiente = DFeTipoAmbiente.Producao;
        config.Geral.VersaoDFe = CTeVersao.v300;
        Array.Clear(config.Certificados.CertificadoBytes);
        config.Certificados.Senha = "changed";
        await client.ConsultarSituacaoServicoAsync();
        var captured = transport.Calls.Single();
        await Assert.That(captured.Url).IsEqualTo("https://empresa-a.test/status");
        await Assert.That(captured.Timeout).IsEqualTo(TimeSpan.FromSeconds(30));
        await Assert.That(captured.Body).Contains("<tpAmb>2</tpAmb>");
        await Assert.That(captured.Thumbprint).IsEqualTo(certificate.Thumbprint);
    }

    [Test]
    public async Task CancellationStopsAnAsynchronousCertificateSourceBeforeSending()
    {
        using var certificate = InfrastructureTests.CreateCertificate();
        var certificates = new CertificateProvider(certificate) { Pending = true };
        var transport = new StatusTransport();
        var client = new OpenCTe(Configuration(transport), certificates);
        using var cancellation = new CancellationTokenSource();
        var operation = client.ConsultarSituacaoServicoAsync(cancellation.Token);
        await certificates.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => operation);
        await Assert.That(transport.Calls).IsEmpty();
    }

    [Test]
    public async Task OneCancelledOperationDoesNotCancelAnotherOnTheSameClient()
    {
        using var certificate = InfrastructureTests.CreateCertificate();
        var transport = new StatusTransport { Pending = true };
        var client = new OpenCTe(Configuration(transport), new CertificateProvider(certificate));
        using var cancellation = new CancellationTokenSource();
        var cancelled = client.ConsultarSituacaoServicoAsync(cancellation.Token);
        var successful = client.ConsultarSituacaoServicoAsync();
        await transport.AllStarted(2).WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => cancelled);
        await Assert.That(successful.IsCompleted).IsFalse();
        transport.Release.SetResult(true);
        await Assert.That((await successful).Resultado.CStat).IsEqualTo(107);
    }

    [Test]
    public async Task InvalidExplicitRequestsFailBeforeLoadingACertificate()
    {
        using var certificate = InfrastructureTests.CreateCertificate();
        var certificates = new CertificateProvider(certificate);
        var transport = new StatusTransport();
        var client = new OpenCTe(Configuration(transport), certificates);
        await Assert.ThrowsAsync<ArgumentException>(() => client.ConsultarAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => client.EnviarEventoAsync(new CTeEventoSolicitacao()));
        await Assert.ThrowsAsync<ArgumentException>(() => client.ConsultarDistribuicaoAsync(new CTeDistribuicaoSolicitacao
        { UF = DFeCodUF.MS, CpfCnpj = "12345678000195", UltimoNsu = "0", Nsu = "1" }));
        await Assert.That(certificates.Issued).IsEmpty();
        await Assert.That(transport.Calls).IsEmpty();
    }

    [Test]
    public async Task PfxFileIsReadAsynchronouslyAndCanBeCancelled()
    {
        using var original = InfrastructureTests.CreateCertificate();
        var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pfx");
        await File.WriteAllBytesAsync(file, original.Export(X509ContentType.Pfx, "test"));
        try
        {
            var config = new CTeConfig();
            config.Certificados.Certificado = file;
            config.Certificados.Senha = "test";
            var source = new CTeCertificadoProvider();
            using var loaded = await source.ObterAsync(config.Certificados);
            await Assert.That(loaded.Thumbprint).IsEqualTo(original.Thumbprint);
            using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => source.ObterAsync(config.Certificados, cancellation.Token));
        }
        finally { File.Delete(file); }
    }

    [Test]
    public async Task CustomServiceCatalogsAreIndependentAndDoNotChangeTheDefaultCatalog()
    {
        using var certificate = InfrastructureTests.CreateCertificate();
        var original = CTeServiceManager.ObterEndereco(CTeVersao.v400, DFeSiglaUF.MS, TipoServicoCTe.CTeStatusServico,
            DFeTipoEmissao.Normal, DFeTipoAmbiente.Homologacao);
        var files = new List<string>();
        try
        {
            foreach (var company in new[] { "a", "b" })
            {
                using var stream = typeof(OpenCTe).Assembly.GetManifestResourceStream("OpenAC.Net.CTe.Resources.services.xml")!;
                var xml = XDocument.Load(stream);
                foreach (var endpoint in xml.Descendants().Where(element => !element.HasElements && element.Value.StartsWith("https://")))
                    endpoint.Value = "https://empresa-" + company + ".test/status";
                var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".cte"); files.Add(file);
                await File.WriteAllTextAsync(file, xml.ToString());
                var transport = new StatusTransport();
                var config = Configuration(transport);
                config.Arquivos.ArquivoServicos = file;
                await new OpenCTe(config, new CertificateProvider(certificate)).ConsultarSituacaoServicoAsync();
                await Assert.That(transport.Calls.Single().Url).IsEqualTo("https://empresa-" + company + ".test/status");
            }
            await Assert.That(CTeServiceManager.ObterEndereco(CTeVersao.v400, DFeSiglaUF.MS, TipoServicoCTe.CTeStatusServico,
                DFeTipoEmissao.Normal, DFeTipoAmbiente.Homologacao)).IsEqualTo(original);
        }
        finally { foreach (var file in files) File.Delete(file); }
    }

    [Test]
    [Arguments("last")]
    [Arguments("nsu")]
    public async Task DistributionUsesTheExplicitStateAndOneFilter(string filter)
    {
        using var certificate = InfrastructureTests.CreateCertificate();
        var transport = new StatusTransport { Distribution = true };
        var config = Configuration(transport);
        config.WebServices.Enderecos[TipoServicoCTe.DistribuicaoDFe] = "https://distribuicao.test/";
        var client = new OpenCTe(config, new CertificateProvider(certificate));
        var request = new CTeDistribuicaoSolicitacao { UF = DFeCodUF.SP, CpfCnpj = "12345678000195" };
        if (filter == "last") request.UltimoNsu = "0";
        if (filter == "nsu") request.Nsu = "123";
        var response = await client.ConsultarDistribuicaoAsync(request);
        await Assert.That(response.Resultado.CStat).IsEqualTo(137);
        var call = transport.Calls.Single();
        await Assert.That(call.Url).IsEqualTo("https://distribuicao.test/");
        await Assert.That(call.Body).Contains("<cUFAutor>35</cUFAutor>");
        var xml = XElement.Parse(call.Body);
        var query = xml.Descendants().Single(element => element.Name.LocalName == "distDFeInt");
        await Assert.That(query.Elements().Count(element => element.Name.LocalName is "distNSU" or "consNSU")).IsEqualTo(1);
    }

    private static CTeConfig Configuration(StatusTransport transport)
    {
        var config = new CTeConfig { HttpTransport = transport };
        config.Arquivos.PathSchemas = Path.Combine(AppContext.BaseDirectory, "Schemas");
        return config;
    }

    private sealed class CertificateProvider(X509Certificate2 certificate) : ICTeCertificadoProvider
    {
        private readonly byte[] pfx = certificate.Export(X509ContentType.Pfx, "test");
        internal readonly ConcurrentBag<X509Certificate2> Issued = [];
        internal readonly TaskCompletionSource<bool> Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal bool Pending;
        public async Task<X509Certificate2> ObterAsync(CTeConfigCertificados configuration, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult(true);
            if (Pending) await Task.Delay(Timeout.Infinite, cancellationToken);
            var copy = new CTeConfig(); copy.Certificados.CertificadoBytes = pfx; copy.Certificados.Senha = "test";
            var result = await new CTeCertificadoProvider().ObterAsync(copy.Certificados, cancellationToken);
            Issued.Add(result);
            return result;
        }
    }

    private sealed class StatusTransport : IDFeHttpTransport
    {
        internal ConcurrentQueue<(string Url, string Body, string Thumbprint, TimeSpan Timeout)> Calls { get; } = new();
        internal readonly TaskCompletionSource<bool> Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly SemaphoreSlim starts = new(0);
        internal bool Pending, Distribution;
        internal async Task AllStarted(int count) { for (var i = 0; i < count; i++) await starts.WaitAsync(); }
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, DFeHttpTransportOptions options, CancellationToken cancellationToken = default)
        {
            Calls.Enqueue((request.RequestUri!.ToString(), await request.Content!.ReadAsStringAsync(cancellationToken), options.Certificate!.Thumbprint, options.Timeout));
            starts.Release();
            if (Pending) await Release.Task.WaitAsync(cancellationToken);
            if (Distribution) return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body><retDistDFeInt xmlns=\"http://www.portalfiscal.inf.br/cte\" versao=\"1.00\"><tpAmb>2</tpAmb><cStat>137</cStat><xMotivo>Nenhum documento localizado</xMotivo></retDistDFeInt></s:Body></s:Envelope>") };
            return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body><cteStatusServicoCTResult><retConsStatServCTe xmlns=\"http://www.portalfiscal.inf.br/cte\" versao=\"4.00\"><tpAmb>2</tpAmb><cStat>107</cStat><xMotivo>Serviço em operação</xMotivo></retConsStatServCTe></cteStatusServicoCTResult></s:Body></s:Envelope>") };
        }
    }
}
