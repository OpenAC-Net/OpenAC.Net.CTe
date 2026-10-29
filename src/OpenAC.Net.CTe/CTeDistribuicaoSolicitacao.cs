using OpenAC.Net.DFe.Core.Common;

namespace OpenAC.Net.CTe;

/// <summary>Consulta de distribuição com exatamente um filtro: último NSU ou NSU específico.</summary>
public sealed class CTeDistribuicaoSolicitacao
{
    public DFeCodUF UF { get; set; }
    public string CpfCnpj { get; set; }
    public string UltimoNsu { get; set; }
    public string Nsu { get; set; }
}
