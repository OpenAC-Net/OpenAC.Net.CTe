using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Common;
using OpenAC.Net.DFe.Core.Serializer;
namespace OpenAC.Net.CTe;
[DFeRoot("infPercurso", Namespace = "http://www.portalfiscal.inf.br/cte")]
public sealed partial class CTePercurso
{
    [DFeElement(TipoCampo.Enum, "UFPer", Ocorrencia = Ocorrencia.Obrigatoria)]
    public DFeSiglaUF UFPer { get; set; }
}
