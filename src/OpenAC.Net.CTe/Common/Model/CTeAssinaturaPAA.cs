using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;

namespace OpenAC.Net.CTe;

public sealed partial class CTeAssinaturaPAA
{
    [DFeElement(TipoCampo.Str, "SignatureValue", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string SignatureValue { get; set; }
    [DFeElement("RSAKeyValue", Ordem=2, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeChavePublicaRSA RSAKeyValue { get; set; }
}
