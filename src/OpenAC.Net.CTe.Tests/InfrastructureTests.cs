using OpenAC.Net.DFe.Core.Storage;
using OpenAC.Net.DFe.Core.Http;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.CTe.Storage;
using OpenAC.Net.CTe.Web;

namespace OpenAC.Net.CTe.Tests;

public class InfrastructureTests
{
    [Test]
    public async Task SoapUsesInjectedTransportAndStoreWithoutChangingGlobalTls()
    {
        var tls = ServicePointManager.SecurityProtocol;
        var transport = new FakeTransport();
        var store = new RecordingStore();
        var config = new CTeConfig { HttpTransport = transport, DocumentStore = store };
        config.WebServices.Salvar = true;
        using var certificate = CreateCertificate();
        using var service = new ProbeService(config, certificate);
        var result = await service.SendAsync(CancellationToken.None);
        await Assert.That(XElement.Parse(result).Value).IsEqualTo("ok");
        await Assert.That(ServicePointManager.SecurityProtocol).IsEqualTo(tls);
        await Assert.That(transport.Certificate).IsSameReferenceAs(certificate);
        await Assert.That(store.Documents.Count).IsEqualTo(2);
        await Assert.That(transport.ContentType).Contains("application/soap+xml");
        await Assert.That(transport.Envelope).Contains("<soapenv:Header>");
    }

    [Test]
    public async Task CancellationReachesTransport()
    {
        var transport = new FakeTransport { Wait = true };
        var config = new CTeConfig { HttpTransport = transport };
        using var certificate = CreateCertificate();
        using var service = new ProbeService(config, certificate);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.SendAsync(cancellation.Token));
        await Assert.That(transport.Token.CanBeCanceled).IsTrue();
    }

    [Test]
    public async Task SoapFaultIsReported()
    {
        var config = new CTeConfig { HttpTransport = new FakeTransport { Fault = true } };
        using var certificate = CreateCertificate();
        using var service = new ProbeService(config, certificate);
        var exception = await Assert.ThrowsExactlyAsync<OpenAC.Net.DFe.Core.OpenDFeCommunicationException>(() => service.SendAsync(default));
        await Assert.That(exception!.Message).Contains("Falha de teste");
    }

    [Test]
    public async Task WebFactoryCreatesIndependentConfigurationsAndTenantDirectories()
    {
        using var certificate = CreateCertificate();
        var bytes = certificate.Export(X509ContentType.Pfx, "test");
        var services = new ServiceCollection();
        services.AddOpenCTeWeb(config =>
        {
            config.Certificados.CertificadoBytes = bytes;
            config.Certificados.Senha = "test";
        }, options => { options.PersistenciaHabilitada = true; options.DiretorioDocumentos = Path.GetTempPath(); });
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IOpenCTeClientFactory>();
        var first = await factory.CriarAsync("../../empresa-a");
        var second = await factory.CriarAsync("empresa-b");
        await Assert.That(second).IsNotSameReferenceAs(first);
        await Assert.That(first is IOpenCTeClient).IsTrue();
    }

    [Test]
    public async Task DocumentStoreWritesCompleteDocumentsUnderConcurrentReplacement()
    {
        var path = Path.Combine(Path.GetTempPath(), "openac-cte-tests-" + Guid.NewGuid().ToString("N"), "doc.xml");
        try
        {
            var store = new LocalDocumentStore();
            var payloads = Enumerable.Range(0, 12).Select(i => $"<doc>{new string((char)('a' + i), 8192)}</doc>").ToArray();
            await Task.WhenAll(payloads.Select(value => store.SaveAsync(new DFeDocument(path, value, DFeArquivoExiste.Sobrescrever))));
            await Assert.That(payloads).Contains(await File.ReadAllTextAsync(path));
            await Assert.That(Directory.GetFiles(Path.GetDirectoryName(path)!)).Count().IsEqualTo(1);
        }
        finally { Directory.Delete(Path.GetDirectoryName(path)!, true); }
    }

    internal static X509Certificate2 CreateCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=CTe Tests", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    private sealed class RecordingStore : IDFeDocumentStore
    {
        internal readonly List<string> Documents = [];
        public Task SaveAsync(IDFeDocument document, CancellationToken cancellationToken = default)
        { Documents.Add(document.Conteudo); return Task.CompletedTask; }
    }
    private sealed class ProbeService(CTeConfig config, X509Certificate2 certificate) : CTeServiceClient(config, "https://example.test/cte", certificate)
    {
        internal Task<string> SendAsync(CancellationToken token) => ExecuteAsync("urn:test", "<request/>", "", token);
        protected override string TratarRetorno(XElement element) => element.Elements().First().ToString();
    }
    private sealed class FakeTransport : IDFeHttpTransport
    {
        public bool Wait, Fault;
        public X509Certificate2? Certificate;
        public CancellationToken Token;
        public string Envelope = "", ContentType = "";
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, DFeHttpTransportOptions options, CancellationToken cancellationToken = default)
        {
            Certificate = options.Certificate;
            Token = cancellationToken;
            Envelope = await request.Content!.ReadAsStringAsync(cancellationToken);
            ContentType = request.Content.Headers.ContentType!.ToString();
            if (Wait) await Task.Delay(Timeout.Infinite, cancellationToken);
            var body = Fault ? "<s:Fault><s:Reason><s:Text>Falha de teste</s:Text></s:Reason></s:Fault>" : "<result>ok</result>";
            return new HttpResponseMessage(Fault ? HttpStatusCode.InternalServerError : HttpStatusCode.OK)
            { Content = new StringContent($"<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body>{body}</s:Body></s:Envelope>") };
        }
    }
}
