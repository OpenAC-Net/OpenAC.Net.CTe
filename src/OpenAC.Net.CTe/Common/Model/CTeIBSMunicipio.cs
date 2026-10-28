using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;
using OpenAC.Net.DFe.Core.Collection;

namespace OpenAC.Net.CTe;

public sealed partial class CTeIBSMunicipio
{
    [DFeElement(TipoCampo.De4, "pIBSMun", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? PIBSMun { get; set; }
    [DFeElement("gDif", Ordem=2, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeDiferimento GDif { get; set; }
    [DFeElement("gDevTrib", Ordem=3, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeDevolucaoTributo GDevTrib { get; set; }
    [DFeElement("gRed", Ordem=4, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeReducaoAliquota GRed { get; set; }
    [DFeElement(TipoCampo.De2, "vIBSMun", Ordem=5, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VIBSMun { get; set; }
}
