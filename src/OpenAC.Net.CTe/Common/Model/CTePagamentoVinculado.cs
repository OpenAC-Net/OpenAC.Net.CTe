using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;
using OpenAC.Net.DFe.Core.Collection;

namespace OpenAC.Net.CTe;

public sealed partial class CTePagamentoVinculado
{
    [DFeCollection("pgto", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public DFeCollection<CTePagamentoRTC> Pgto { get; set; } = [];
}
