using System.Xml.Linq;
using OpenAC.Net.CTe.DACTe.PDFSharp;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.DFe.Core;
using OpenAC.Net.DFe.Core.Common;

namespace OpenAC.Net.CTe.Tests;

public class VersionTests
{
    private static readonly XNamespace Ns = "http://www.portalfiscal.inf.br/cte";
    [Test]
    [Arguments(CTeVersao.v200, false)]
    [Arguments(CTeVersao.v300, false)]
    [Arguments(CTeVersao.v400, true)]
    public async Task SingleModelSelectsFieldsByVersionAndRoundTrips(CTeVersao version, bool hasCrt)
    {
        var cte = Sample(version);
        var xml = cte.GetXml();
        var root = XElement.Parse(xml);
        await Assert.That(root.Descendants(Ns + "CRT").Any()).IsEqualTo(hasCrt);
        var loaded = CTe.Load(xml);
        await Assert.That(loaded.InfCTe.Versao).IsEqualTo(version);
        await Assert.That(loaded.InfCTe.Emit.XNome).IsEqualTo(cte.InfCTe.Emit.XNome);
        await Assert.That(loaded.InfCTe.VPrest.VTPrest).IsEqualTo(cte.InfCTe.VPrest.VTPrest);
        await Assert.That(loaded.InfCTe.InfoCTe).IsTypeOf<CTeNormal>();
    }

    [Test]
    public async Task Cte400SignsAndValidatesAgainstSchema()
    {
        var cte = Sample(CTeVersao.v400);
        using var certificate = InfrastructureTests.CreateCertificate();
        cte.Assinar(certificate, DFeSaveOptions.DisableFormatting);
        await Assert.That(cte.ValidarAssinatura()).IsTrue();
        await AssertSchemaAsync(cte.GetXml(), "cte_v4.00.xsd");
    }

    [Test]
    public async Task CteOS400SignsRoundTripsAndPrints()
    {
        var os = SampleOS();
        using var certificate = InfrastructureTests.CreateCertificate();
        os.Assinar(certificate, DFeSaveOptions.DisableFormatting);
        await Assert.That(os.ValidarAssinatura()).IsTrue();
        await AssertSchemaAsync(os.GetXml(), "cteOS_v4.00.xsd");
        var loaded = CTeOS.Load(os.GetXml());
        await Assert.That(loaded.InfCTe.Toma.XNome).IsEqualTo("Cliente Exemplo");
        await Assert.That(loaded.InfCTe.InfoCTe).IsTypeOf<CTeNormalOS>();
        var proc = new CTeProc { CTe = null, CTeOS = loaded, Versao = CTeVersao.v400 };
        var xml = proc.GetXml();
        await Assert.That(XElement.Parse(xml).Name.LocalName).IsEqualTo("cteOSProc");
        await Assert.That(CTeProc.Load(xml).CTeOS).IsNotNull();
        var bytes = new OpenDACTe().GerarPDF(proc);
        await Assert.That(bytes.Length > 1000).IsTrue();
    }

