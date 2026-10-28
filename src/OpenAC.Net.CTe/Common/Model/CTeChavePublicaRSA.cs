using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;

namespace OpenAC.Net.CTe;

public sealed partial class CTeChavePublicaRSA
{
    [DFeElement(TipoCampo.Str, "Modulus", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string Modulus { get; set; }
    [DFeElement(TipoCampo.Str, "Exponent", Ordem=2, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string Exponent { get; set; }
}