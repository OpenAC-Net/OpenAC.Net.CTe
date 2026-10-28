using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;
using OpenAC.Net.DFe.Core.Service;

namespace OpenAC.Net.CTe.Services;

/// <summary>Resultado de autorização síncrona de CT-e e CT-e OS.</summary>
[DFeRoot("retCTe", Namespace = "http://www.portalfiscal.inf.br/cte")]
public sealed partial class CTeAutorizacaoResult : CTeResultBase<CTeAutorizacaoResult>
{
    [DFeElement("protCTe", Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeProtCTe ProtCTe { get; set; }
    private static string[] GetRootNames() => ["retCTe", "retCTeOS"];
}

public sealed class CTeAutorizacaoResposta : DFeResposta<CTeAutorizacaoResult>
{
    public CTeAutorizacaoResposta(string xmlEnvio, string xmlRetorno, string envelopeEnvio, string envelopeRetorno)
        : base(xmlEnvio, xmlRetorno, envelopeEnvio, envelopeRetorno) { }
    public CTeProc Conhecimento { get; internal set; }
    public bool Autorizado => Resultado?.ProtCTe?.InfProt?.CStat == 100 || Resultado?.ProtCTe?.InfProt?.CStat == 150;
}