    [Test]
    [Arguments(DFeSiglaUF.MS, DFeTipoAmbiente.Homologacao, "homologacao.cte.ms.gov.br")]
    [Arguments(DFeSiglaUF.SP, DFeTipoAmbiente.Producao, "nfe.fazenda.sp.gov.br")]
    [Arguments(DFeSiglaUF.BA, DFeTipoAmbiente.Producao, "cte.svrs.rs.gov.br")]
    public async Task CatalogSelectsVersionAndEnvironment(DFeSiglaUF uf, DFeTipoAmbiente environment, string host)
    {
        var address = CTeServiceManager.ObterEndereco(CTeVersao.v400, uf, TipoServicoCTe.CTeRecepcaoSinc, DFeTipoEmissao.Normal, environment);
        await Assert.That(new Uri(address).Host).IsEqualTo(host);
        await Assert.That(address).Contains("CTeRecepcaoSincV4");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CTeServiceManager.ObterEndereco(CTeVersao.v400, uf, (TipoServicoCTe)999, DFeTipoEmissao.Normal, environment));
    }

    [Test]
    public async Task ReformTaxFieldsUseDFeCoreAndAreOmittedFromVersion300()
    {
        var document = Sample(CTeVersao.v400);
        document.InfCTe.Imp.IBSCBS = new CTeIBSCBS
        {
            CST = "000", CClassTrib = "000001",
            GIBSCBS = new CTeValoresIBSCBS
            {
                VBC = 1500m,
                GIBSUF = new CTeIBSUF { PIBSUF = .1m, VIBSUF = 1.5m },
                GIBSMun = new CTeIBSMunicipio { PIBSMun = 0m, VIBSMun = 0m },
                VIBS = 1.5m,
                GCBS = new CTeCBS { PCBS = .9m, VCBS = 13.5m }
            }
        };
        document.InfCTe.Imp.VTotDFe = 1515m;
        using var certificate = InfrastructureTests.CreateCertificate();
        document.Assinar(certificate, DFeSaveOptions.DisableFormatting);
        var xml = document.GetXml();
        await AssertSchemaAsync(xml, "cte_v4.00.xsd");
        var loaded = CTe.Load(xml);
        await Assert.That(loaded.InfCTe.Imp.IBSCBS.GIBSCBS.GIBSUF.PIBSUF).IsEqualTo(.1m);
        await Assert.That(loaded.InfCTe.Imp.VTotDFe).IsEqualTo(1515m);
        document.InfCTe.Versao = CTeVersao.v300;
        var legacy = XElement.Parse(document.GetXml());
        await Assert.That(legacy.Descendants(Ns + "IBSCBS")).IsEmpty();
        await Assert.That(legacy.Descendants(Ns + "vTotDFe")).IsEmpty();
    }

    [Test]
    public async Task ModelsLoadWithoutAClientOrCollection()
    {
        await Assert.That(CTe.Load(Sample(CTeVersao.v400).GetXml()).InfCTe.Versao).IsEqualTo(CTeVersao.v400);
        await Assert.That(CTeOS.Load(SampleOS().GetXml()).Versao).IsEqualTo(CTeVersao.v400);
    }

    [Test]
    public async Task Version400SupportsMultipleComplementedDocuments()
    {
        var document = Sample(CTeVersao.v400);
        document.InfCTe.Ide.TpCTe = CTeTipo.Complemento;
        document.InfCTe.InfoCTe = null;
        document.InfCTe.Complementos.Add(new CTeComplemento { ChCTe = PdfTests.Key });
        document.InfCTe.Complementos.Add(new CTeComplemento { ChCTe = PdfTests.Key.Substring(0, 43) + "1" });
        using var certificate = InfrastructureTests.CreateCertificate();
        document.Assinar(certificate, DFeSaveOptions.DisableFormatting);
        var xml = document.GetXml();
        await AssertSchemaAsync(xml, "cte_v4.00.xsd");
        await Assert.That(CTe.Load(xml).InfCTe.Complementos.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Cte300StillSignsAndValidatesAgainstItsSchema()
    {
        var document = Sample(CTeVersao.v300);
        using var certificate = InfrastructureTests.CreateCertificate();
        document.Assinar(certificate, DFeSaveOptions.DisableFormatting);
        await Assert.That(document.ValidarAssinatura()).IsTrue();
        await AssertSchemaAsync(document.GetXml(), "cte_v3.00.xsd");
    }

    internal static CTe Sample(CTeVersao version)
    {
        var cte = PdfTests.Sample().CTe;
        var info = cte.InfCTe;
        info.Id = null;
        info.Versao = version;
        info.Ide.CUF = DFeCodUF.SP;
        info.Ide.CCT = 12345678;
        info.Ide.TpImp = DFeTipoImpressao.Retrato;
        info.Ide.CFOP = 6353;
        info.Ide.VerProc = "OpenAC.Net";
        info.Ide.CMunEnv = info.Ide.CMunIni = 3550308;
        info.Ide.XMunEnv = "São Paulo";
        info.Ide.UFEnv = info.Ide.UFIni = DFeSiglaUF.SP;
        info.Ide.CMunFim = 5002704;
        info.Ide.UFFim = DFeSiglaUF.MS;
        info.Ide.TpAmb = DFeTipoAmbiente.Homologacao;
        info.Ide.TpEmis = DFeTipoEmissao.Normal;
        info.Ide.Tomador = new CTeToma3 { Toma = CTeTomador.Remetente };
        info.Emit.CRT = 3;
        info.Emit.IE = "123456789";
        info.Emit.EnderEmit.XBairro = "Centro";
        info.Emit.EnderEmit.CMun = 3550308;
        info.Emit.EnderEmit.UF = DFeSiglaUF.SP;
        info.Rem.CNPJ = "11222333000181";
        info.Rem.IE = "123456789";
        Address(info.Rem.EnderReme);
        info.Dest.CNPJ = "44555666000181";
        info.Dest.IE = "123456789";
        Address(info.Dest.EnderDest);
        info.Imp.ICMS.ICMS = new CTeICMS00 { VBC = 1500, PICMS = 12, VICMS = 180 };
        var normal = (CTeNormal)info.InfoCTe;
        normal.InfCarga.InfQ.Add(new CTeInfQ { TpMed = "PESO BRUTO", QCarga = 1500 });
        cte.InfCTeSupl = null;
        return cte;
    }
    internal static CTeOS SampleOS()
    {
        var os = new CTeOS { InfCTe = Sample(CTeVersao.v400).InfCTe, Versao = CTeVersao.v400 };
        os.InfCTe.Ide.Mod = ModeloCTe.CTeOS;
        os.InfCTe.Ide.TpServ = CTeTipoServico.TranspPessoas;
        os.InfCTe.Toma = new CTeOSToma { CNPJ = "11222333000181", XNome = "Cliente Exemplo" };
        Address(os.InfCTe.Toma.EnderToma);
        os.InfCTe.InfoCTe = new CTeNormalOS { InfServico = new CTeOSInfServico { XDescServ = "Transporte de passageiros" }, InfModal = new CTeInfModal { Modal = new CTeRodoModalOS { TAF = "123456789012" } } };
        return os;
    }
    private static void Address(CTeEndereco address)
    {
        address.XLgr = "Rua Exemplo"; address.Nro = "100"; address.XBairro = "Centro";
        address.CMun = 3550308; address.XMun = "São Paulo"; address.UF = DFeSiglaUF.SP;
    }
    private static async Task AssertSchemaAsync(string xml, string file)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Schemas", file);
        XmlSchemaValidation.ValidarXml(xml, path, out var errors, out _);
        await Assert.That(!errors.Any()).IsTrue().Because(string.Join(Environment.NewLine, errors));
    }
}
