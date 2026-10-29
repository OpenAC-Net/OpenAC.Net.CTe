using OpenAC.Net.DFe.Core.Storage;
using OpenAC.Net.DFe.Core.Http;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.CTe.Storage;
using OpenAC.Net.DFe.Core.Common;

namespace OpenAC.Net.CTe.Tests;

public class AuthorizationTests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task AuthorizesOneDocumentInCompressedSoapAndPersistsItsProtocol(bool os)
    {
        using var certificate = InfrastructureTests.CreateCertificate();
        var transport = new AuthorizationTransport();
        var store = new MemoryStore();
        var config = new CTeConfig { HttpTransport = transport, DocumentStore = store };
        config.Geral.VersaoDFe = CTeVersao.v400;
        config.Geral.ValidarDigest = true;
        config.Geral.ExibirErroSchema = true;
        config.Certificados.CertificadoBytes = certificate.Export(X509ContentType.Pfx, "test");
        config.Certificados.Senha = "test";
        config.Arquivos.PathSchemas = Path.Combine(AppContext.BaseDirectory, "Schemas");
        config.Arquivos.Salvar = true;
        config.Arquivos.CriarDiretorios = false;
        var client = new OpenCTe(config);
        var cte = VersionTests.Sample(CTeVersao.v400);
        var cteOS = VersionTests.SampleOS();
        var before = os ? cteOS.GetXml() : cte.GetXml();
        var response = os ? await client.AutorizarAsync(cteOS) : await client.AutorizarAsync(cte);
        await Assert.That(os ? cteOS.GetXml() : cte.GetXml()).IsEqualTo(before);
        await Assert.That(os ? cteOS.Assinado : cte.Assinado).IsFalse();
        var originalInfo = os ? cteOS.InfCTe : cte.InfCTe;
        var copiedInfo = os ? response.DocumentoProcessado.CTeOS.InfCTe : response.DocumentoProcessado.CTe.InfCTe;
        await Assert.That(copiedInfo).IsNotSameReferenceAs(originalInfo);
        await Assert.That(copiedInfo.Ide.Parent).IsSameReferenceAs(copiedInfo);
        await Assert.That(copiedInfo.Rem.Parent).IsSameReferenceAs(copiedInfo);
        await Assert.That(response.Codigo).IsEqualTo(100);
        await Assert.That(response.Protocolo).IsEqualTo("135260000000123");
        await Assert.That(response.Autorizado).IsTrue();
        await Assert.That(response.DocumentoProcessado).IsNotNull();
        await Assert.That(transport.Document!.Name.LocalName).IsEqualTo(os ? "CTeOS" : "CTe");
        await Assert.That(transport.Action).Contains(os ? "CTeRecepcaoOSV4" : "CTeRecepcaoSincV4");
        await Assert.That(transport.Envelope).DoesNotContain("cteCabecMsg");
        await Assert.That(transport.Calls).IsEqualTo(1);
        var processed = store.Documents.Single(value => value.Contains("<cteProc") || value.Contains("<cteOSProc"));
        await Assert.That(XElement.Parse(processed).Name.LocalName).IsEqualTo(os ? "cteOSProc" : "cteProc");
    }

    [Test]
    public async Task FiscalRejectionDoesNotProduceAnAuthorizedDocument()
    {
        using var certificate = InfrastructureTests.CreateCertificate();
        var config = new CTeConfig { HttpTransport = new AuthorizationTransport { Rejected = true } };
        config.Geral.VersaoDFe = CTeVersao.v400;
        config.Certificados.CertificadoBytes = certificate.Export(X509ContentType.Pfx, "test");
        config.Certificados.Senha = "test";
        config.Arquivos.PathSchemas = Path.Combine(AppContext.BaseDirectory, "Schemas");
        var client = new OpenCTe(config);
        var response = await client.AutorizarAsync(VersionTests.Sample(CTeVersao.v400));
        await Assert.That(response.Autorizado).IsFalse();
        await Assert.That(response.DocumentoProcessado).IsNull();
        await Assert.That(response.Resultado.CStat).IsEqualTo(539);
    }

    private sealed class MemoryStore : IDFeDocumentStore
    {
        internal readonly List<string> Documents = [];
        public Task SaveAsync(IDFeDocument document, CancellationToken cancellationToken = default)
        { Documents.Add(document.Conteudo); return Task.CompletedTask; }
    }
    private sealed class AuthorizationTransport : IDFeHttpTransport
    {
        internal XElement? Document;
        internal bool Rejected;
        internal int Calls;
        internal string Action = "", Envelope = "";
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, DFeHttpTransportOptions options, CancellationToken cancellationToken = default)
        {
            Calls++;
            Envelope = await request.Content!.ReadAsStringAsync(cancellationToken);
            Action = request.Content.Headers.ContentType!.ToString();
            var payload = XElement.Parse(Envelope).Descendants().Single(element => element.Name.LocalName == "cteDadosMsg");
            using var compressed = new MemoryStream(Convert.FromBase64String(payload.Value));
            using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip);
            Document = XElement.Parse(await reader.ReadToEndAsync(cancellationToken));
            XNamespace ns = "http://www.portalfiscal.inf.br/cte";
            XNamespace ds = "http://www.w3.org/2000/09/xmldsig#";
            var key = Document.Element(ns + "infCte")!.Attribute("Id")!.Value.Substring(3);
            var os = Document.Name.LocalName == "CTeOS";
            var result = new XElement(ns + (os ? "retCTeOS" : "retCTe"), new XAttribute("versao", "4.00"),
                new XElement(ns + "tpAmb", 2), new XElement(ns + "cUF", 35), new XElement(ns + "verAplic", "TEST"),
                new XElement(ns + "cStat", Rejected ? 539 : 100), new XElement(ns + "xMotivo", Rejected ? "Rejeitado" : "Autorizado"));
            if (!Rejected) result.Add(new XElement(ns + "protCTe", new XAttribute("versao", "4.00"), new XElement(ns + "infProt",
                new XElement(ns + "tpAmb", 2), new XElement(ns + "verAplic", "TEST"), new XElement(ns + "chCTe", key),
                new XElement(ns + "dhRecbto", "2026-10-27T10:00:00-04:00"), new XElement(ns + "nProt", "135260000000123"),
                new XElement(ns + "digVal", Document.Descendants(ds + "DigestValue").Single().Value),
                new XElement(ns + "cStat", 100), new XElement(ns + "xMotivo", "Autorizado"))));
            XNamespace soap = "http://www.w3.org/2003/05/soap-envelope";
            var response = new XElement(soap + "Envelope", new XElement(soap + "Body",
                new XElement(payload.Name.Namespace + (os ? "cteRecepcaoOSResult" : "cteRecepcaoResult"), result)));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response.ToString(SaveOptions.DisableFormatting)) };
        }
    }
}
