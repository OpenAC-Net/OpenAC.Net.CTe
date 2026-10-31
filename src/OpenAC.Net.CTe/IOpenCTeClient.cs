using System.Threading;
using System.Threading.Tasks;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.DFe.Core.Common;

namespace OpenAC.Net.CTe;

/// <summary>Operações fiscais independentes, com dados e cancelamento explícitos por chamada.</summary>
public interface IOpenCTeClient
{
    Task<CTeAutorizacaoResposta> AutorizarAsync(CTe documento, CancellationToken cancellationToken = default);

    Task<CTeAutorizacaoResposta> AutorizarAsync(CTeOS documento, CancellationToken cancellationToken = default);

    Task<ConsultaCTeResposta> ConsultarAsync(string chave, CancellationToken cancellationToken = default);

    Task<ConsultaStatusResposta> ConsultarSituacaoServicoAsync(CancellationToken cancellationToken = default);

    Task<RecepcaoEventoResposta> EnviarEventoAsync(CTeEventoSolicitacao solicitacao,
        CancellationToken cancellationToken = default);

    Task<DFeConsultaCadastroResposta> ConsultaCadastroAsync(DFeCodUF uf, string cpfCNPJ = "", string ie = "",
        CancellationToken cancellationToken = default);

    Task<DistribuicaoDFeResponse> ConsultarDistribuicaoAsync(CTeDistribuicaoSolicitacao solicitacao,
        CancellationToken cancellationToken = default);
}