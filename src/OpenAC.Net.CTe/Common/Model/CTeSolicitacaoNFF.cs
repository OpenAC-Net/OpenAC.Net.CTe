using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;

namespace OpenAC.Net.CTe;

public sealed partial class CTeSolicitacaoNFF
{
    [DFeElement(TipoCampo.Str, "xSolic", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string XSolic { get; set; }
}
