using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;
using OpenAC.Net.DFe.Core.Collection;

namespace OpenAC.Net.CTe;

public sealed partial class TcTeOsInfCteInfCTeNormInfGtVeComp
{
    [DFeElement(TipoCampo.Enum, "tpComp", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public TcTeOsInfCteInfCTeNormInfGtVeCompTpComp TpComp { get; set; }
    [DFeElement(TipoCampo.De2, "vComp", Ordem=2, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VComp { get; set; }
    [DFeElement(TipoCampo.Str, "xComp", Ordem=3, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string XComp { get; set; }
}
