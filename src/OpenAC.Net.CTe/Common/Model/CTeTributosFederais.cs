using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;

namespace OpenAC.Net.CTe;

public sealed partial class CTeTributosFederais
{
    [DFeElement(TipoCampo.De2, "vPIS", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VPIS { get; set; }
    [DFeElement(TipoCampo.De2, "vCOFINS", Ordem=2, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VCOFINS { get; set; }
    [DFeElement(TipoCampo.De2, "vIR", Ordem=3, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VIR { get; set; }
    [DFeElement(TipoCampo.De2, "vINSS", Ordem=4, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VINSS { get; set; }
    [DFeElement(TipoCampo.De2, "vCSLL", Ordem=5, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VCSLL { get; set; }
}
