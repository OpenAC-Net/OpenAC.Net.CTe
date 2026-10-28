using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;
using OpenAC.Net.DFe.Core.Collection;

namespace OpenAC.Net.CTe;

public sealed partial class CTeIBSCBS
{
    [DFeElement(TipoCampo.Str, "CST", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string CST { get; set; }
    [DFeElement(TipoCampo.Str, "cClassTrib", Ordem=2, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string CClassTrib { get; set; }
    [DFeElement(TipoCampo.Enum, "indDoacao", Ordem=3, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeIndicadorDoacao? IndDoacao { get; set; }
    [DFeElement("gIBSCBS", Ordem=4, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeValoresIBSCBS GIBSCBS { get; set; }
    [DFeElement("gEstornoCred", Ordem=5, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeEstornoCredito GEstornoCred { get; set; }
}
