using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;
using OpenAC.Net.DFe.Core.Collection;

namespace OpenAC.Net.CTe;

public sealed partial class CTeCreditoPresumidoCBS
{
    [DFeElement(TipoCampo.De4, "pAliqEfetRegCBS", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? PAliqEfetRegCBS { get; set; }
    [DFeElement(TipoCampo.De2, "vTribRegCBS", Ordem=2, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VTribRegCBS { get; set; }
}
