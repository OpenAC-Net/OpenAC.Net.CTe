using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;
using OpenAC.Net.DFe.Core.Collection;

namespace OpenAC.Net.CTe;

public sealed partial class CTeReducaoAliquota
{
    [DFeElement(TipoCampo.De4, "pRedAliq", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? PRedAliq { get; set; }
    [DFeElement(TipoCampo.De4, "pAliqEfet", Ordem=2, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? PAliqEfet { get; set; }
}
