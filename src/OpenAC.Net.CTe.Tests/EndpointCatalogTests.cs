using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.DFe.Core.Common;

namespace OpenAC.Net.CTe.Tests;

public class EndpointCatalogTests
{
    private static readonly DateTime ReferenceDate = new(2026, 10, 29);
    private static readonly DFeSiglaUF[] States =
    [
        .. "AC AL AP AM BA CE DF ES GO MA MT MS MG PA PB PR PE PI RJ RN RS RO RR SC SP SE TO"
            .Split(' ').Select(Enum.Parse<DFeSiglaUF>)
    ];
    private static readonly HashSet<DFeSiglaUF> SvcRs = [DFeSiglaUF.AP, DFeSiglaUF.PE, DFeSiglaUF.RR, DFeSiglaUF.MT, DFeSiglaUF.MS, DFeSiglaUF.SP];
    private static readonly Dictionary<TipoServicoCTe, string> Services = new()
    {
        [TipoServicoCTe.QRCode] = "qrcode", [TipoServicoCTe.CTeStatusServico] = "CteStatusServicoV4",
        [TipoServicoCTe.CTeConsultaProtocolo] = "CteConsultaV4", [TipoServicoCTe.RecepcaoEvento] = "CteRecepcaoEventoV4",
        [TipoServicoCTe.CTeRecepcaoOS] = "CTeRecepcaoOSV4", [TipoServicoCTe.CTeRecepcaoSinc] = "CTeRecepcaoSincV4"
    };

    [Test]
    [Arguments(DFeTipoAmbiente.Producao)]
    [Arguments(DFeTipoAmbiente.Homologacao)]
    public async Task EveryStateMatchesItsOfficialNormalAndContingencyAuthorizer(DFeTipoAmbiente environment)
    {
        using var fixture = LoadFixture();
        var table = fixture.RootElement.GetProperty("cte").GetProperty(environment == DFeTipoAmbiente.Producao ? "1" : "2");
        foreach (var state in States)
        {
            var normal = state is DFeSiglaUF.AP or DFeSiglaUF.PE or DFeSiglaUF.RR ? "SVSP"
                : state is DFeSiglaUF.MT or DFeSiglaUF.MS or DFeSiglaUF.MG or DFeSiglaUF.PR or DFeSiglaUF.RS or DFeSiglaUF.SP ? state.ToString() : "SVRS";
            var contingency = SvcRs.Contains(state) ? "SVRS" : "SVSP";
            var emission = SvcRs.Contains(state) ? DFeTipoEmissao.SVCRS : DFeTipoEmissao.SVCSP;
            foreach (var (service, official) in Services)
            {
                await Assert.That(Address(state, service, DFeTipoEmissao.Normal, environment))
                    .IsEqualTo(table.GetProperty(normal).GetProperty(official).GetProperty("endpoint").GetString());
                await Assert.That(Address(state, service, emission, environment))
                    .IsEqualTo(table.GetProperty(contingency).GetProperty(official).GetProperty("endpoint").GetString());
                var incorrect = emission == DFeTipoEmissao.SVCRS ? DFeTipoEmissao.SVCSP : DFeTipoEmissao.SVCRS;
                Assert.ThrowsExactly<NotSupportedException>(() => Address(state, service, incorrect, environment));
            }
        }
    }

    [Test]
    [Arguments(DFeTipoAmbiente.Producao)]
    [Arguments(DFeTipoAmbiente.Homologacao)]
    public async Task CadastroAndDistributionUseTheirOwnEnvironmentAndPublishedCoverage(DFeTipoAmbiente environment)
    {
        using var fixture = LoadFixture();
        var env = environment == DFeTipoAmbiente.Producao ? "1" : "2";
        var published = fixture.RootElement.GetProperty("cadastro").GetProperty(env);
        var national = fixture.RootElement.GetProperty("cte").GetProperty(env).GetProperty("AN").GetProperty("CTeDistribuicaoDFe").GetProperty("endpoint").GetString();
        foreach (var state in States)
        {
            var cadastral = state is DFeSiglaUF.AC or DFeSiglaUF.ES or DFeSiglaUF.RN or DFeSiglaUF.PB or DFeSiglaUF.SC ? "SVRS" : state.ToString();
            foreach (var emission in Enum.GetValues<DFeTipoEmissao>())
            {
                await Assert.That(Address(state, TipoServicoCTe.DistribuicaoDFe, emission, environment)).IsEqualTo(national);
                if (published.TryGetProperty(cadastral, out var expected))
                    await Assert.That(Address(state, TipoServicoCTe.CTeConsultaCadastro, emission, environment)).IsEqualTo(expected.GetString());
                else Assert.ThrowsExactly<NotSupportedException>(() => Address(state, TipoServicoCTe.CTeConsultaCadastro, emission, environment));
            }
        }
    }

