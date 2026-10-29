using OpenAC.Net.DFe.Core.Storage;
using OpenAC.Net.DFe.Core.Http;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.CTe.Eventos;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.DFe.Core.Common;

namespace OpenAC.Net.CTe.Tests;

public class ServiceVersionTests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ConsultationAndStatusUseV4ContractWithoutLegacyHeader(bool status)
    {
        var transport = new CaptureTransport(status ? "cteStatusServicoCTResult" : "cteConsultaCTResult", status ? "retConsStatServCTe" : "retConsSitCTe");
        var config = Config(transport);
        using var certificate = InfrastructureTests.CreateCertificate();
        if (status)
        {
            using var service = new CTeStatusServicoServiceClient(config, certificate);
            var result = await service.StatusServicoAsync();
            await Assert.That(result.Resultado.CStat).IsEqualTo(107);
            await Assert.That(transport.ContentType).Contains("CTeStatusServicoV4");
        }
        else
        {
            using var service = new CTeConsultaServiceClient(config, certificate);
            var result = await service.ConsultaAsync(PdfTests.Key);
            await Assert.That(result.Resultado.CStat).IsEqualTo(107);
            await Assert.That(transport.ContentType).Contains("CTeConsultaV4");
        }
        await Assert.That(transport.Envelope).DoesNotContain("cteCabecMsg");
    }

    [Test]
    public async Task CancellationEventUsesV4ContractAndPersistsSignedRequestWithProtocol()
    {
        var transport = new CaptureTransport("cteRecepcaoEventoResult", "retEventoCTe");
        var store = new EventStore();
        var config = Config(transport);
        config.DocumentStore = store;
        config.Arquivos.Salvar = true;
        config.Arquivos.CriarDiretorios = false;
        using var certificate = InfrastructureTests.CreateCertificate();
        using var service = new CTeRecepcaoEventoServiceClient(config, TipoServicoCTe.RecepcaoEvento, certificate);
        await service.RecepcaoEventoAsync(1, PdfTests.Key, "12345678000195", new CTeEvCancCTe { DescEvento = "Cancelamento", NProt = "135260000000123", XJust = "Documento emitido com dados incorretos." });
        await Assert.That(transport.ContentType).Contains("CTeRecepcaoEventoV4");
        var document = XElement.Parse(store.Processed!);
        await Assert.That(document.Name.LocalName).IsEqualTo("procEventoCTe");
        await Assert.That(document.Elements().Any(element => element.Name.LocalName == "eventoCTe")).IsTrue();
        await Assert.That(document.Elements().Any(element => element.Name.LocalName == "retEventoCTe")).IsTrue();
    }

    private static CTeConfig Config(IDFeHttpTransport transport)
    {
        var config = new CTeConfig { HttpTransport = transport };
        config.Geral.VersaoDFe = CTeVersao.v400;
        config.Geral.ExibirErroSchema = true;
        config.Arquivos.PathSchemas = Path.Combine(AppContext.BaseDirectory, "Schemas");
        config.WebServices.Ambiente = DFeTipoAmbiente.Homologacao;
        return config;
    }
    private sealed class CaptureTransport(string wrapper, string root) : IDFeHttpTransport
    {
        internal string Envelope = "", ContentType = "";
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, DFeHttpTransportOptions options, CancellationToken cancellationToken = default)
        {
            Envelope = await request.Content!.ReadAsStringAsync(cancellationToken);
            ContentType = request.Content.Headers.ContentType!.ToString();
            XNamespace ns = "http://www.portalfiscal.inf.br/cte";
            XNamespace soap = "http://www.w3.org/2003/05/soap-envelope";
            var response = new XElement(ns + root, new XAttribute("versao", "4.00"), new XElement(ns + "tpAmb", 2), new XElement(ns + "cStat", 107), new XElement(ns + "xMotivo", "Serviço em operação"));
            var envelope = new XElement(soap + "Envelope", new XElement(soap + "Body", new XElement(wrapper, response)));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(envelope.ToString()) };
        }
    }
    private sealed class EventStore : IDFeDocumentStore
    {
        public string? Processed;
        public Task SaveAsync(IDFeDocument document, CancellationToken cancellationToken = default)
        { if (document.Conteudo.Contains("<procEventoCTe")) Processed = document.Conteudo; return Task.CompletedTask; }
    }
}
