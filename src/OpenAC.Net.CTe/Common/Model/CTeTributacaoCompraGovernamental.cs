using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;

namespace OpenAC.Net.CTe;

public sealed partial class CTeTributacaoCompraGovernamental
{
    [DFeElement(TipoCampo.De4, "pAliqIBSUF", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? PAliqIBSUF { get; set; }
    [DFeElement(TipoCampo.De2, "vTribIBSUF", Ordem=2, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VTribIBSUF { get; set; }
    [DFeElement(TipoCampo.De4, "pAliqIBSMun", Ordem=3, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? PAliqIBSMun { get; set; }
    [DFeElement(TipoCampo.De2, "vTribIBSMun", Ordem=4, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VTribIBSMun { get; set; }
    [DFeElement(TipoCampo.De4, "pAliqCBS", Ordem=5, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? PAliqCBS { get; set; }
    [DFeElement(TipoCampo.De2, "vTribCBS", Ordem=6, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VTribCBS { get; set; }
}
