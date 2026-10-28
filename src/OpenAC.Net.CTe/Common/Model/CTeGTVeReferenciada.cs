using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;
using OpenAC.Net.DFe.Core.Collection;

namespace OpenAC.Net.CTe;

public sealed partial class CTeGTVeReferenciada
{
    [DFeElement(TipoCampo.Str, "chCTe", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string ChCTe { get; set; }
    [DFeCollection("Comp", Ordem=2, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public DFeCollection<TcTeOsInfCteInfCTeNormInfGtVeComp> Comp { get; set; } = [];
}
