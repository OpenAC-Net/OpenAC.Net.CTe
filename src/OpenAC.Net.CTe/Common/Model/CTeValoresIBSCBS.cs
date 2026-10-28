using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;
using OpenAC.Net.DFe.Core.Collection;

namespace OpenAC.Net.CTe;

public sealed partial class CTeValoresIBSCBS
{
    [DFeElement(TipoCampo.De2, "vBC", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VBC { get; set; }
    [DFeElement("gIBSUF", Ordem=2, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeIBSUF GIBSUF { get; set; }
    [DFeElement("gIBSMun", Ordem=3, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeIBSMunicipio GIBSMun { get; set; }
    [DFeElement(TipoCampo.De2, "vIBS", Ordem=4, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VIBS { get; set; }
    [DFeElement("gCBS", Ordem=5, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeCBS GCBS { get; set; }
    [DFeElement("gTribRegular", Ordem=6, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeTributacaoRegular GTribRegular { get; set; }
    [DFeElement("gTribCompraGov", Ordem=7, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeTributacaoCompraGovernamental GTribCompraGov { get; set; }
}
