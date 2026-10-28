using System;
using System.Threading;
using System.Threading.Tasks;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.DFe.Core.Common;

namespace OpenAC.Net.CTe;

/// <summary>Contrato assíncrono do componente. Utilize uma instância por fluxo de operações.</summary>
public interface IOpenCTeClient : IDisposable
{
    CTeConfig Configuracoes { get; }

    ConhecimentosCollection Conhecimentos { get; }

    Task<CTeAutorizacaoResposta> AutorizarAsync(CTe documento, CancellationToken cancellationToken = default);

    Task<CTeAutorizacaoResposta> AutorizarAsync(CTeOS documento, CancellationToken cancellationToken = default);

    Task<EnviarCTeResposta> EnviarAsync(int lote, bool imprimir = true, CancellationToken cancellationToken = default);

    Task<EnviarCTeResposta> EnviarAsync(string lote, bool imprimir = true, CancellationToken cancellationToken = default);

    Task<RetRecepcaoResposta> ConsultaReciboAsync(string recibo, CancellationToken cancellationToken = default);

    Task<ConsultaCTeResposta> ConsultarAsync(string chave = "", CancellationToken cancellationToken = default);

    Task<ConsultaStatusResposta> ConsultarSituacaoServicoAsync(CancellationToken cancellationToken = default);

    Task<InutilizaoResposta> InutilizacaoAsync(string cnpj, string aJustificativa, int ano, ModeloCTe modelo, int serie,
        int numeroInicial, int numeroFinal, CancellationToken cancellationToken = default);

    Task<RecepcaoEventoResposta> EnviarEventoAsync(int nSeqEvento, string chave, string cnpj, IEventoCTe evento, CancellationToken cancellationToken = default);

    Task<DFeConsultaCadastroResposta> ConsultaCadastroAsync(DFeCodUF uf, string cpfCNPJ = "", string ie = "", CancellationToken cancellationToken = default);

    Task<DistribuicaoDFeResponse> ConsultarDistribuicaoAsync(DFeCodUF uf, string cpfCNPJ, string nsu = "", string chave = "", string ultNsu = "", CancellationToken cancellationToken = default);
}
