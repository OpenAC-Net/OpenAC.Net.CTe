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
    public CTeProc DocumentoProcessado { get; internal set; }
    public int Codigo => Resultado?.ProtCTe?.InfProt?.CStat ?? Resultado?.CStat ?? 0;
    public string Motivo => Resultado?.ProtCTe?.InfProt?.XMotivo ?? Resultado?.Motivo;
    public string Protocolo => Resultado?.ProtCTe?.InfProt?.NProt;
    public bool Autorizado => Resultado?.ProtCTe?.InfProt?.CStat is 100 or 150;
}
