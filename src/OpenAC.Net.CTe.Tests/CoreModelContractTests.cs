using System.Globalization;
using OpenAC.Net.CTe.Eventos;
using OpenAC.Net.CTe.Services;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using OpenAC.Net.DFe.Core.Common;

namespace OpenAC.Net.CTe.Tests;

public class CoreModelContractTests
{
    [Test]
    [Arguments("CTeDest")]
    [Arguments("CTeRem")]
    [Arguments("CTeExped")]
    [Arguments("CTeReceb")]
    [Arguments("CTeOSToma")]
    [Arguments("CTeToma4")]
    [Arguments("CTeEmiDocAnt")]
    [Arguments("CTeTomadorEPEC")]
    [Arguments("Services.DFeInfCad")]
    public async Task InscricaoEstadualNormalizaSomenteOXmlELeDeVolta(string name)
    {
        dynamic model = Activator.CreateInstance(typeof(CTe).Assembly.GetType("OpenAC.Net.CTe." + name)!)!;
        foreach (var sample in new[] { (Input: " isento ", Output: "ISENTO"), (Input: "12.345.678-9", Output: "123456789") })
        {
            model.IE = sample.Input;
            XElement xml = model.WriteToXml();
            await Assert.That(xml.Elements().Single(e => e.Name.LocalName == "IE").Value).IsEqualTo(sample.Output);
            string original = model.IE;
            await Assert.That(original).IsEqualTo(sample.Input);
            model.ReadXml(xml);
            string loaded = model.IE;
            await Assert.That(loaded).IsEqualTo(sample.Output);
        }
    }

    [Test]
    [Arguments("CTeDest")]
    [Arguments("CTeRem")]
    [Arguments("CTeExped")]
    [Arguments("CTeReceb")]
    [Arguments("CTeOSToma")]
    public async Task NomeDeHomologacaoSegueOParentSemAlterarNomeOriginal(string name)
    {
        dynamic model = Activator.CreateInstance(typeof(CTe).Assembly.GetType("OpenAC.Net.CTe." + name)!)!;
        model.XNome = "RAZAO SOCIAL ORIGINAL";
        XElement detached = model.WriteToXml();
        await Assert.That(detached.Elements().Single(e => e.Name.LocalName == "xNome").Value).IsEqualTo("RAZAO SOCIAL ORIGINAL");
        var parent = new CTeInfCTe();
        parent.Ide.TpAmb = DFeTipoAmbiente.Homologacao;
        model.Parent = parent;
        XElement homologation = model.WriteToXml();
        await Assert.That(homologation.Elements().Single(e => e.Name.LocalName == "xNome").Value).IsEqualTo("CT-E EMITIDO EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL");
        string original = model.XNome;
        await Assert.That(original).IsEqualTo("RAZAO SOCIAL ORIGINAL");
        parent.Ide.TpAmb = DFeTipoAmbiente.Producao;
        XElement production = model.WriteToXml();
        await Assert.That(production.Elements().Single(e => e.Name.LocalName == "xNome").Value).IsEqualTo("RAZAO SOCIAL ORIGINAL");
    }

