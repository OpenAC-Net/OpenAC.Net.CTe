using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;
using OpenAC.Net.DFe.Core.Collection;

namespace OpenAC.Net.CTe;

public sealed partial class CTePagamentoAntecipado
{
    [DFeCollection("chDFePagAnt", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public DFeCollection<string> ChDFePagAnt { get; set; } = [];
}
