using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;
using OpenAC.Net.DFe.Core.Collection;

namespace OpenAC.Net.CTe;

public sealed partial class CTeProvedorAssinatura
{
    [DFeElement(TipoCampo.Str, "CNPJPAA", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string CNPJPAA { get; set; }
    [DFeElement("PAASignature", Ordem=2, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeAssinaturaPAA PAASignature { get; set; }
}
