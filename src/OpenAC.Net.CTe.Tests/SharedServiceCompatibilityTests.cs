using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.CTe.Web;
using OpenAC.Net.DFe.Core.Common;
using OpenAC.Net.DFe.Core.Http;
using OpenAC.Net.DFe.Core.Storage;

namespace OpenAC.Net.CTe.Tests;

public class SharedServiceCompatibilityTests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task SyncAndAsyncKeepSoapNamesDirectoryAndOverwritePolicy(bool sync)
    {
        var store = new CaptureStore();
        var transport = new CaptureTransport();
        var config = new CTeConfig { HttpTransport = transport, DocumentStore = store };
        config.WebServices.Salvar = true;
        config.Arquivos.PathSalvar = Path.Combine(Path.GetTempPath(), "xml-" + Guid.NewGuid().ToString("N"));
        config.Arquivos.AdicionarLiteral = config.Arquivos.SepararPorCNPJ = config.Arquivos.SepararPorAno = true;
        using var certificate = InfrastructureTests.CreateCertificate();
        using var client = new Probe(config, certificate);
        var result = sync ? client.SendSync() : await client.SendAsync();
        await Assert.That(XElement.Parse(result).Value).IsEqualTo("ok");
        await Assert.That(store.Documents.Count).IsEqualTo(2);
        foreach (var document in store.Documents)
        {
            await Assert.That(Path.GetDirectoryName(document.Caminho)).IsEqualTo(config.Arquivos.PathSalvar);
            await Assert.That(Path.GetFileName(document.Caminho)).Matches(@"^\d{17}_[a-f0-9]{32}_probe_(envio|retorno)\.xml$");
            await Assert.That(document.ArquivoExiste).IsEqualTo(DFeArquivoExiste.Sobrescrever);
            await Assert.That(document.Conteudo).StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        }
        await Assert.That(Directory.Exists(config.Arquivos.PathSalvar)).IsFalse();
        await Assert.That(transport.Options!.Certificate).IsSameReferenceAs(certificate);
        await Assert.That(transport.Options.AllowAutoRedirect).IsFalse();
        await Assert.That(transport.Options.UseCookies).IsFalse();
        await Assert.That(transport.Options.AutomaticDecompression).IsEqualTo(DecompressionMethods.None);
        await Assert.That(transport.Options.Timeout).IsEqualTo(config.WebServices.TimeoutOperacao);
        await Assert.That(transport.Calls).IsEqualTo(1);
    }

    [Test]
    public async Task FiscalDocumentsKeepTheirDirectoriesAndXmlDeclaration()
    {
        var store = new CaptureStore();
        var config = new CTeConfig { DocumentStore = store };
        var root = Path.Combine(Path.GetTempPath(), "documentos-" + Guid.NewGuid().ToString("N"));
        config.Arquivos.PathCTe = Path.Combine(root, "conhecimentos");
        config.Arquivos.PathEvento = Path.Combine(root, "movimentos");
        config.Arquivos.PathInu = Path.Combine(root, "faixas");
        config.Arquivos.Salvar = true;
        config.Arquivos.CriarDiretorios = false;
        config.Arquivos.SepararPorCNPJ = config.Arquivos.SepararPorModelo = config.Arquivos.AdicionarLiteral = true;
        config.Arquivos.SepararPorAno = config.Arquivos.SepararPorMes = config.Arquivos.SepararPorDia = true;
        using var certificate = InfrastructureTests.CreateCertificate();
        using var service = new Probe(config, certificate);
        await service.SaveFiscalDocuments();
        var dates = Path.Combine("2026", "10", "29");
        var expected = new[]
        {
            Path.Combine(root, "conhecimentos", "12345678000195", "CTe", dates, "doc.xml"),
            Path.Combine(root, "conhecimentos", "12345678000195", "CTeOS", dates, "doc-os.xml"),
            Path.Combine(root, "movimentos", "12345678000195", dates, "Evento", "110111 - Cancelamento", "evento.xml"),
            Path.Combine(root, "faixas", "12345678000195", dates, "Inu", "inutilizacao.xml")
        };
        await Assert.That(store.Documents.Select(document => document.Caminho).SequenceEqual(expected)).IsTrue();
        foreach (var document in store.Documents)
        {
            await Assert.That(document.Conteudo).IsEqualTo("<?xml version=\"1.0\" encoding=\"utf-8\"?><doc/>");
            await Assert.That(document.ArquivoExiste).IsEqualTo(DFeArquivoExiste.Sobrescrever);
        }
        await Assert.That(Directory.Exists(root)).IsFalse();
    }

    [Test]
    public async Task OperationDeadlineIncludesResponseBodyAndDisposesTheResponse()
    {
        var content = new PendingContent();
        var config = new CTeConfig { HttpTransport = new CaptureTransport { Content = content } };
        config.WebServices.TimeoutOperacao = TimeSpan.FromMilliseconds(150);
        // The CTe-specific operation deadline takes precedence over the generic connection setting.
        config.WebServices.TimeOut = TimeSpan.FromSeconds(10);
        using var certificate = InfrastructureTests.CreateCertificate();
        using var service = new Probe(config, certificate);
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.SendAsync().WaitAsync(TimeSpan.FromSeconds(5)));
        await Assert.That(content.WasDisposed).IsTrue();
    }

    [Test]
    public async Task OperationDeadlineIncludesTheDocumentStoreBeforeSending()
    {
        var store = new CaptureStore { Wait = true };
        var transport = new CaptureTransport();
        var config = new CTeConfig { HttpTransport = transport, DocumentStore = store };
        config.WebServices.TimeoutOperacao = TimeSpan.FromMilliseconds(150);
        config.WebServices.Salvar = true;
        using var certificate = InfrastructureTests.CreateCertificate();
        using var service = new Probe(config, certificate);
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.SendAsync().WaitAsync(TimeSpan.FromSeconds(5)));
        await Assert.That(transport.Calls).IsEqualTo(0);
    }

    [Test]
    public async Task HostTechnicalExceptionsKeepTheirType()
    {
        var error = new CTeTechnicalException("Falha no host", new HttpRequestException());
        var config = new CTeConfig { HttpTransport = new CaptureTransport { Error = error } };
        using var certificate = InfrastructureTests.CreateCertificate();
        using var service = new Probe(config, certificate);
        var actual = await Assert.ThrowsExactlyAsync<CTeTechnicalException>(() => service.SendAsync());
        await Assert.That(actual).IsSameReferenceAs(error);
    }

    private sealed class Probe(CTeConfig config, X509Certificate2 certificate) : CTeServiceClient(config, "https://example.test/cte", certificate)
    {
        internal Task<string> SendAsync() { NomeArquivo = "probe"; return ExecuteAsync("urn:test", "<request/>", ""); }
        internal string SendSync() { NomeArquivo = "probe"; return Execute("urn:test", "<request/>", ""); }
        protected override string TratarRetorno(XElement body) => body.Elements().Single().ToString();
        internal async Task SaveFiscalDocuments()
        {
            var date = new DateTime(2026, 10, 29);
            const string content = "<?xml version=\"1.0\"?><doc/>";
            await GravarCTeAsync(content, "doc.xml", date, "12.345.678/0001-95", ModeloCTe.CTe, default);
            await GravarCTeAsync(content, "doc-os.xml", date, "12.345.678/0001-95", ModeloCTe.CTeOS, default);
            await GravarEventoAsync(content, "evento.xml", CTeTipoEvento.Cancelamento, date, "12.345.678/0001-95", default);
            await GravarInutilizacaoAsync(content, "inutilizacao.xml", date, "12.345.678/0001-95", default);
        }
    }

    private sealed class CaptureTransport : IDFeHttpTransport
    {
        internal DFeHttpTransportOptions? Options;
        internal HttpContent? Content;
        internal Exception? Error;
        internal int Calls;
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, DFeHttpTransportOptions options, CancellationToken cancellationToken = default)
        {
            Calls++;
            Options = options;
            if (Error != null) throw Error;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = Content ?? new StringContent("<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body><result>ok</result></s:Body></s:Envelope>") });
        }
    }

    private sealed class CaptureStore : IDFeDocumentStore
    {
        internal List<IDFeDocument> Documents { get; } = [];
        internal bool Wait;
        public async Task SaveAsync(IDFeDocument document, CancellationToken cancellationToken = default)
        {
            if (Wait) await Task.Delay(Timeout.Infinite, cancellationToken);
            Documents.Add(document);
        }
    }

    private sealed class PendingContent : HttpContent
    {
        internal bool WasDisposed;
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => Task.Delay(Timeout.Infinite);
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken token) => Task.Delay(Timeout.Infinite, token);
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override void Dispose(bool disposing) { WasDisposed = true; base.Dispose(disposing); }
    }
}
