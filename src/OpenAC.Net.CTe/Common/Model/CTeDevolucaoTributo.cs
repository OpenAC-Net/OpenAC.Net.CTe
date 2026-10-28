using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;
using OpenAC.Net.DFe.Core.Collection;

namespace OpenAC.Net.CTe;

public sealed partial class CTeDevolucaoTributo
{
    [DFeElement(TipoCampo.De4, "pDevTrib", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? PDevTrib { get; set; }
    [DFeElement(TipoCampo.De2, "vDevTrib", Ordem=2, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VDevTrib { get; set; }
}