    [Test]
    public async Task TransitionRespectsSeparateEnvironmentsAndEpecReconciliation()
    {
        using var fixture = LoadFixture();
        foreach (var state in States)
        {
            var normalProduction = Address(state, TipoServicoCTe.CTeRecepcaoSinc, DFeTipoEmissao.Normal, DFeTipoAmbiente.Producao);
            var normalHomologation = Address(state, TipoServicoCTe.CTeRecepcaoSinc, DFeTipoEmissao.Normal, DFeTipoAmbiente.Homologacao);
            await Assert.That(Address(state, TipoServicoCTe.CTeRecepcaoSinc, DFeTipoEmissao.FSDA, DFeTipoAmbiente.Producao)).IsEqualTo(normalProduction);
            Assert.ThrowsExactly<NotSupportedException>(() => Address(state, TipoServicoCTe.CTeRecepcaoSinc, DFeTipoEmissao.FSDA, DFeTipoAmbiente.Homologacao));
            await Assert.That(Address(state, TipoServicoCTe.CTeRecepcaoSinc, DFeTipoEmissao.Contingencia, DFeTipoAmbiente.Homologacao)).IsEqualTo(normalHomologation);
            Assert.ThrowsExactly<NotSupportedException>(() => Address(state, TipoServicoCTe.CTeRecepcaoSinc, DFeTipoEmissao.Contingencia, DFeTipoAmbiente.Producao));
            var svc = SvcRs.Contains(state) ? "SVRS" : "SVSP";
            await Assert.That(Address(state, TipoServicoCTe.RecepcaoEventoEpec, DFeTipoEmissao.DPEC, DFeTipoAmbiente.Producao))
                .IsEqualTo(fixture.RootElement.GetProperty("cte").GetProperty("1").GetProperty(svc).GetProperty("CteRecepcaoEventoV4").GetProperty("endpoint").GetString());
            Assert.ThrowsExactly<NotSupportedException>(() => Address(state, TipoServicoCTe.RecepcaoEventoEpec, DFeTipoEmissao.DPEC, DFeTipoAmbiente.Homologacao));
            var after = new DateTime(2026, 12, 14);
            Assert.ThrowsExactly<NotSupportedException>(() => Address(state, TipoServicoCTe.CTeRecepcaoSinc, DFeTipoEmissao.FSDA, DFeTipoAmbiente.Producao, after));
            Assert.ThrowsExactly<NotSupportedException>(() => Address(state, TipoServicoCTe.RecepcaoEventoEpec, DFeTipoEmissao.DPEC, DFeTipoAmbiente.Producao, after));
            await Assert.That(Address(state, TipoServicoCTe.CTeRecepcaoSinc, DFeTipoEmissao.Contingencia, DFeTipoAmbiente.Producao, after)).IsEqualTo(normalProduction);
            await Assert.That(Address(state, TipoServicoCTe.CTeRecepcaoSinc, DFeTipoEmissao.DPEC, DFeTipoAmbiente.Producao, after)).IsEqualTo(normalProduction);
            await Assert.That(Address(state, TipoServicoCTe.CTeRecepcaoSinc, DFeTipoEmissao.DPEC, DFeTipoAmbiente.Homologacao)).IsEqualTo(normalHomologation);
        }
    }

    [Test]
    public async Task EmbeddedCatalogHasOnlyCurrentVersionServicesAndValidSvcAssignments()
    {
        using var stream = typeof(OpenCTe).Assembly.GetManifestResourceStream("OpenAC.Net.CTe.Resources.services.xml")!;
        var xml = XDocument.Load(stream);
        XNamespace ns = xml.Root!.Name.Namespace;
        await Assert.That(xml.Root.Elements().Select(element => (string)element.Attribute("Versao")!).Distinct().Single()).IsEqualTo("4.00");
        await Assert.That(xml.Descendants(ns + "Ambiente").Count()).IsEqualTo(108);
        await Assert.That(xml.Descendants(ns + "Endereco").Count()).IsEqualTo(751);
        foreach (var endpoint in xml.Descendants(ns + "Endereco"))
        {
            await Assert.That(Enum.GetNames<TipoServicoCTe>()).Contains((string)endpoint.Attribute("Tipo")!);
            await Assert.That(endpoint.Value.Contains("?wsdl", StringComparison.OrdinalIgnoreCase)).IsFalse();
        }
        stream.Position = 0;
        var catalogo = CTeServiceManager.CarregarCatalogo(stream);
        using var exportado = new MemoryStream(Encoding.UTF8.GetBytes(catalogo.GetXml()));
        var recarregado = CTeServiceManager.CarregarCatalogo(exportado);
        await Assert.That(recarregado.Webservices.Count).IsEqualTo(3);
        await Assert.That(recarregado[DFeTipoEmissao.Normal, "4.00"][DFeTipoAmbiente.Producao, DFeSiglaUF.MG][TipoServicoCTe.CTeStatusServico])
            .IsEqualTo(Address(DFeSiglaUF.MG, TipoServicoCTe.CTeStatusServico, DFeTipoEmissao.Normal, DFeTipoAmbiente.Producao));
        await Assert.That(Enum.GetNames<TipoServicoCTe>()).DoesNotContain("CTeRetRecepcao");
        await Assert.That(Enum.GetNames<TipoServicoCTe>()).DoesNotContain("CTeInutilizacao");
        await Assert.That(Enum.GetNames<TipoServicoCTe>()).DoesNotContain("RecepcaoEventoAN");
    }