    [Test]
    public async Task TomadorOsRecebeParentAoSerAssociadoARaiz()
    {
        var tomador = new CTeOSToma { XNome = "TOMADOR ORIGINAL" };
        var info = new CTeInfCTe { Toma = tomador };
        info.Ide.Mod = ModeloCTe.CTeOS;
        info.Ide.TpAmb = DFeTipoAmbiente.Homologacao;
        await Assert.That(tomador.Parent).IsSameReferenceAs(info);
        await Assert.That(tomador.WriteToXml().Elements().Single(e => e.Name.LocalName == "xNome").Value)
            .IsEqualTo("CT-E EMITIDO EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL");
        await Assert.That(tomador.XNome).IsEqualTo("TOMADOR ORIGINAL");
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task IndicadoresBooleanosUsamCodigosNumericosNaEscritaELeitura(bool value)
    {
        var disagreement = new CTeEvPrestDesacordo { IndDesacordoOper = value };
        var status = new DFeInfCad { CSit = value };
        var eventXml = disagreement.WriteToXml();
        var statusXml = status.WriteToXml();
        await Assert.That(eventXml.Elements().Single(e => e.Name.LocalName == "indDesacordoOper").Value).IsEqualTo(value ? "1" : "0");
        await Assert.That(statusXml.Elements().Single(e => e.Name.LocalName == "cSit").Value).IsEqualTo(value ? "1" : "0");
        await Assert.That(CTeEvPrestDesacordo.ReadFromXml(eventXml)!.IndDesacordoOper).IsEqualTo(value);
        await Assert.That(DFeInfCad.ReadFromXml(statusXml)!.CSit).IsEqualTo(value);
    }

    [Test]
    [Arguments(" isento ", "ISENTO")]
    [Arguments("12.345.678", "12345678")]
    public async Task RntrcUsaNumeroOuIsentoSemAlterarEntrada(string input, string expected)
    {
        var modal = new CTeRodoModal { RNTRC = input };
        var xml = modal.WriteToXml();
        await Assert.That(xml.Elements().Single(e => e.Name.LocalName == "RNTRC").Value).IsEqualTo(expected);
        await Assert.That(modal.RNTRC).IsEqualTo(input);
        await Assert.That(CTeRodoModal.ReadFromXml(xml)!.RNTRC).IsEqualTo(expected);
    }

    [Test]
    [Arguments(CTeVersao.v200, "pt-BR")]
    [Arguments(CTeVersao.v200, "en-US")]
    [Arguments(CTeVersao.v300, "pt-BR")]
    [Arguments(CTeVersao.v300, "en-US")]
    public async Task ContingenciaUsaFormatoDaVersaoEPreservaFusoQuandoExiste(CTeVersao version, string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var info = new CTeInfCTe { Versao = version };
            var date = new DateTimeOffset(2026, 10, 30, 13, 45, 0, TimeSpan.FromHours(-3));
            info.Ide.Mod = ModeloCTe.CTe;
            info.Ide.TpEmis = DFeTipoEmissao.FSDA;
            info.Ide.DhCont = date;
            info.Ide.DhEmi = date;
            var xml = info.Ide.WriteToXml();
            var dates = xml.Elements().Where(e => e.Name.LocalName == "dhCont").ToArray();
            await Assert.That(dates.Length).IsEqualTo(1);
            await Assert.That(dates[0].Value).IsEqualTo(version == CTeVersao.v200 ? "2026-10-30T13:45:00" : "2026-10-30T13:45:00-03:00");
            await Assert.That(xml.Elements().Single(e => e.Name.LocalName == "dhEmi").Value).IsEqualTo(dates[0].Value);
            var loaded = CTeIde.ReadFromXml(xml)!;
            await Assert.That(loaded.DhCont.DateTime).IsEqualTo(date.DateTime);
            await Assert.That(loaded.DhEmi.DateTime).IsEqualTo(date.DateTime);
            if (version == CTeVersao.v300)
            {
                await Assert.That(loaded.DhCont.Offset).IsEqualTo(date.Offset);
                await Assert.That(loaded.DhEmi.Offset).IsEqualTo(date.Offset);
            }
            info.Versao = CTeVersao.v400;
            await Assert.That(info.Ide.WriteToXml().Elements().Any(e => e.Name.LocalName == "dhCont")).IsFalse();
            info.Versao = version;
            info.Ide.TpEmis = DFeTipoEmissao.Normal;
            await Assert.That(info.Ide.WriteToXml().Elements().Any(e => e.Name.LocalName == "dhCont")).IsFalse();
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Test]
    [Arguments(CTeVersao.v200)]
    [Arguments(CTeVersao.v300)]
    public async Task DocumentoLegadoEmContingenciaAssinaEPassaNoXsdLocal(CTeVersao version)
    {
        var document = VersionTests.Sample(version);
        document.InfCTe.Ide.TpEmis = DFeTipoEmissao.FSDA;
        document.InfCTe.Ide.DhCont = new DateTimeOffset(2026, 10, 30, 13, 45, 0, TimeSpan.FromHours(-3));
        document.InfCTe.Ide.XJust = "Contingencia de teste por falha de comunicacao";
        using var certificate = InfrastructureTests.CreateCertificate();
        document.Assinar(certificate, DFeSaveOptions.DisableFormatting | DFeSaveOptions.OmitDeclaration);
        var xml = XDocument.Parse(document.GetXml());
        await Assert.That(Errors(xml, version == CTeVersao.v200 ? "cte_v2.00.xsd" : "cte_v3.00.xsd")).IsEmpty();
        await Assert.That(document.ValidarAssinatura()).IsTrue();
    }

    [Test]
    [Arguments("3.00", false)]
    [Arguments("3.00", true)]
    [Arguments("4.00", false)]
    [Arguments("4.00", true)]
    public async Task MultimodalEmiteSeguroSomenteQuandoInformadoEValidaNoXsd(string version, bool insured)
    {
        var modal = new CTeMultimodal { COTM = "12345" };
        if (insured)
        {
            modal.Seg.InfSeg.XSeg = "SEGURADORA DE TESTE";
            modal.Seg.InfSeg.CNPJ = "12345678000195";
            modal.Seg.NApol = "APOLICE123";
            modal.Seg.NAver = "AVERBACAO123";
        }
        var xml = modal.WriteToXml("multimodal", "http://www.portalfiscal.inf.br/cte");
        await Assert.That(xml.Elements().Any(e => e.Name.LocalName == "seg")).IsEqualTo(insured);
        await Assert.That(Errors(new XDocument(xml), "cteMultiModal_v" + version + ".xsd")).IsEmpty();
        var loaded = CTeMultimodal.ReadFromXml(xml)!;
        if (insured) await Assert.That(loaded.Seg.NAver).IsEqualTo("AVERBACAO123");
        modal.Seg = null!;
        await Assert.That(modal.WriteToXml().Elements().Any(e => e.Name.LocalName == "seg")).IsFalse();
    }

    private static List<string> Errors(XDocument document, string file)
    {
        var schemas = new XmlSchemaSet { XmlResolver = new XmlUrlResolver() };
        schemas.Add(null, Path.Combine(AppContext.BaseDirectory, "Schemas", file));
        schemas.Compile();
        var errors = new List<string>();
        document.Validate(schemas, (_, e) => errors.Add(e.Message));
        return errors;
    }
}
