using OpenAC.Net.DFe.Core.Http;
using System.Xml.Linq;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.CTe.DACTe.PDFSharp;
using OpenAC.Net.CTe.Eventos;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.DFe.Core.Common;
using PdfSharp.Pdf.IO;

namespace OpenAC.Net.CTe.Tests;

public class LegacyVersionTests
{
    [Test]
    public async Task NewDocumentsUseVersion400ByDefault()
    {
        await Assert.That(new CTeConfig().Geral.VersaoDFe).IsEqualTo(CTeVersao.v400);
        await Assert.That(new CTe().InfCTe.Versao).IsEqualTo(CTeVersao.v400);
        await Assert.That(new CTeOS().Versao).IsEqualTo(CTeVersao.v400);
        await Assert.That(new CTeProc().Versao).IsEqualTo(CTeVersao.v400);
    }

    [Test]
    [Arguments(CTeVersao.v200)]
    [Arguments(CTeVersao.v300)]
    public async Task LegacyOperationsStopBeforeLoadingCertificateOrUsingTransport(CTeVersao version)
    {
        var transport = new RejectTransport();
        var config = new CTeConfig { HttpTransport = transport };
        config.Geral.VersaoDFe = version;
        var client = new OpenCTe(config);
        Func<Task>[] operations =
        [
            () => client.AutorizarAsync(new CTe()),
            () => client.AutorizarAsync(new CTeOS()),
            () => client.EnviarEventoAsync(new CTeEventoSolicitacao { Chave = PdfTests.Key, CnpjAutor = "12345678000195", Evento = new CTeEvCancCTe() }),
            () => client.ConsultarAsync(PdfTests.Key),
            () => client.ConsultarSituacaoServicoAsync(),
            () => client.ConsultaCadastroAsync(DFeCodUF.MS, "12345678000195"),
            () => client.ConsultarDistribuicaoAsync(new CTeDistribuicaoSolicitacao { UF = DFeCodUF.MS, CpfCnpj = "12345678000195", UltimoNsu = "0" })
        ];
        foreach (var operation in operations)
        {
            var error = await Assert.ThrowsExactlyAsync<NotSupportedException>(operation);
            await Assert.That(error!.Message).Contains("apenas para leitura, geração e impressão");
        }
        await Assert.That(transport.Calls).IsEqualTo(0);
    }

    [Test]
    [Arguments(CTeVersao.v200)]
    [Arguments(CTeVersao.v300)]
    public async Task Version400ConfigurationDoesNotUpgradeOrSendLegacyDocuments(CTeVersao version)
    {
        var transport = new RejectTransport();
        var client = new OpenCTe(new CTeConfig { HttpTransport = transport });
        var cte = VersionTests.Sample(version);
        var os = VersionTests.SampleOS();
        os.Versao = os.InfCTe.Versao = version;
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => client.AutorizarAsync(cte));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => client.AutorizarAsync(os));
        await Assert.That(cte.InfCTe.Versao).IsEqualTo(version);
        await Assert.That(os.InfCTe.Versao).IsEqualTo(version);
        await Assert.That(os.Versao).IsEqualTo(version);
        await Assert.That(transport.Calls).IsEqualTo(0);
    }

    [Test]
    [Arguments(CTeVersao.v200)]
    [Arguments(CTeVersao.v300)]
    public void CustomAddressesCannotEnableLegacyServices(CTeVersao version)
    {
        var config = new CTeConfig();
        config.Geral.VersaoDFe = version;
        foreach (var service in Enum.GetValues<TipoServicoCTe>())
        {
            config.WebServices.Enderecos[service] = "https://example.test/cte";
            Assert.ThrowsExactly<NotSupportedException>(() => CTeServiceManager.ObterEndereco(
                version, DFeSiglaUF.MS, service, DFeTipoEmissao.Normal, DFeTipoAmbiente.Homologacao));
        }
        Assert.ThrowsExactly<NotSupportedException>(() => new CTeAutorizacaoServiceClient(config));
        Assert.ThrowsExactly<NotSupportedException>(() => new CTeAutorizacaoServiceClient(config, true));
        Assert.ThrowsExactly<NotSupportedException>(() => new CTeRecepcaoEventoServiceClient(config, TipoServicoCTe.RecepcaoEvento));
    }

    [Test]
    [Arguments("2.00", false)]
    [Arguments("3.00", false)]
    [Arguments("3.00", true)]
    public async Task DirectAuthorizationRejectsLegacyXml(string version, bool os)
    {
        var transport = new RejectTransport();
        var config = new CTeConfig { HttpTransport = transport };
        using var service = new CTeAutorizacaoServiceClient(config, os);
        var xml = $"<{(os ? "CTeOS" : "CTe")} xmlns=\"http://www.portalfiscal.inf.br/cte\" versao=\"{version}\"><infCte versao=\"{version}\"/></{(os ? "CTeOS" : "CTe")}>";
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => service.AutorizarAsync(xml));
        await Assert.That(transport.Calls).IsEqualTo(0);
    }

    [Test]
    public async Task ChangingConfigurationAfterServiceCreationCannotEnableLegacySoap()
    {
        var transport = new RejectTransport();
        var config = new CTeConfig { HttpTransport = transport };
        using var service = new ProbeService(config);
        config.Geral.VersaoDFe = CTeVersao.v300;
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => service.SendAsync());
        await Assert.That(transport.Calls).IsEqualTo(0);
    }

    [Test]
    [Arguments(CTeVersao.v200)]
    [Arguments(CTeVersao.v300)]
    public async Task LegacyDocumentsStillGenerateLoadAndPrint(CTeVersao version)
    {
        var sample = PdfTests.Sample();
        sample.Versao = sample.CTe.InfCTe.Versao = version;
        var xml = sample.GetXml();
        var loaded = CTeProc.Load(xml);
        await Assert.That(loaded.Versao).IsEqualTo(version);
        await Assert.That(loaded.CTe.InfCTe.Versao).IsEqualTo(version);
        var bytes = new OpenDACTe().GerarPDF(loaded);
        using var stream = new MemoryStream(bytes);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        await Assert.That(pdf.PageCount > 0).IsTrue();
    }

    [Test]
    public async Task LegacyOSDocumentsStillGenerateLoadAndPrint()
    {
        var sample = VersionTests.SampleOS();
        sample.Versao = sample.InfCTe.Versao = CTeVersao.v300;
        sample.InfCTe.Id = "CTe" + PdfTests.Key;
        var loaded = CTeOS.Load(sample.GetXml());
        await Assert.That(loaded.InfCTe.Versao).IsEqualTo(CTeVersao.v300);
        var processed = new CTeProc { CTe = null, CTeOS = loaded, Versao = CTeVersao.v300 };
        using var stream = new MemoryStream(new OpenDACTe().GerarPDF(processed));
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        await Assert.That(pdf.PageCount > 0).IsTrue();
    }

    private sealed class ProbeService(CTeConfig config) : CTeServiceClient(config, "https://example.test/cte")
    {
        internal Task<string> SendAsync() => ExecuteAsync("urn:test", "<request/>", "", default);
        protected override string TratarRetorno(XElement body) => body.ToString();
    }

    private sealed class RejectTransport : IDFeHttpTransport
    {
        internal int Calls;
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, DFeHttpTransportOptions options, CancellationToken cancellationToken = default)
        {
            Calls++;
            throw new InvalidOperationException("Nenhuma comunicação deveria ocorrer.");
        }
    }
}
