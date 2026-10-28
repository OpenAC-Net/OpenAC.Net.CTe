using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;
using OpenAC.Net.DFe.Core.Collection;

namespace OpenAC.Net.CTe;

public sealed partial class CTePagamentoRTC
{
    [DFeElement(TipoCampo.Str, "tpMeioPgto", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string TpMeioPgto { get; set; }
    [DFeElement(TipoCampo.Str, "CNPJReceb", Ordem=2, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string CNPJReceb { get; set; }
    [DFeElement(TipoCampo.Str, "CNPJBasePSP", Ordem=3, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string CNPJBasePSP { get; set; }
}
