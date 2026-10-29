using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;

namespace OpenAC.Net.CTe;

public sealed partial class CTeDiferimento
{
    [DFeElement(TipoCampo.De4, "pDif", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? PDif { get; set; }
    [DFeElement(TipoCampo.De2, "vDif", Ordem=2, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VDif { get; set; }
}
