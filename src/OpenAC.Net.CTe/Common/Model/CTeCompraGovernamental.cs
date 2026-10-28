using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;
using OpenAC.Net.DFe.Core.Collection;

namespace OpenAC.Net.CTe;

public sealed partial class CTeCompraGovernamental
{
    [DFeElement(TipoCampo.Enum, "tpEnteGov", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeEnteGovernamental TpEnteGov { get; set; }
    [DFeElement(TipoCampo.De4, "pRedutor", Ordem=2, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? PRedutor { get; set; }
    [DFeElement(TipoCampo.Enum, "tpOperGov", Ordem=3, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeOperacaoCompraGovernamental TpOperGov { get; set; }
    [DFeCollection("refDFeAnt", Ordem=4, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public DFeCollection<string> RefDFeAnt { get; set; } = [];
}
