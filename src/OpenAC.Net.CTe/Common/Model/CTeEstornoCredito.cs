using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;

namespace OpenAC.Net.CTe;

public sealed partial class CTeEstornoCredito
{
    [DFeElement(TipoCampo.De2, "vIBSEstCred", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VIBSEstCred { get; set; }
    [DFeElement(TipoCampo.De2, "vCBSEstCred", Ordem=2, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VCBSEstCred { get; set; }
}
