using OpenAC.Net.DFe.Core.Extensions;
using System;
using System.Security.Cryptography.X509Certificates;
using OpenAC.Net.Core;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.DFe.Core;
using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Common;
using OpenAC.Net.DFe.Core.Document;
using OpenAC.Net.DFe.Core.Serializer;

namespace OpenAC.Net.CTe;

[DFeSignInfoElement("infCte")]
[DFeRoot("CTeOS", Namespace = "http://www.portalfiscal.inf.br/cte")]
public sealed partial class CTeOS : DFeSignDocument<CTeOS>
{
    #region Events


    #endregion Events

    public CTeOS()
    {
        InfCTe = new CTeInfCTe();
        InfCTe.Ide.Mod = ModeloCTe.CTeOS;
        Versao = CTeVersao.v400;
    }

    #region Propriedades

    [DFeElement("infCte", Ocorrencia = Ocorrencia.Obrigatoria)]
    public CTeInfCTe InfCTe { get; set; }

    [DFeAttribute(TipoCampo.Enum, "versao", Id = "#002", Min = 4, Max = 4, Ocorrencia = Ocorrencia.Obrigatoria)]
    public CTeVersao Versao { get; set; }

    [DFeElement("infCTeSupl", Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeInfCteSupl InfCTeSupl { get; set; }

    [DFeIgnore]
    public bool Cancelada { get; set; }

    [DFeIgnore]
    public bool Assinado => ShouldSerializeSignature();

    #endregion Propriedades

    #region Methods

    /// <summary>
    /// Assina o CTe.
    /// </summary>
    /// <param name="certificado">The certificado.</param>
    /// <param name="saveOptions">The save options.</param>
    public void Assinar(X509Certificate2 certificado, DFeSaveOptions saveOptions)
    {
        Guard.Against<ArgumentNullException>(certificado == null, "Certificado não pode ser nulo.");

        if (InfCTe.Id.IsEmpty() || InfCTe.Id.Length < 44)
        {
            var chave = ChaveDFe.Gerar(InfCTe.Ide.CUF, InfCTe.Ide.DhEmi.DateTime,
                InfCTe.Emit.CNPJ, (int)InfCTe.Ide.Mod, InfCTe.Ide.Serie,
                InfCTe.Ide.NCT, InfCTe.Ide.TpEmis, InfCTe.Ide.CCT);

            InfCTe.Id = $"CTe{chave.Chave}";
            InfCTe.Ide.CDV = chave.Digito;
        }

        Versao = InfCTe.Versao;
        if (InfCTe.Versao == CTeVersao.v400 && string.IsNullOrEmpty(InfCTeSupl?.QrCode))
        {
            var address = Services.CTeServiceManager.GetServiceAndress(InfCTe.Versao, InfCTe.Ide.CUF.ToSiglaUF(),
                Services.TipoServicoCTe.QRCode, DFeTipoEmissao.Normal, InfCTe.Ide.TpAmb);
            InfCTeSupl = new CTeInfCteSupl { QrCode = address + (address.Contains("?") ? "&" : "?")
                + "chCTe=" + InfCTe.Id.Substring(3) + "&tpAmb=" + InfCTe.Ide.TpAmb.GetDFeValue() };
        }
        AssinarDocumento(certificado, saveOptions, false, SignDigest.SHA1);
    }

    public bool ValidarAssinatura(bool gerarXml = true)
    {
        Guard.Against<OpenDFeException>(!Assinado, "Documento não esta assinado.");
        return XmlSigning.ValidarAssinatura(this, gerarXml);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public string GetXmlName()
    {
        return $"{InfCTe.Id.OnlyNumbers()}-cteos.xml";
    }

    #endregion Methods
    internal bool ShouldSerializeInfCTeSupl() => !string.IsNullOrEmpty(InfCTeSupl?.QrCode);
    protected override bool ShouldSerializeSignature() => Signature != null && base.ShouldSerializeSignature();
}