    [Test]
    [Arguments("version")]
    [Arguments("service")]
    [Arguments("duplicate")]
    [Arguments("environment")]
    [Arguments("svc")]
    [Arguments("numeric-service")]
    public void CustomCatalogRejectsInvalidOrRetiredEntriesBeforeDeserialization(string kind)
    {
        XNamespace ns = "https://www.openac.net.br/";
        var xml = new XElement(ns + "DFeServices", new XElement(ns + "Services", new XAttribute("Tipo", "CTe"), new XAttribute("TipoEmissao", "1"), new XAttribute("Versao", "4.00"),
            new XElement(ns + "Ambiente", new XAttribute("Tipo", "1"), new XAttribute("UF", "MG"),
                new XElement(ns + "Enderecos", new XElement(ns + "Endereco", new XAttribute("Tipo", "CTeStatusServico"), "https://example.test/status")))));
        if (kind == "version") xml.Element(ns + "Services")!.SetAttributeValue("Versao", "3.00");
        if (kind == "service") xml.Descendants(ns + "Endereco").Single().SetAttributeValue("Tipo", "CTeInutilizacao");
        if (kind == "numeric-service") xml.Descendants(ns + "Endereco").Single().SetAttributeValue("Tipo", "0");
        if (kind == "duplicate") xml.Descendants(ns + "Enderecos").Single().Add(new XElement(xml.Descendants(ns + "Endereco").Single()));
        if (kind == "environment") xml.Descendants(ns + "Ambiente").Single().SetAttributeValue("Tipo", "3");
        if (kind == "svc") xml.Element(ns + "Services")!.SetAttributeValue("TipoEmissao", "7");
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(xml.ToString()));
        if (kind is "version" or "service" or "numeric-service" or "duplicate") Assert.ThrowsExactly<NotSupportedException>(() => CTeServiceManager.CarregarCatalogo(input));
        else Assert.ThrowsExactly<ArgumentException>(() => CTeServiceManager.CarregarCatalogo(input));
    }

    [Test]
    public void CustomEndpointCannotBypassStateAndContingencyValidation()
    {
        var config = new CTeConfig(); config.WebServices.UF = DFeCodUF.MG;
        config.Geral.FormaEmissao = DFeTipoEmissao.SVCRS;
        config.WebServices.Enderecos[TipoServicoCTe.CTeStatusServico] = "https://example.test/status";
        Assert.ThrowsExactly<NotSupportedException>(() => new CTeStatusServicoServiceClient(config));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Address(DFeSiglaUF.AN, TipoServicoCTe.CTeStatusServico, DFeTipoEmissao.Normal, DFeTipoAmbiente.Producao));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Address(DFeSiglaUF.MG, TipoServicoCTe.CTeStatusServico, DFeTipoEmissao.Normal, (DFeTipoAmbiente)99));
    }

    [Test]
    public void CteOsDoesNotExposeEpecAsAnEmissionMode()
    {
        foreach (var state in States)
            foreach (var environment in new[] { DFeTipoAmbiente.Producao, DFeTipoAmbiente.Homologacao })
                Assert.ThrowsExactly<NotSupportedException>(() => Address(state, TipoServicoCTe.CTeRecepcaoOS, DFeTipoEmissao.DPEC, environment));
    }

    private static string Address(DFeSiglaUF state, TipoServicoCTe service, DFeTipoEmissao emission, DFeTipoAmbiente environment, DateTime? date = null) =>
        CTeServiceManager.ObterEndereco(CTeVersao.v400, state, service, emission, environment, date ?? ReferenceDate);

    private static JsonDocument LoadFixture()
    {
        using var stream = typeof(EndpointCatalogTests).Assembly.GetManifestResourceStream("OpenAC.Net.CTe.Tests.Resources.enderecos-oficiais-2026-10-29.json")!;
        return JsonDocument.Parse(stream);
    }
}
